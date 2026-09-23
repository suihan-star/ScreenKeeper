using OLED_Sleeper.Features.ColorTemperature;
using OLED_Sleeper.Storage;
using OLED_Sleeper.Tests.TestDoubles;

namespace OLED_Sleeper.Tests.Features.ColorTemperature;

public sealed class ColorBaselineResetTests
{
    private static readonly ColorDisplayScope A = new("a", "physical-a", "DISPLAY-A", "Example A", "topology", "sdr");
    private static readonly ColorDisplayScope B = new("b", "physical-b", "DISPLAY-B", "Example B", "topology", "sdr");
    private static ColorValue Linear() => new() { Channels = Enumerable.Range(0, 768).Select(i => (ushort)(i % 256 * 257)).ToArray() };
    private static Task NoDelay(TimeSpan _, CancellationToken token) { token.ThrowIfCancellationRequested(); return Task.CompletedTask; }

    [Fact]
    public async Task ReconnectedSamePhysicalScreenCanArchiveOldScopeWithoutWritingItsOldRamp()
    {
        var f = new Fixture();
        f.Backend.CurrentA = A with { DeviceName = "DISPLAY-NEW", TopologyFingerprint = "reconnected", ModeFingerprint = "new-sdr-mode" };
        var check = await f.Prepare();
        Assert.True(check.Ready);
        Assert.Empty(f.Store.Archives);
        Assert.Equal(0, f.Backend.Writes);

        Assert.True((await f.Service.ConfirmBaselineResetAsync(check.Ticket, f.Registry)).Ready);
        var archive = Assert.Single(f.Store.Archives);
        Assert.Equal(A, archive.Previous.Records[0].Scope);
        Assert.Equal(f.Backend.CurrentA, archive.ConfirmedColors[0].Scope);
        Assert.Empty(f.Store.Document.Records);
        Assert.Contains("a", f.Store.Document.ValidationRequiredScreens);
        Assert.False(f.Registry.IsValidated(A));
        Assert.False(f.Registry.IsValidated(f.Backend.CurrentA));
        Assert.Equal(0, f.Backend.Writes);
    }

    [Theory]
    [InlineData("physical")]
    [InlineData("identity")]
    [InlineData("duplicate")]
    [InlineData("cloned")]
    [InlineData("hdr")]
    [InlineData("offline")]
    public async Task NewBaselineNeverGuessesPhysicalIdentityOrAcceptsUnsupportedMode(string condition)
    {
        var f = new Fixture();
        switch (condition)
        {
            case "physical": f.Backend.CurrentA = A with { ScopeId = "different-physical-screen" }; break;
            case "identity": f.Backend.CurrentA = A with { HardwareId = "other" }; break;
            case "duplicate": f.Backend.Duplicate = true; break;
            case "cloned": f.Backend.Cloned = true; break;
            case "hdr": f.Backend.Hdr = true; break;
            case "offline": f.Backend.Offline = true; break;
        }
        Assert.False((await f.Prepare()).Ready);
        Assert.Empty(f.Store.Archives);
        Assert.Equal(2, f.Store.Document.Records.Count);
        Assert.Equal(0, f.Backend.Writes);
    }

    [Fact]
    public async Task ConfirmedCurrentColorsArchiveOldWarmBaselineWithoutWritingAndRequireFreshVerification()
    {
        var f = new Fixture(); var old = f.Store.Document.Copy();
        var unrelated = A with { HardwareId = "unrelated", ScopeId = "unrelated-path" };
        Assert.True(f.Registry.Inner.TryConfirm(unrelated));
        var check = await f.Prepare();
        Assert.True(check.Ready); Assert.Equal(new[] { "Example A", "Example B" }, check.DisplayNames);
        Assert.Empty(f.Events); Assert.Equal(2, f.Store.Document.Records.Count); Assert.True(f.Registry.IsValidated(A));
        var result = await f.Service.ConfirmBaselineResetAsync(check.Ticket, f.Registry);
        Assert.True(result.Ready); Assert.Equal(new[] { "archive", "revoke", "write" }, f.Events);
        var archive = Assert.Single(f.Store.Archives);
        Assert.True(old.Records[0].Baseline.SameAs(archive.Previous.Records[0].Baseline));
        Assert.All(archive.ConfirmedColors, color => Assert.True(Linear().SameAs(color.Value)));
        Assert.Empty(f.Store.Document.Records); Assert.Equal(new[] { "a", "b" }, f.Store.Document.ValidationRequiredScreens);
        Assert.False(f.NewRegistry().IsValidated(A)); Assert.False(f.Registry.IsValidated(B));
        Assert.True(f.NewRegistry().IsValidated(unrelated));
        Assert.False(f.Service.Settings.Enabled); Assert.Equal(0, f.Backend.Writes);
        Assert.False((await f.Service.ConfirmBaselineResetAsync(check.Ticket, f.Registry)).Ready);

        // Even an older registry backup must not resurrect qualification while the durable reset marker exists.
        Assert.True(f.Registry.Inner.TryConfirm(A));
        var restarted = new ColorTemperatureService(f.Backend, f.Store);
        await restarted.RefreshAsync();
        Assert.Equal(ColorCapabilityState.Unverified, restarted.Capabilities.Single(c => c.Scope.HardwareId == "a").State);
        Assert.False(await restarted.PreviewAsync("a", 5300)); Assert.Equal(0, f.Backend.Writes);
        var trial = await restarted.RunValidationAsync("a", delay: NoDelay);
        Assert.True(trial.CanConfirm); Assert.Equal(2, f.Backend.Writes);
        Assert.True((await restarted.ConfirmValidationAsync(trial.Ticket, f.Registry)).CanConfirm);
        Assert.Equal("b", Assert.Single(f.Store.Document.ValidationRequiredScreens));
        Assert.Equal(ColorCapabilityState.Supported, restarted.Capabilities.Single(c => c.Scope.HardwareId == "a").State);
        Assert.True(Linear().SameAs(f.Backend.Values["a"]));
    }

    [Theory]
    [InlineData("archive")]
    [InlineData("revoke")]
    [InlineData("journal")]
    public async Task PersistenceFailuresKeepActiveRecordsAndNeverWriteDevices(string stage)
    {
        var f = new Fixture(); var check = await f.Prepare();
        f.Store.ArchiveFails = stage == "archive"; f.Files.WritesFail = stage == "revoke"; f.Store.WriteFails = stage == "journal";
        Assert.False((await f.Service.ConfirmBaselineResetAsync(check.Ticket, f.Registry)).Ready);
        Assert.Equal(2, f.Store.Document.Records.Count); Assert.Equal(0, f.Backend.Writes);
        Assert.Equal(stage == "archive" ? 0 : 1, f.Store.Archives.Count);
        if (stage == "archive") Assert.DoesNotContain("revoke", f.Events);
        if (stage == "journal") Assert.False(f.Registry.IsValidated(A));
        else Assert.True(f.Registry.IsValidated(A));
        Assert.False((await f.Service.ConfirmBaselineResetAsync(check.Ticket, f.Registry)).Ready);
    }

    [Theory]
    [InlineData("colors")]
    [InlineData("mode")]
    [InlineData("offline")]
    [InlineData("read")]
    [InlineData("hdr")]
    [InlineData("expired")]
    [InlineData("cancel")]
    [InlineData("enabled")]
    [InlineData("suspended")]
    [InlineData("settings-race")]
    [InlineData("unavailable-registry")]
    public async Task AChangedConfirmationCannotArchiveOrUnlock(string condition)
    {
        var f = new Fixture(); var check = await f.Prepare(); Task? queued = null;
        switch (condition)
        {
            case "colors": f.Backend.Values["a"].Channels[400] = 19; break;
            case "mode": f.Backend.CurrentA = A with { ModeFingerprint = "other" }; break;
            case "offline": f.Backend.Offline = true; break;
            case "read": f.Backend.ReadFails = true; break;
            case "hdr": f.Backend.Hdr = true; break;
            case "expired": f.Monotonic += 300_000; break;
            case "cancel": f.Service.CancelBaselineReset(); break;
            case "enabled": await f.Service.ApplySettingsAsync(new() { Enabled = true }); break;
            case "suspended": await f.Service.SetSuspendedAsync(true); break;
            case "settings-race": f.Backend.OnRead = () => { f.Backend.OnRead = null; queued = f.Service.ApplySettingsAsync(new() { Enabled = true }); }; break;
            case "unavailable-registry": f.Registry.Unavailable = true; break;
        }
        Assert.False((await f.Service.ConfirmBaselineResetAsync(check.Ticket, f.Registry)).Ready);
        if (queued != null) await queued;
        Assert.Empty(f.Store.Archives); Assert.Equal(2, f.Store.Document.Records.Count); Assert.Equal(0, f.Backend.Writes);
    }

    [Theory]
    [InlineData("unsafe")]
    [InlineData("no-records")]
    [InlineData("enabled")]
    [InlineData("unstable")]
    [InlineData("cancelled")]
    public async Task PreparationNeverModifiesRecoveryAndRejectsUnsafeState(string condition)
    {
        var f = new Fixture();
        if (condition == "unsafe") f.Store.Safe = false;
        if (condition == "no-records") f.Store.Document.Records.Clear();
        if (condition == "enabled") await f.Service.ApplySettingsAsync(new() { Enabled = true });
        var result = await f.Service.PrepareBaselineResetAsync(delay: (_, _) =>
        {
            if (condition == "unstable") f.Backend.Values["b"].Channels[400] = 18;
            if (condition == "cancelled") f.Service.CancelBaselineReset();
            return Task.CompletedTask;
        });
        Assert.False(result.Ready); Assert.Empty(f.Events); Assert.Equal(0, f.Backend.Writes);
    }

    [Fact]
    public async Task FailedMarkerRemovalCannotRestoreQualificationAfterSuccessfulTrial()
    {
        var f = new Fixture(); var check = await f.Prepare();
        Assert.True((await f.Service.ConfirmBaselineResetAsync(check.Ticket, f.Registry)).Ready);
        var trial = await f.Service.RunValidationAsync("a", delay: NoDelay); Assert.True(trial.CanConfirm);
        f.Store.WriteFails = true;
        Assert.False((await f.Service.ConfirmValidationAsync(trial.Ticket, f.Registry)).CanConfirm);
        await f.Service.RefreshAsync();
        Assert.Equal(ColorCapabilityState.Unverified, f.Service.Capabilities.Single(c => c.Scope.HardwareId == "a").State);
        Assert.Contains("a", f.Store.Document.ValidationRequiredScreens);
    }

    private sealed class Fixture
    {
        public FakeFileSystem Files = new();
        public List<string> Events = [];
        public Store Store;
        public Registry Registry;
        public Backend Backend;
        public ColorTemperatureService Service;
        public long Monotonic;
        public Fixture()
        {
            Registry = new(NewRegistry(), Events); Assert.True(Registry.Inner.TryConfirm(A));
            Store = new(Events);
            foreach (var scope in new[] { A, B }) Store.Document.Records.Add(new() { Scope = scope, SessionId = "old", Baseline = GammaTarget.Create(Linear(), 5000, 1), LastApplied = GammaTarget.Create(Linear(), 4000, 1), Conflicted = true });
            Backend = new(Registry); Service = new(Backend, Store, monotonicMilliseconds: () => Monotonic);
        }
        public ColorValidationRegistry NewRegistry()
        {
            var root = new StorageRoot(Files, false);
            return new(new IndependentSettingsStore<ColorValidationDocument>(new AppDataFileStore(Files, root), Files, root, "color-temperature-validation.json", () => new(), s => s.Validate()));
        }
        public Task<ColorBaselineResetCheck> Prepare() => Service.PrepareBaselineResetAsync(delay: NoDelay);
    }
    private sealed class Registry(ColorValidationRegistry inner, List<string> events) : IColorValidationRegistry
    {
        public ColorValidationRegistry Inner => inner;
        public bool Unavailable;
        public bool IsAvailable => !Unavailable && inner.IsAvailable;
        public bool IsValidated(ColorDisplayScope scope) => inner.IsValidated(scope);
        public bool TryConfirm(ColorDisplayScope scope) => inner.TryConfirm(scope);
        public bool TryRevoke(IReadOnlyCollection<string> ids) { events.Add("revoke"); return inner.TryRevoke(ids); }
    }
    private sealed class Store(List<string> events) : IColorRecoveryStore
    {
        public ColorRecoveryDocument Document = new();
        public List<ColorRecoveryArchive> Archives = [];
        public bool Safe = true, ArchiveFails, WriteFails;
        public ColorRecoveryReadResult Read() => new(Document.Copy(), Safe);
        public bool TryArchive(ColorRecoveryArchive archive) { events.Add("archive"); if (ArchiveFails) return false; Archives.Add(archive.Copy()); return true; }
        public bool TryWrite(ColorRecoveryDocument document) { events.Add("write"); if (WriteFails) return false; Document = document.Copy(); return true; }
    }
    private sealed class Backend(IColorValidationRegistry registry) : IColorTrialBackend
    {
        public ColorDisplayScope CurrentA = A;
        public Dictionary<string, ColorValue> Values = new() { ["a"] = Linear(), ["b"] = Linear() };
        public bool Offline, ReadFails, Hdr, Duplicate, Cloned;
        public Action? OnRead;
        public int Writes;
        private bool _leased;
        public Task<IReadOnlyList<ColorCapability>> DiscoverAsync(CancellationToken token) => Task.FromResult<IReadOnlyList<ColorCapability>>(
            Offline ? [] : (Duplicate ? new[] { CurrentA, CurrentA, B } : new[] { CurrentA, B }).Select(s => new ColorCapability(s, registry.IsValidated(s) ? ColorCapabilityState.Supported : ColorCapabilityState.Unverified, "fixture", Hdr, Cloned, true)).ToArray());
        public Task<ColorValue?> ReadAsync(ColorDisplayScope scope, CancellationToken token)
        {
            token.ThrowIfCancellationRequested(); OnRead?.Invoke();
            return Task.FromResult(ReadFails || !WindowsGammaBackend.SameScope(scope, scope.HardwareId == "a" ? CurrentA : B) ? null : Values[scope.HardwareId].Copy());
        }
        public Task<bool> WriteAsync(ColorDisplayScope scope, ColorValue value, CancellationToken token)
        {
            Assert.True(_leased || registry.IsValidated(scope)); Values[scope.HardwareId] = value.Copy(); Writes++; return Task.FromResult(true);
        }
        public Task<bool> RestoreAsync(ColorDisplayScope scope, ColorValue value, CancellationToken token)
        { Values[scope.HardwareId] = value.Copy(); Writes++; return Task.FromResult(true); }
        public ColorValue CreateTarget(ColorValue baseline, int kelvin, double warmth) => GammaTarget.Create(baseline, kelvin, warmth);
        public IDisposable AuthorizeTrial(ColorDisplayScope scope) { _leased = true; return new Lease(() => _leased = false); }
        private sealed class Lease(Action close) : IDisposable { public void Dispose() => close(); }
    }
}
