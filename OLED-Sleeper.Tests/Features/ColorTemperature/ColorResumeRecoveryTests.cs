using OLED_Sleeper.Features.ColorTemperature;
using OLED_Sleeper.Storage;
using OLED_Sleeper.Storage.Interfaces;
using System.IO;

namespace OLED_Sleeper.Tests.Features.ColorTemperature;

public sealed class ColorResumeRecoveryTests
{
    private static readonly ColorDisplayScope Scope = new("resume-fixture", "physical-fixture", "DISPLAY-FIXTURE", "Example", "topology", "sdr-mode");
    private static ColorValue Baseline() => new() { Channels = Enumerable.Range(0, 768).Select(i => (ushort)(i % 256 * 257)).ToArray() };
    private static ColorTemperatureSettings Settings(int kelvin = 4500) => new()
    {
        Schema = 2,
        ScreenProfiles = new()
        {
            [Scope.HardwareId] = new()
            {
                Enabled = true,
                DayUsesOriginal = false,
                NightUsesOriginal = false,
                DayKelvin = kelvin,
                NightKelvin = kelvin,
                TransitionMinutes = 0
            }
        }
    };

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ResumeObservesKnownColorForTwoSecondsBeforeFollowingSavedPolicy(bool resetToBaseline)
    {
        await using var fixture = new Fixture();
        await fixture.WarmAsync();
        var original = fixture.Record;
        await fixture.Service.SetSuspendedAsync(true);
        await fixture.Service.ApplySettingsAsync(Settings(5300));
        if (resetToBaseline) fixture.Backend.Value = original.Baseline.Copy();
        int writes = fixture.Backend.Writes.Count;

        await fixture.Service.SetSuspendedAsync(false);
        Assert.Equal(writes, fixture.Backend.Writes.Count);
        Assert.False(fixture.Record.Conflicted);
        fixture.Advance(1999);
        await fixture.Service.RefreshAsync();
        Assert.Equal(writes, fixture.Backend.Writes.Count);
        fixture.Advance(1);
        await fixture.Service.RefreshAsync();
        Assert.Equal(writes, fixture.Backend.Writes.Count);

        fixture.Advance(ColorTransition.DurationMilliseconds);
        await fixture.Service.RefreshAsync();
        Assert.Equal(ColorControlState.Warm, fixture.State);
        Assert.False(fixture.Record.Conflicted);
        Assert.True(original.Baseline.SameAs(fixture.Record.Baseline));
        Assert.True(GammaTarget.Create(original.Baseline, 5300, 1).SameAs(fixture.Backend.Value));
        Assert.Empty(fixture.Document.ValidationRequiredScreens);
    }

    [Fact]
    public async Task UnknownColorAfterUnlockImmediatelyBecomesConflictAndIsNeverOverwritten()
    {
        await using var fixture = new Fixture();
        await fixture.WarmAsync();
        var original = fixture.Record;
        await fixture.Service.SetSuspendedAsync(true);
        fixture.Backend.Value.Channels[400] = 17;
        var unknown = fixture.Backend.Value.Copy();
        int writes = fixture.Backend.Writes.Count;
        await fixture.Service.SetSuspendedAsync(false);

        Assert.Equal(ColorControlState.Conflict, fixture.State);
        Assert.True(fixture.Record.Conflicted);
        fixture.Advance(30_000);
        await fixture.Service.RefreshAsync();
        Assert.Equal(writes, fixture.Backend.Writes.Count);
        Assert.True(unknown.SameAs(fixture.Backend.Value));
        Assert.True(original.Baseline.SameAs(fixture.Record.Baseline));
        Assert.True(original.LastApplied.SameAs(fixture.Record.LastApplied));
    }

    [Fact]
    public async Task BaselineChangeWithoutSessionResumeStillUsesNormalConflictProtection()
    {
        await using var fixture = new Fixture();
        await fixture.WarmAsync();
        fixture.Backend.Value = fixture.Record.Baseline.Copy();
        int writes = fixture.Backend.Writes.Count;
        await fixture.Service.RefreshAsync();
        Assert.Equal(ColorControlState.Conflict, fixture.State);
        Assert.True(fixture.Record.Conflicted);
        Assert.Equal(writes, fixture.Backend.Writes.Count);
    }

    [Theory]
    [InlineData("mode")]
    [InlineData("topology")]
    [InlineData("device")]
    [InlineData("physical")]
    [InlineData("unverified")]
    [InlineData("unsupported")]
    public async Task UnavailableOrChangedScopeCannotReuseAnEarlierStableObservation(string change)
    {
        await using var fixture = new Fixture();
        await fixture.WarmAsync();
        await fixture.Service.SetSuspendedAsync(true);
        fixture.Backend.Value = fixture.Record.Baseline.Copy();
        int writes = fixture.Backend.Writes.Count;
        await fixture.Service.SetSuspendedAsync(false);
        fixture.Advance(1000);
        fixture.Backend.Capability = change switch
        {
            "mode" => fixture.Backend.Capability with { Scope = Scope with { ModeFingerprint = "changed" } },
            "topology" => fixture.Backend.Capability with { Scope = Scope with { TopologyFingerprint = "changed" } },
            "device" => fixture.Backend.Capability with { Scope = Scope with { DeviceName = "DISPLAY-CHANGED" } },
            "physical" => fixture.Backend.Capability with { Scope = Scope with { ScopeId = "different-physical" } },
            "unverified" => fixture.Backend.Capability with { State = ColorCapabilityState.Unverified },
            _ => fixture.Backend.Capability with { State = ColorCapabilityState.Unsupported }
        };
        await fixture.Service.RefreshAsync();
        fixture.Advance(5000);
        await fixture.Service.RefreshAsync();
        Assert.Equal(writes, fixture.Backend.Writes.Count);
        Assert.False(fixture.Record.Conflicted);
        Assert.True(Baseline().SameAs(fixture.Record.Baseline));

        fixture.Backend.Capability = Backend.Qualified;
        await fixture.Service.RefreshAsync();
        fixture.Advance(1999);
        await fixture.Service.RefreshAsync();
        Assert.Equal(writes, fixture.Backend.Writes.Count);
        fixture.Advance(1);
        await fixture.Service.RefreshAsync();
        Assert.Equal(writes, fixture.Backend.Writes.Count);
        fixture.Advance(ColorTransition.DurationMilliseconds);
        await fixture.Service.RefreshAsync();
        Assert.Equal(ColorControlState.Warm, fixture.State);
        Assert.False(fixture.Record.Conflicted);
    }

    [Theory]
    [InlineData("null-read")]
    [InlineData("baseline-to-applied")]
    [InlineData("applied-to-baseline")]
    public async Task MissingReadOrKnownColorChangeRestartsStableObservation(string interruption)
    {
        await using var fixture = new Fixture();
        await fixture.WarmAsync();
        var original = fixture.Record;
        await fixture.Service.SetSuspendedAsync(true);
        await fixture.Service.ApplySettingsAsync(Settings(5300));
        fixture.Backend.Value = interruption == "applied-to-baseline" ? original.LastApplied.Copy() : original.Baseline.Copy();
        int writes = fixture.Backend.Writes.Count;
        await fixture.Service.SetSuspendedAsync(false);
        fixture.Advance(1000);
        if (interruption == "null-read") fixture.Backend.ReadFails = true;
        else fixture.Backend.Value = interruption == "baseline-to-applied" ? original.LastApplied.Copy() : original.Baseline.Copy();
        await fixture.Service.RefreshAsync();
        if (interruption == "null-read")
        {
            fixture.Backend.ReadFails = false;
            await fixture.Service.RefreshAsync();
        }
        fixture.Advance(1999);
        await fixture.Service.RefreshAsync();
        Assert.Equal(writes, fixture.Backend.Writes.Count);
        Assert.False(fixture.Record.Conflicted);
        fixture.Advance(1);
        await fixture.Service.RefreshAsync();
        Assert.Equal(writes, fixture.Backend.Writes.Count);
        fixture.Advance(ColorTransition.DurationMilliseconds);
        await fixture.Service.RefreshAsync();
        Assert.Equal(ColorControlState.Warm, fixture.State);
        Assert.True(GammaTarget.Create(original.Baseline, 5300, 1).SameAs(fixture.Backend.Value));
    }

    [Fact]
    public async Task ExistingConflictCannotBeClearedByAnotherLockCycleEvenAtExactBaseline()
    {
        await using var fixture = new Fixture();
        await fixture.WarmAsync();
        fixture.Backend.Value.Channels[400] = 17;
        await fixture.Service.RefreshAsync();
        Assert.True(fixture.Record.Conflicted);
        int writes = fixture.Backend.Writes.Count;
        await fixture.Service.SetSuspendedAsync(true);
        fixture.Backend.Value = fixture.Record.Baseline.Copy();
        await fixture.Service.SetSuspendedAsync(false);
        fixture.Advance(30_000);
        await fixture.Service.RefreshAsync();
        Assert.True(fixture.Record.Conflicted);
        Assert.Equal(writes, fixture.Backend.Writes.Count);
        Assert.True(Baseline().SameAs(fixture.Backend.Value));
    }

    [Fact]
    public async Task FailedPendingWriteIsNotAcceptedAsSuccessfulOwnershipAfterResume()
    {
        await using var fixture = new Fixture();
        await fixture.WarmAsync();
        fixture.Backend.FailAfterWrite = true;
        await fixture.Service.ApplySettingsAsync(Settings(5300));
        fixture.Advance(ColorTransition.DurationMilliseconds);
        await fixture.Service.RefreshAsync();
        var failed = fixture.Record;
        Assert.NotNull(failed.PendingValue);
        Assert.True(failed.PendingValue.SameAs(fixture.Backend.Value));
        Assert.False(failed.LastApplied.SameAs(fixture.Backend.Value));
        int writes = fixture.Backend.Writes.Count;
        fixture.Backend.FailAfterWrite = false;
        await fixture.Service.SetSuspendedAsync(true);
        await fixture.Service.SetSuspendedAsync(false);
        fixture.Advance(30_000);
        await fixture.Service.RefreshAsync();
        Assert.Equal(writes, fixture.Backend.Writes.Count);
        Assert.NotNull(fixture.Record.PendingValue);
        Assert.True(failed.LastApplied.SameAs(fixture.Record.LastApplied));
    }

    [Fact]
    public async Task FailedDurableResumeUpdatePreventsAnyFurtherColorWrite()
    {
        var store = new Store();
        await using var fixture = new Fixture(store);
        await fixture.WarmAsync();
        var original = fixture.Record;
        await fixture.Service.SetSuspendedAsync(true);
        fixture.Backend.Value = original.Baseline.Copy();
        int writes = fixture.Backend.Writes.Count;
        await fixture.Service.SetSuspendedAsync(false);
        store.FailWrites = true;
        fixture.Advance(2000);
        await fixture.Service.RefreshAsync();
        fixture.Advance(30_000);
        await fixture.Service.RefreshAsync();
        Assert.Equal(writes, fixture.Backend.Writes.Count);
        Assert.True(original.Baseline.SameAs(fixture.Backend.Value));
        Assert.True(original.LastApplied.SameAs(fixture.Record.LastApplied));
        Assert.False(fixture.Record.Conflicted);
        Assert.NotNull(fixture.Service.LastError);
    }

    [Fact]
    public async Task ASeparateReadAfterConfirmationStillRejectsAnExternalChange()
    {
        await using var fixture = new Fixture();
        await fixture.WarmAsync();
        var original = fixture.Record;
        await fixture.Service.SetSuspendedAsync(true);
        fixture.Backend.Value = original.Baseline.Copy();
        int writes = fixture.Backend.Writes.Count;
        await fixture.Service.SetSuspendedAsync(false);
        fixture.Advance(2000);
        fixture.Backend.AfterRead = () =>
        {
            fixture.Backend.AfterRead = null;
            fixture.Backend.Value.Channels[400] = 17;
        };
        await fixture.Service.RefreshAsync();
        await fixture.Service.RefreshAsync();
        Assert.Equal(writes, fixture.Backend.Writes.Count);
        Assert.True(fixture.Record.Conflicted);
        Assert.True(original.Baseline.SameAs(fixture.Record.Baseline));
        Assert.Equal((ushort)17, fixture.Backend.Value.Channels[400]);
    }

    [Fact]
    public async Task ANewLockDuringResumeReadCancelsAcceptanceAndWriting()
    {
        await using var fixture = new Fixture();
        await fixture.WarmAsync();
        await fixture.Service.SetSuspendedAsync(true);
        fixture.Backend.Value = fixture.Record.Baseline.Copy();
        await fixture.Service.SetSuspendedAsync(false);
        int writes = fixture.Backend.Writes.Count;
        fixture.Advance(2000);
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        fixture.Backend.BeforeRead = async () => { entered.TrySetResult(); await release.Task; };
        var refresh = fixture.Service.RefreshAsync();
        Task? suspend = null;
        try
        {
            await entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
            suspend = fixture.Service.SetSuspendedAsync(true);
        }
        finally
        {
            release.TrySetResult();
            await refresh;
            if (suspend != null) await suspend;
            fixture.Backend.BeforeRead = null;
        }
        Assert.Equal(writes, fixture.Backend.Writes.Count);
        Assert.False(fixture.Record.Conflicted);
        Assert.Equal(ColorControlState.Paused, fixture.State);
        await fixture.Service.SetSuspendedAsync(false);
        fixture.Advance(1999);
        await fixture.Service.RefreshAsync();
        Assert.Equal(writes, fixture.Backend.Writes.Count);
    }

    [Fact]
    public async Task QueuedLockThenUnlockBehindSlowReadStillArmsStableResumeRecovery()
    {
        await using var fixture = new Fixture();
        await fixture.WarmAsync();
        int writes = fixture.Backend.Writes.Count;
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        fixture.Backend.BeforeRead = async () =>
        {
            fixture.Backend.BeforeRead = null;
            entered.TrySetResult();
            await release.Task;
        };
        var refresh = fixture.Service.RefreshAsync();
        Task? suspend = null;
        Task? resume = null;
        try
        {
            await entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
            fixture.Backend.Value = fixture.Record.Baseline.Copy();
            suspend = fixture.Service.SetSuspendedAsync(true);
            resume = fixture.Service.SetSuspendedAsync(false);
        }
        finally
        {
            release.TrySetResult();
            await refresh;
            if (suspend != null) await suspend;
            if (resume != null) await resume;
            fixture.Backend.BeforeRead = null;
        }

        Assert.False(fixture.Record.Conflicted);
        Assert.Equal(writes, fixture.Backend.Writes.Count);
        Assert.NotEqual(ColorControlState.Conflict, fixture.State);
        fixture.Advance(1999);
        await fixture.Service.RefreshAsync();
        Assert.Equal(writes, fixture.Backend.Writes.Count);
        fixture.Advance(1);
        await fixture.Service.RefreshAsync();
        Assert.Equal(writes, fixture.Backend.Writes.Count);
        fixture.Advance(ColorTransition.DurationMilliseconds);
        await fixture.Service.RefreshAsync();
        Assert.Equal(ColorControlState.Warm, fixture.State);
        Assert.False(fixture.Record.Conflicted);
        Assert.True(GammaTarget.Create(Baseline(), 4500, 1).SameAs(fixture.Backend.Value));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SettingsChangedDuringDurableResumeUpdateRequireFreshObservationAndNeverAdoptUnknownColor(bool unknownColor)
    {
        var store = new Store();
        await using var fixture = new Fixture(store);
        await fixture.WarmAsync();
        var original = fixture.Record;
        await fixture.Service.SetSuspendedAsync(true);
        fixture.Backend.Value = original.Baseline.Copy();
        await fixture.Service.SetSuspendedAsync(false);
        int writes = fixture.Backend.Writes.Count;
        bool callbackFired = false;
        Task? changeSettings = null;
        store.AfterWrite = () =>
        {
            store.AfterWrite = null;
            callbackFired = true;
            if (unknownColor) fixture.Backend.Value.Channels[400] = 17;
            changeSettings = fixture.Service.ApplySettingsAsync(Settings(5300));
        };
        fixture.Advance(2000);
        await fixture.Service.RefreshAsync();
        Assert.True(callbackFired);
        Assert.NotNull(changeSettings);
        await changeSettings!;
        Assert.Equal(writes, fixture.Backend.Writes.Count);
        Assert.True(original.Baseline.SameAs(fixture.Record.Baseline));
        Assert.True(original.Baseline.SameAs(fixture.Record.LastApplied));
        Assert.Equal(5300, fixture.Service.Settings.ForScreen(Scope.HardwareId).DayKelvin);

        if (unknownColor)
        {
            Assert.True(fixture.Record.Conflicted);
            fixture.Advance(30_000);
            await fixture.Service.RefreshAsync();
            Assert.Equal(writes, fixture.Backend.Writes.Count);
            Assert.Equal((ushort)17, fixture.Backend.Value.Channels[400]);
        }
        else
        {
            Assert.False(fixture.Record.Conflicted);
            fixture.Advance(1999);
            await fixture.Service.RefreshAsync();
            Assert.Equal(writes, fixture.Backend.Writes.Count);
            fixture.Advance(1);
            await fixture.Service.RefreshAsync();
            Assert.Equal(writes, fixture.Backend.Writes.Count);
            fixture.Advance(ColorTransition.DurationMilliseconds);
            await fixture.Service.RefreshAsync();
            Assert.Equal(ColorControlState.Warm, fixture.State);
            Assert.False(fixture.Record.Conflicted);
            Assert.True(GammaTarget.Create(original.Baseline, 5300, 1).SameAs(fixture.Backend.Value));
        }
    }

    [Fact]
    public async Task QueuedLockUnlockLockBehindSlowReadKeepsLastLockAndRequiresFullObservationAfterNextUnlock()
    {
        await using var fixture = new Fixture();
        await fixture.WarmAsync();
        int writes = fixture.Backend.Writes.Count;
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        fixture.Backend.BeforeRead = async () =>
        {
            fixture.Backend.BeforeRead = null;
            entered.TrySetResult();
            await release.Task;
        };
        var refresh = fixture.Service.RefreshAsync();
        Task? firstLock = null;
        Task? staleUnlock = null;
        Task? finalLock = null;
        try
        {
            await entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
            fixture.Backend.Value = fixture.Record.Baseline.Copy();
            firstLock = fixture.Service.SetSuspendedAsync(true);
            staleUnlock = fixture.Service.SetSuspendedAsync(false);
            finalLock = fixture.Service.SetSuspendedAsync(true);
        }
        finally
        {
            release.TrySetResult();
            await refresh;
            if (firstLock != null) await firstLock;
            if (staleUnlock != null) await staleUnlock;
            if (finalLock != null) await finalLock;
            fixture.Backend.BeforeRead = null;
        }

        Assert.Equal(ColorControlState.Paused, fixture.State);
        Assert.False(fixture.Record.Conflicted);
        Assert.Equal(writes, fixture.Backend.Writes.Count);
        fixture.Advance(30_000);
        await fixture.Service.RefreshAsync();
        Assert.Equal(ColorControlState.Paused, fixture.State);
        Assert.Equal(writes, fixture.Backend.Writes.Count);
        await fixture.Service.SetSuspendedAsync(false);
        fixture.Advance(1999);
        await fixture.Service.RefreshAsync();
        Assert.Equal(writes, fixture.Backend.Writes.Count);
        fixture.Advance(1);
        await fixture.Service.RefreshAsync();
        Assert.Equal(writes, fixture.Backend.Writes.Count);
        fixture.Advance(ColorTransition.DurationMilliseconds);
        await fixture.Service.RefreshAsync();
        Assert.Equal(ColorControlState.Warm, fixture.State);
        Assert.False(fixture.Record.Conflicted);
        Assert.True(GammaTarget.Create(Baseline(), 4500, 1).SameAs(fixture.Backend.Value));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task RevisionChangeInsideIndependentResumeReadRequiresANewStableObservation(bool changeSettings)
    {
        await using var fixture = new Fixture();
        await fixture.WarmAsync();
        var original = fixture.Record;
        await fixture.Service.SetSuspendedAsync(true);
        fixture.Backend.Value = original.Baseline.Copy();
        await fixture.Service.SetSuspendedAsync(false);
        int writes = fixture.Backend.Writes.Count;
        bool independentReadEntered = false;
        bool baselineWasAlreadyDurable = false;
        Task? revisionChange = null;
        fixture.Advance(2000);
        fixture.Backend.AfterRead = () =>
        {
            fixture.Backend.AfterRead = null;
            fixture.Backend.BeforeRead = () =>
            {
                fixture.Backend.BeforeRead = null;
                independentReadEntered = true;
                baselineWasAlreadyDurable = fixture.Record.LastApplied.SameAs(original.Baseline);
                revisionChange = changeSettings
                    ? fixture.Service.ApplySettingsAsync(Settings(5300))
                    : fixture.Service.CancelPreviewAsync();
                return Task.CompletedTask;
            };
        };
        await fixture.Service.RefreshAsync();
        Assert.True(independentReadEntered);
        Assert.True(baselineWasAlreadyDurable);
        Assert.NotNull(revisionChange);
        await revisionChange!;
        Assert.Equal(writes, fixture.Backend.Writes.Count);
        Assert.False(fixture.Record.Conflicted);
        Assert.True(original.Baseline.SameAs(fixture.Record.LastApplied));
        fixture.Advance(1999);
        await fixture.Service.RefreshAsync();
        Assert.Equal(writes, fixture.Backend.Writes.Count);
        fixture.Advance(1);
        await fixture.Service.RefreshAsync();
        Assert.Equal(writes, fixture.Backend.Writes.Count);
        fixture.Advance(ColorTransition.DurationMilliseconds);
        await fixture.Service.RefreshAsync();
        Assert.Equal(ColorControlState.Warm, fixture.State);
        Assert.False(fixture.Record.Conflicted);
        Assert.True(GammaTarget.Create(original.Baseline, changeSettings ? 5300 : 4500, 1).SameAs(fixture.Backend.Value));
    }

    [Fact]
    public async Task PreviewRequestedInsideIndependentResumeReadCannotBypassPendingRecovery()
    {
        await using var fixture = new Fixture();
        await fixture.WarmAsync();
        var original = fixture.Record;
        await fixture.Service.SetSuspendedAsync(true);
        fixture.Backend.Value = original.Baseline.Copy();
        await fixture.Service.SetSuspendedAsync(false);
        int writes = fixture.Backend.Writes.Count;
        bool baselineWasAlreadyDurable = false;
        Task<bool>? preview = null;
        fixture.Advance(2000);
        fixture.Backend.AfterRead = () =>
        {
            fixture.Backend.AfterRead = null;
            fixture.Backend.BeforeRead = () =>
            {
                fixture.Backend.BeforeRead = null;
                baselineWasAlreadyDurable = fixture.Record.LastApplied.SameAs(original.Baseline);
                preview = fixture.Service.PreviewAsync(Scope.HardwareId, 5000);
                return Task.CompletedTask;
            };
        };
        await fixture.Service.RefreshAsync();
        Assert.True(baselineWasAlreadyDurable);
        Assert.NotNull(preview);
        Assert.False(await preview!);
        Assert.False(fixture.Service.HasPreview);
        Assert.Equal(writes, fixture.Backend.Writes.Count);
        Assert.False(fixture.Record.Conflicted);
        Assert.True(original.Baseline.SameAs(fixture.Record.Baseline));
        Assert.True(original.Baseline.SameAs(fixture.Backend.Value));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task TransientIndependentReadFailureAfterDurableBaselineConfirmationRestartsObservation(bool throwOnRead)
    {
        await using var fixture = new Fixture();
        await fixture.WarmAsync();
        var original = fixture.Record;
        await fixture.Service.SetSuspendedAsync(true);
        fixture.Backend.Value = original.Baseline.Copy();
        await fixture.Service.SetSuspendedAsync(false);
        int writes = fixture.Backend.Writes.Count;
        bool independentReadFailed = false;
        bool baselineWasAlreadyDurable = false;
        fixture.Advance(2000);
        fixture.Backend.AfterRead = () =>
        {
            fixture.Backend.AfterRead = null;
            fixture.Backend.BeforeRead = () =>
            {
                fixture.Backend.BeforeRead = null;
                independentReadFailed = true;
                baselineWasAlreadyDurable = fixture.Record.LastApplied.SameAs(original.Baseline);
                if (throwOnRead) return Task.FromException(new IOException("Transient fixture read failure."));
                fixture.Backend.ReadFails = true;
                fixture.Backend.AfterRead = () =>
                {
                    fixture.Backend.ReadFails = false;
                    fixture.Backend.AfterRead = null;
                };
                return Task.CompletedTask;
            };
        };
        await fixture.Service.RefreshAsync();
        Assert.True(independentReadFailed);
        Assert.True(baselineWasAlreadyDurable);
        Assert.Equal(writes, fixture.Backend.Writes.Count);
        Assert.NotEqual(ColorControlState.Failed, fixture.State);
        Assert.False(fixture.Record.Conflicted);
        Assert.True(original.Baseline.SameAs(fixture.Record.Baseline));
        Assert.True(original.Baseline.SameAs(fixture.Record.LastApplied));

        await fixture.Service.RefreshAsync();
        fixture.Advance(1999);
        await fixture.Service.RefreshAsync();
        Assert.Equal(writes, fixture.Backend.Writes.Count);
        fixture.Advance(1);
        await fixture.Service.RefreshAsync();
        Assert.Equal(writes, fixture.Backend.Writes.Count);
        fixture.Advance(ColorTransition.DurationMilliseconds);
        await fixture.Service.RefreshAsync();
        Assert.Equal(ColorControlState.Warm, fixture.State);
        Assert.False(fixture.Record.Conflicted);
        Assert.True(original.LastApplied.SameAs(fixture.Backend.Value));
    }

    [Fact]
    public async Task OfflineCapabilitiesDiscardTheEarlierSampleBeforeReturningDisplayCanResume()
    {
        await using var fixture = new Fixture();
        await fixture.WarmAsync();
        await fixture.Service.SetSuspendedAsync(true);
        fixture.Backend.Value = fixture.Record.Baseline.Copy();
        await fixture.Service.SetSuspendedAsync(false);
        int writes = fixture.Backend.Writes.Count;
        fixture.Advance(1000);
        fixture.Backend.Offline = true;
        await fixture.Service.RefreshAsync();
        Assert.Empty(fixture.Service.Capabilities);
        fixture.Advance(10_000);
        await fixture.Service.RefreshAsync();
        Assert.Equal(writes, fixture.Backend.Writes.Count);

        fixture.Backend.Offline = false;
        await fixture.Service.RefreshAsync();
        fixture.Advance(1999);
        await fixture.Service.RefreshAsync();
        Assert.Equal(writes, fixture.Backend.Writes.Count);
        fixture.Advance(1);
        await fixture.Service.RefreshAsync();
        Assert.Equal(writes, fixture.Backend.Writes.Count);
        fixture.Advance(ColorTransition.DurationMilliseconds);
        await fixture.Service.RefreshAsync();
        Assert.Equal(ColorControlState.Warm, fixture.State);
        Assert.False(fixture.Record.Conflicted);
    }

    [Fact]
    public async Task ExactBaselineResumePersistsThroughRealRecoveryFileStore()
    {
        using var disk = new DiskFixture();
        var files = new FileSystem();
        var fileStore = new AppDataFileStore(files, disk);
        var recovery = new ColorRecoveryFileStore(fileStore, files, disk);
        await using var fixture = new Fixture(recovery);
        await fixture.WarmAsync();
        var original = fixture.Record;
        await fixture.Service.SetSuspendedAsync(true);
        fixture.Backend.Value = original.Baseline.Copy();
        await fixture.Service.SetSuspendedAsync(false);
        fixture.Advance(2000);
        await fixture.Service.RefreshAsync();
        var observed = new ColorRecoveryFileStore(fileStore, files, disk).Read();
        Assert.True(observed.IsSafe);
        Assert.All(observed.Document.Records, record =>
        {
            Assert.False(record.Conflicted);
            Assert.True(original.Baseline.SameAs(record.Baseline));
        });
        fixture.Advance(ColorTransition.DurationMilliseconds);
        await fixture.Service.RefreshAsync();
        var reloaded = new ColorRecoveryFileStore(fileStore, files, disk).Read();
        Assert.True(reloaded.IsSafe);
        var record = Assert.Single(reloaded.Document.Records);
        Assert.False(record.Conflicted);
        Assert.True(original.Baseline.SameAs(record.Baseline));
        Assert.True(fixture.Backend.Value.SameAs(record.LastApplied));
        Assert.Equal(ColorControlState.Warm, fixture.State);
        Assert.Empty(reloaded.Document.ValidationRequiredScreens);
        Assert.True(File.Exists(Path.Combine(disk.DirectoryPath, ColorRecoveryFileStore.FileName + ".bak")));
    }

    private sealed class Fixture : IAsyncDisposable
    {
        private long _monotonic;
        private DateTimeOffset _now = new(2026, 10, 26, 12, 0, 0, TimeSpan.Zero);
        private readonly IColorRecoveryStore _store;
        public Backend Backend { get; } = new();
        public ColorTemperatureService Service { get; }
        public ColorRecoveryDocument Document => _store.Read().Document;
        public ColorRecoveryRecord Record => Assert.Single(Document.Records);
        public ColorControlState State => Assert.Single(Service.States).State;
        public Fixture(IColorRecoveryStore? store = null)
        {
            _store = store ?? new Store();
            Service = new(Backend, _store, () => _now, () => _monotonic);
        }
        public void Advance(long milliseconds) { _monotonic += milliseconds; _now = _now.AddMilliseconds(milliseconds); }
        public async Task WarmAsync()
        {
            await Service.ApplySettingsAsync(Settings());
            Advance(ColorTransition.DurationMilliseconds);
            await Service.RefreshAsync();
            Assert.Equal(ColorControlState.Warm, State);
            Assert.False(Record.Conflicted);
            Assert.False(Record.Baseline.SameAs(Record.LastApplied));
        }
        public async ValueTask DisposeAsync() => await Service.StopAsync();
    }

    private sealed class Backend : IColorTemperatureBackend
    {
        public static readonly ColorCapability Qualified = new(Scope, ColorCapabilityState.Supported, "fixture qualified", false, false, true);
        public ColorCapability Capability = Qualified;
        public ColorValue Value = Baseline();
        public List<ColorValue> Writes = [];
        public bool ReadFails, FailAfterWrite, Offline;
        public Func<Task>? BeforeRead;
        public Action? AfterRead;
        public Task<IReadOnlyList<ColorCapability>> DiscoverAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<ColorCapability>>(Offline ? [] : [Capability]);
        public async Task<ColorValue?> ReadAsync(ColorDisplayScope scope, CancellationToken cancellationToken)
        {
            if (BeforeRead != null) await BeforeRead();
            var value = ReadFails ? null : Value.Copy();
            AfterRead?.Invoke();
            return value;
        }
        public Task<bool> WriteAsync(ColorDisplayScope scope, ColorValue value, CancellationToken cancellationToken)
        {
            Assert.Equal(ColorCapabilityState.Supported, Capability.State);
            Assert.True(WindowsGammaBackend.SameScope(scope, Capability.Scope));
            Value = value.Copy(); Writes.Add(value.Copy());
            return Task.FromResult(!FailAfterWrite);
        }
        public ColorValue CreateTarget(ColorValue baseline, int kelvin, double warmth) => GammaTarget.Create(baseline, kelvin, warmth);
    }

    private sealed class Store : IColorRecoveryStore
    {
        public ColorRecoveryDocument Document = new();
        public bool FailWrites;
        public Action? AfterWrite;
        public ColorRecoveryReadResult Read() => new(Document.Copy(), true);
        public bool TryWrite(ColorRecoveryDocument document)
        {
            if (FailWrites) return false;
            Document = document.Copy();
            AfterWrite?.Invoke();
            return true;
        }
    }

    private sealed class DiskFixture : IStorageRoot, IDisposable
    {
        private readonly string _tempRoot = Path.GetFullPath(Path.GetTempPath());
        public string DirectoryPath { get; }
        public DiskFixture()
        {
            DirectoryPath = Path.Combine(_tempRoot, "screenkeeper-color-resume-test-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(DirectoryPath);
        }
        public void Dispose()
        {
            string target = Path.GetFullPath(DirectoryPath);
            Assert.Equal(_tempRoot.TrimEnd(Path.DirectorySeparatorChar), Path.GetDirectoryName(target), ignoreCase: true);
            Assert.StartsWith("screenkeeper-color-resume-test-", Path.GetFileName(target));
            Directory.Delete(target, recursive: true);
        }
    }
}
