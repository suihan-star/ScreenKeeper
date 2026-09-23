using OLED_Sleeper.Features.ColorTemperature;
using OLED_Sleeper.Storage;
using OLED_Sleeper.Tests.TestDoubles;

namespace OLED_Sleeper.Tests.Features.ColorTemperature;

public sealed class ColorValidationTests
{
    private static readonly ColorDisplayScope A = new("a", "physical-a", "DISPLAY-A", "Test display A", "topology", "sdr-mode");
    private static readonly ColorDisplayScope B = new("b", "physical-b", "DISPLAY-B", "Test display B", "topology", "sdr-mode");
    private static ColorValue Original() => new() { Channels = Enumerable.Range(0, 768).Select(i => (ushort)(i % 256 * 257)).ToArray() };
    private static Task NoDelay(TimeSpan _, CancellationToken token) { token.ThrowIfCancellationRequested(); return Task.CompletedTask; }

    [Fact]
    public async Task TrialRestoresBeforeObservationAndOnlyExplicitConfirmationPersistsExactScope()
    {
        var f = new Fixture();
        var result = await f.Run();
        Assert.True(result.CanConfirm);
        Assert.False(f.Registry.IsValidated(A));
        Assert.Equal(2, f.Backend.Writes);
        Assert.False(f.Backend.Leased);
        Assert.True(Original().SameAs(f.Backend.Values["a"]));
        Assert.True(Original().SameAs(f.Backend.Values["b"]));
        Assert.Empty(f.Store.Document.Records);
        Assert.True((await f.Service.ConfirmValidationAsync(result.Ticket, f.Registry)).CanConfirm);
        Assert.True(f.Registry.IsValidated(A));
        Assert.False(f.Registry.IsValidated(B));
        Assert.False(f.Registry.IsValidated(A with { ModeFingerprint = "hdr" }));
        Assert.False(f.Registry.IsValidated(A with { TopologyFingerprint = "other" }));
        Assert.True(f.NewRegistry().IsValidated(A));
        Assert.Contains(f.Service.Capabilities, c => c.Scope == A && c.State == ColorCapabilityState.Supported);
        Assert.False(f.Service.Settings.Enabled);
        Assert.Empty(f.Service.Settings.ParticipatingScreens);
    }

    [Fact]
    public async Task CancellationRestoresButCannotQualify()
    {
        var f = new Fixture(); using var cancel = new CancellationTokenSource();
        var result = await f.Service.RunValidationAsync("a", cancellationToken: cancel.Token, delay: (_, token) => { cancel.Cancel(); token.ThrowIfCancellationRequested(); return Task.CompletedTask; });
        Assert.False(result.CanConfirm); Assert.True(Original().SameAs(f.Backend.Values["a"]));
        Assert.Empty(f.Store.Document.Records); Assert.False(f.Registry.IsValidated(A)); Assert.False(f.Backend.Leased);
    }

    [Fact]
    public async Task FailedDurableIntentNeverWritesHardware()
    {
        var f = new Fixture(); f.Store.FailWrites = true;
        Assert.False((await f.Run()).CanConfirm);
        Assert.Equal(0, f.Backend.Writes); Assert.False(f.Registry.IsValidated(A));
    }

    [Fact]
    public async Task FailedRestoreRetainsJournalAndRecoveryDoesNotRequireQualification()
    {
        var f = new Fixture(); f.Backend.FailRestore = true;
        Assert.False((await f.Run()).CanConfirm);
        Assert.Single(f.Store.Document.Records); Assert.False(f.Registry.IsValidated(A));
        f.Backend.FailRestore = false;
        await f.Service.StopAsync();
        Assert.True(Original().SameAs(f.Backend.Values["a"])); Assert.Empty(f.Store.Document.Records);
    }

    [Fact]
    public async Task PriorInterruptedTrialCanRestoreOnStartupWithoutPermanentPermission()
    {
        var f = new Fixture(); var warm = GammaTarget.Create(Original(), 5000, 1); f.Backend.Values["a"] = warm;
        f.Store.Document.Records.Add(new() { Scope = A, SessionId = "previous-process", Baseline = Original(), LastApplied = warm });
        await f.Service.ApplySettingsAsync(new());
        Assert.True(Original().SameAs(f.Backend.Values["a"])); Assert.Empty(f.Store.Document.Records);
        Assert.False(f.Registry.IsValidated(A)); Assert.Equal(1, f.Backend.Writes);
    }

    [Fact]
    public async Task ExternalChangeIsNotOverwrittenAndPreventsConfirmation()
    {
        var f = new Fixture(); ColorValue? external = null;
        var result = await f.Service.RunValidationAsync("a", delay: (_, _) =>
        {
            external = f.Backend.Values["a"].Copy(); external.Channels[400] = 17;
            f.Backend.Values["a"] = external.Copy(); return Task.CompletedTask;
        });
        Assert.False(result.CanConfirm); Assert.Equal(1, f.Backend.Writes);
        Assert.True(external!.SameAs(f.Backend.Values["a"])); Assert.True(Assert.Single(f.Store.Document.Records).Conflicted);
    }

    [Fact]
    public async Task OtherScreenChangingRejectsEvenWhenTargetRestores()
    {
        var f = new Fixture(); f.Backend.SharedRamp = true;
        Assert.False((await f.Run()).CanConfirm); Assert.False(f.Registry.IsValidated(A));
        Assert.True(Original().SameAs(f.Backend.Values["a"])); Assert.True(Original().SameAs(f.Backend.Values["b"]));
    }

    [Fact]
    public async Task ModeChangeDuringTrialNeverWritesAnOldBaselineIntoNewMode()
    {
        var f = new Fixture();
        var result = await f.Service.RunValidationAsync("a", delay: (_, _) => { f.Backend.CurrentA = A with { ModeFingerprint = "new-mode" }; return Task.CompletedTask; });
        Assert.False(result.CanConfirm); Assert.Equal(1, f.Backend.Writes); Assert.Single(f.Store.Document.Records);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ChangedModeOrExternalColorAfterTrialInvalidatesObservation(bool mode)
    {
        var f = new Fixture(); var result = await f.Run();
        if (mode) f.Backend.CurrentA = A with { ModeFingerprint = "new-mode" }; else f.Backend.Values["a"].Channels[400] = 17;
        Assert.False((await f.Service.ConfirmValidationAsync(result.Ticket, f.Registry)).CanConfirm);
        Assert.False(f.Registry.IsValidated(A));
    }

    [Fact]
    public async Task ConfirmationSaveFailureDoesNotGrantSessionPermission()
    {
        var f = new Fixture(); var result = await f.Run(); f.Fs.WritesFail = true;
        Assert.False((await f.Service.ConfirmValidationAsync(result.Ticket, f.Registry)).CanConfirm);
        Assert.False(f.Registry.IsValidated(A));
    }

    [Fact]
    public async Task ConfirmationRequiresFreshSingleUseEvidence()
    {
        var f = new Fixture(); Assert.False((await f.Service.ConfirmValidationAsync(Guid.NewGuid(), f.Registry)).CanConfirm);
        var result = await f.Run(); f.Now = f.Now.AddMinutes(6);
        Assert.False((await f.Service.ConfirmValidationAsync(result.Ticket, f.Registry)).CanConfirm); Assert.False(f.Registry.IsValidated(A));
    }

    [Fact]
    public async Task StopCancelsWaitingTrialThenWaitsForRecovery()
    {
        var f = new Fixture(); var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var trial = f.Service.RunValidationAsync("a", delay: async (_, token) => { entered.TrySetResult(); await Task.Delay(Timeout.Infinite, token); });
        await entered.Task.WaitAsync(TimeSpan.FromSeconds(3));
        await f.Service.StopAsync().WaitAsync(TimeSpan.FromSeconds(3));
        Assert.False((await trial).CanConfirm); Assert.Empty(f.Store.Document.Records); Assert.True(Original().SameAs(f.Backend.Values["a"]));
    }

    [Fact]
    public async Task EnabledPolicyAndUnsupportedModeDoNotStartTrial()
    {
        var f = new Fixture(); await f.Service.ApplySettingsAsync(new() { Enabled = true });
        Assert.Equal("请先关闭色温调节并保存，再进行验证。", f.Service.GetValidationBlockReason("a"));
        var blocked = await f.Run();
        Assert.False(blocked.CanConfirm); Assert.Equal(f.Service.GetValidationBlockReason("a"), blocked.Message); Assert.Equal(0, f.Backend.Writes);
        await f.Service.ApplySettingsAsync(new()); f.Backend.Advanced = true;
        Assert.False((await f.Run()).CanConfirm); Assert.Equal(0, f.Backend.Writes);
    }

    [Fact]
    public async Task ValidationExplainsRemainingRecoveryAndRecheckOnlyClearsChosenScreen()
    {
        var f = new Fixture();
        foreach (var scope in new[] { A, B }) f.Store.Document.Records.Add(new() { Scope = scope, SessionId = "old", Baseline = Original(), LastApplied = Original(), Conflicted = true });
        await f.Service.RefreshAsync();
        Assert.Equal("有屏幕的原色记录尚未处理。请先处理恢复记录，再开始验证。", f.Service.GetValidationBlockReason("a"));
        Assert.False((await f.Run()).CanConfirm); Assert.Equal(0, f.Backend.Writes);
        await f.Service.RecheckOriginalAsync("a");
        Assert.Equal("b", Assert.Single(f.Store.Document.Records).Scope.HardwareId);
        Assert.NotNull(f.Service.GetValidationBlockReason("a"));
        await f.Service.RecheckOriginalAsync("b");
        Assert.Null(f.Service.GetValidationBlockReason("a")); Assert.Equal(0, f.Backend.Writes);
    }

    [Fact]
    public void UnknownRegistrySchemaIsKeptAndNeverBypassed()
    {
        var f = new Fixture(); var path = @"C:\FakeAppData\ScreenKeeper\color-temperature-validation.json";
        f.Fs.AddFile(path, """{"Schema":99}"""); var registry = f.NewRegistry();
        Assert.False(registry.IsAvailable); Assert.False(registry.TryConfirm(A)); Assert.Contains("99", f.Fs.Contents(path));
    }

    private sealed class Fixture
    {
        public FakeFileSystem Fs { get; } = new();
        public Store Store { get; } = new();
        public ColorValidationRegistry Registry { get; }
        public Backend Backend { get; }
        public ColorTemperatureService Service { get; }
        public DateTimeOffset Now = new(2026, 9, 18, 20, 0, 0, TimeSpan.Zero);
        public Fixture()
        {
            Registry = NewRegistry(); Backend = new(Store, Registry); Service = new(Backend, Store, () => Now);
        }
        public ColorValidationRegistry NewRegistry()
        {
            var root = new StorageRoot(Fs, false);
            return new(new IndependentSettingsStore<ColorValidationDocument>(new AppDataFileStore(Fs, root), Fs, root, "color-temperature-validation.json", () => new(), s => s.Validate()));
        }
        public Task<ColorValidationResult> Run() => Service.RunValidationAsync("a", delay: NoDelay);
    }

    private sealed class Backend(Store store, IColorValidationRegistry registry) : IColorTrialBackend
    {
        public ColorDisplayScope CurrentA = A;
        public Dictionary<string, ColorValue> Values = new() { ["a"] = Original(), ["b"] = Original() };
        public bool Leased, FailRestore, SharedRamp, Advanced;
        public int Writes;
        public Task<IReadOnlyList<ColorCapability>> DiscoverAsync(CancellationToken token) => Task.FromResult<IReadOnlyList<ColorCapability>>(
            [new(CurrentA, registry.IsValidated(CurrentA) ? ColorCapabilityState.Supported : ColorCapabilityState.Unverified, "test", Advanced, false, true), new(B, ColorCapabilityState.Unverified, "test", false, false, true)]);
        public Task<ColorValue?> ReadAsync(ColorDisplayScope scope, CancellationToken token) => Task.FromResult<ColorValue?>(
            WindowsGammaBackend.SameScope(scope, scope.HardwareId == "a" ? CurrentA : B) ? Values[scope.HardwareId].Copy() : null);
        public Task<bool> WriteAsync(ColorDisplayScope scope, ColorValue value, CancellationToken token)
        {
            Assert.True(Leased || registry.IsValidated(scope));
            Assert.NotNull(Assert.Single(store.Document.Records).PendingValue);
            Values[scope.HardwareId] = value.Copy(); Writes++;
            if (SharedRamp) Values["b"] = value.Copy();
            return Task.FromResult(true);
        }
        public Task<bool> RestoreAsync(ColorDisplayScope scope, ColorValue value, CancellationToken token)
        {
            if (FailRestore) return Task.FromResult(false);
            Assert.True(WindowsGammaBackend.SameScope(scope, CurrentA)); Assert.True(Original().SameAs(value));
            Values[scope.HardwareId] = value.Copy(); Writes++;
            if (SharedRamp) Values["b"] = value.Copy();
            return Task.FromResult(true);
        }
        public ColorValue CreateTarget(ColorValue baseline, int kelvin, double warmth) => GammaTarget.Create(baseline, kelvin, warmth);
        public IDisposable AuthorizeTrial(ColorDisplayScope scope) { Assert.Equal(A, scope); Leased = true; return new Lease(() => Leased = false); }
        private sealed class Lease(Action close) : IDisposable { public void Dispose() => close(); }
    }

    private sealed class Store : IColorRecoveryStore
    {
        public ColorRecoveryDocument Document = new();
        public bool FailWrites;
        public ColorRecoveryReadResult Read() => new(Document.Copy(), true);
        public bool TryWrite(ColorRecoveryDocument document) { if (FailWrites) return false; Document = document.Copy(); return true; }
    }
}
