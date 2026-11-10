using OLED_Sleeper.Features.ColorTemperature;

namespace OLED_Sleeper.Tests.Features.ColorTemperature
{
    public class ColorTemperatureServiceTests
    {
        private static readonly ColorDisplayScope Scope = new("monitor-1", "stable-path", "DISPLAY1", "Example", "topology-1", "mode-1");
        private static ColorValue Baseline() => new() { Channels = Enumerable.Range(0, 768).Select(i => (ushort)((i % 256) * 257)).ToArray() };
        private static ColorTemperatureSettings Enabled() => new() { Enabled = true, Mode = ColorTemperatureMode.Manual, ParticipatingScreens = new() { [Scope.HardwareId] = true } };
        private static ColorTemperatureService CreateService(Backend backend, Store store, Func<DateTimeOffset>? now = null) =>
            new(backend, store, now, () => backend.MonotonicMilliseconds);
        private static async Task ApplyReadyAsync(ColorTemperatureService service, Backend backend, ColorTemperatureSettings settings, CancellationToken cancellationToken = default)
        {
            await service.ApplySettingsAsync(settings, cancellationToken);
            backend.MonotonicMilliseconds += ColorTransition.DurationMilliseconds;
            await service.RefreshAsync(cancellationToken);
        }

        [Fact]
        public async Task WorkTargetAppliesOnlyAfterDisplayParticipationIsSaved()
        {
            var backend = new Backend(); var store = new Store();
            var now = new DateTimeOffset(2026, 9, 20, 12, 0, 0, TimeSpan.Zero);
            var service = CreateService(backend, store, () => now);
            var settings = new ColorTemperatureSettings { Enabled = true, DayUsesOriginal = false, DayKelvin = 3900, NightKelvin = 3900 };
            try
            {
                await ApplyReadyAsync(service, backend, settings);
                Assert.Empty(backend.Writes); Assert.Empty(store.Document.Records);
                Assert.Equal(ColorControlState.Disabled, Assert.Single(service.States).State);
                settings.ParticipatingScreens[Scope.HardwareId] = true;
                await ApplyReadyAsync(service, backend, settings);
                Assert.True(GammaTarget.Create(Baseline(), 3900, 1).SameAs(store.Document.Records.Single().LastApplied));
                settings.DayKelvin = 5300;
                await ApplyReadyAsync(service, backend, settings);
                Assert.True(GammaTarget.Create(Baseline(), 5300, 1).SameAs(store.Document.Records.Single().LastApplied));
            }
            finally { await service.StopAsync(); }
        }

        [Fact]
        public async Task UnverifiedDeviceNeverWritesEvenWhenSettingEnabled()
        {
            var backend = new Backend { Capability = new(Scope, ColorCapabilityState.Unverified, "unverified") };
            var service = CreateService(backend, new Store());
            await ApplyReadyAsync(service, backend, Enabled());
            Assert.Empty(backend.Writes);
            Assert.Equal(ColorControlState.Unverified, Assert.Single(service.States).State);
            Assert.False(await service.PreviewAsync(Scope.HardwareId, 5000));
        }

        [Fact]
        public async Task RecoveryIntentMustBePersistedBeforeAnyWrite()
        {
            var backend = new Backend();
            var store = new Store { FailOnWrite = 1 };
            var service = CreateService(backend, store);
            await ApplyReadyAsync(service, backend, Enabled());
            Assert.Empty(backend.Writes);
            Assert.Equal(ColorControlState.Failed, Assert.Single(service.States).State);
        }

        [Fact]
        public async Task ReadFailureDoesNotTakeOver()
        {
            var backend = new Backend { ReadFails = true };
            var store = new Store();
            await CreateService(backend, store).ApplySettingsAsync(Enabled());
            Assert.Empty(backend.Writes);
            Assert.Empty(store.Document.Records);
        }

        [Fact]
        public async Task PauseRestoresBaselineAndDoesNotChangeSavedPolicy()
        {
            var backend = new Backend();
            var store = new Store();
            var now = new DateTimeOffset(2026, 9, 17, 20, 0, 0, TimeSpan.Zero);
            var service = CreateService(backend, store, () => now);
            await ApplyReadyAsync(service, backend, Enabled());
            Assert.Single(store.Document.Records);
            Assert.True(await service.PauseAsync(Scope.HardwareId, now.AddMinutes(20)));
            Assert.True(Baseline().SameAs(backend.Value));
            Assert.True(service.Settings.Enabled);
            Assert.Equal(ColorControlState.Paused, Assert.Single(service.States).State);
            Assert.Empty(store.Document.Records);
            now = now.AddMinutes(21);
            await service.RefreshAsync();
            backend.MonotonicMilliseconds += ColorTransition.DurationMilliseconds;
            await service.RefreshAsync();
            Assert.False(Baseline().SameAs(backend.Value));
        }

        [Fact]
        public async Task RepeatedSameTargetDoesNotWriteOrJournalEveryPoll()
        {
            var backend = new Backend();
            var store = new Store();
            var service = CreateService(backend, store);
            await ApplyReadyAsync(service, backend, Enabled());
            for (int i = 0; i < 10; i++) await service.RefreshAsync();
            Assert.Single(backend.Writes);
            Assert.Equal(2, store.WriteCount);
            await service.StopAsync();
            Assert.True(Baseline().SameAs(backend.Value));
            Assert.Empty(store.Document.Records);
        }

        [Fact]
        public async Task ExternalChangeStopsCompetitionIncludingStopAndPause()
        {
            var backend = new Backend();
            var store = new Store();
            var service = CreateService(backend, store);
            await ApplyReadyAsync(service, backend, Enabled());
            backend.Value.Channels[260] = 999;
            var external = backend.Value.Copy();
            await service.RefreshAsync();
            await service.RefreshAsync();
            await service.PauseAsync(Scope.HardwareId, DateTimeOffset.Now.AddHours(1));
            await service.StopAsync();
            Assert.Single(backend.Writes);
            Assert.True(external.SameAs(backend.Value));
            Assert.True(Assert.Single(store.Document.Records).Conflicted);
        }

        [Theory]
        [InlineData("DISPLAY2", "topology-1", "mode-1")]
        [InlineData("DISPLAY1", "topology-2", "mode-1")]
        [InlineData("DISPLAY1", "topology-1", "mode-2")]
        public async Task MappingOrModeChangesNeverWriteOldBaseline(string display, string topology, string mode)
        {
            var backend = new Backend();
            var store = new Store();
            var service = CreateService(backend, store);
            await ApplyReadyAsync(service, backend, Enabled());
            backend.Capability = backend.Capability with { Scope = Scope with { DeviceName = display, TopologyFingerprint = topology, ModeFingerprint = mode } };
            await service.RefreshAsync();
            await service.StopAsync();
            Assert.Single(backend.Writes);
            Assert.Single(store.Document.Records);
            Assert.Equal(ColorControlState.RecoveryPending, Assert.Single(service.States).State);
        }

        [Fact]
        public async Task CrashJournalRestoresBeforeTakingNewBaseline()
        {
            var backend = new Backend();
            var warm = GammaTarget.Create(Baseline(), 4500, 1);
            backend.Value = warm.Copy();
            var store = new Store { Document = new() { Records = [new() { Scope = Scope, SessionId = "previous", Baseline = Baseline(), LastApplied = warm }] } };
            var service = CreateService(backend, store);
            await ApplyReadyAsync(service, backend, Enabled());
            Assert.True(Baseline().SameAs(backend.Writes[0]));
            Assert.True(Baseline().SameAs(Assert.Single(store.Document.Records).Baseline));
        }

        [Fact]
        public async Task InterruptedWriteIntentCanRecoverOnlyKnownPendingValue()
        {
            var backend = new Backend();
            var warm = GammaTarget.Create(Baseline(), 4500, 1);
            backend.Value = warm.Copy();
            var store = new Store { Document = new() { Records = [new() { Scope = Scope, SessionId = "previous", Baseline = Baseline(), LastApplied = Baseline(), PendingValue = warm }] } };
            await CreateService(backend, store).StopAsync();
            Assert.True(Baseline().SameAs(backend.Value));
            Assert.Empty(store.Document.Records);
        }

        [Fact]
        public async Task FailedPostWriteCommitKeepsIntentForRestore()
        {
            var backend = new Backend();
            var store = new Store { FailOnWrite = 2 };
            var service = CreateService(backend, store);
            await ApplyReadyAsync(service, backend, Enabled());
            Assert.NotNull(Assert.Single(store.Document.Records).PendingValue);
            await service.StopAsync();
            Assert.True(Baseline().SameAs(backend.Value));
            Assert.Empty(store.Document.Records);
        }

        [Fact]
        public async Task PartialOrFalseApiResultCanStillRestoreKnownWrittenValue()
        {
            var backend = new Backend { ReturnFalseAfterWriting = true };
            var store = new Store();
            var service = CreateService(backend, store);
            await ApplyReadyAsync(service, backend, Enabled());
            backend.ReturnFalseAfterWriting = false;
            Assert.True(await service.PauseAsync(Scope.HardwareId, DateTimeOffset.Now.AddHours(1)));
            Assert.True(Baseline().SameAs(backend.Value));
            Assert.Empty(store.Document.Records);
        }

        [Fact]
        public async Task UnsafeRecoveryFileBlocksCaptureAndWrites()
        {
            var backend = new Backend();
            var service = CreateService(backend, new Store { IsSafe = false });
            await ApplyReadyAsync(service, backend, Enabled());
            Assert.Empty(backend.Writes);
            Assert.Equal(ColorControlState.RecoveryPending, Assert.Single(service.States).State);
        }

        [Fact]
        public async Task PreviewExpiresToCurrentSavedStrategyRatherThanPreviousPreview()
        {
            var backend = new Backend();
            var now = DateTimeOffset.Now;
            var service = CreateService(backend, new Store(), () => now);
            var settings = Enabled();
            await ApplyReadyAsync(service, backend, settings);
            Assert.True(await service.PreviewAsync(Scope.HardwareId, 5000));
            backend.MonotonicMilliseconds += ColorTransition.DurationMilliseconds;
            await service.RefreshAsync();
            Assert.True(GammaTarget.Create(Baseline(), 5000, 1).SameAs(backend.Value));
            now = now.AddSeconds(16);
            await service.RefreshAsync();
            backend.MonotonicMilliseconds += ColorTransition.DurationMilliseconds;
            await service.RefreshAsync();
            Assert.True(GammaTarget.Create(Baseline(), 4500, 1).SameAs(backend.Value));
            Assert.False(service.HasPreview);
        }

        [Fact]
        public async Task CanceledBeforeWriteLeavesSafeIntentAndNoDeviceMutation()
        {
            var cancellation = new CancellationTokenSource();
            var backend = new Backend();
            var store = new Store { AfterWrite = () => cancellation.Cancel() };
            var service = CreateService(backend, store);
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => ApplyReadyAsync(service, backend, Enabled(), cancellation.Token));
            Assert.Empty(backend.Writes);
            store.AfterWrite = null;
            await service.StopAsync();
            Assert.Empty(store.Document.Records);
        }

        [Fact]
        public async Task SupersededPolicyDoesNotWriteAfterItsReadCompletes()
        {
            var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var backend = new Backend { BeforeRead = async () => { entered.TrySetResult(); await release.Task; } };
            var service = CreateService(backend, new Store());
            var first = service.ApplySettingsAsync(Enabled());
            await entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
            var replacement = Enabled();
            replacement.NightKelvin = 5000;
            var second = service.ApplySettingsAsync(replacement);
            release.SetResult();
            await Task.WhenAll(first, second).WaitAsync(TimeSpan.FromSeconds(5));
            backend.MonotonicMilliseconds += ColorTransition.DurationMilliseconds;
            await service.RefreshAsync();
            Assert.Single(backend.Writes);
            Assert.True(GammaTarget.Create(Baseline(), 5000, 1).SameAs(backend.Writes[0]));
        }

        [Fact]
        public async Task PublishedStateSnapshotDoesNotEnumerateMutableDictionary()
        {
            var backend = new Backend();
            var service = CreateService(backend, new Store());
            await ApplyReadyAsync(service, backend, Enabled());
            var beforePause = service.States;
            await service.PauseAsync(Scope.HardwareId, DateTimeOffset.Now.AddHours(1));
            Assert.Equal(ColorControlState.Warm, Assert.Single(beforePause).State);
            Assert.Equal(ColorControlState.Paused, Assert.Single(service.States).State);
        }

        [Fact]
        public async Task SessionSuspensionDoesNotIssueHardwareWritesOrResetPolicy()
        {
            var backend = new Backend();
            var service = CreateService(backend, new Store());
            await ApplyReadyAsync(service, backend, Enabled());
            await service.SetSuspendedAsync(true);
            await service.RefreshAsync();
            Assert.Single(backend.Writes);
            Assert.Equal(ColorControlState.Paused, Assert.Single(service.States).State);
            await service.SetSuspendedAsync(false);
            Assert.Single(backend.Writes);
            Assert.Equal(ColorControlState.Detecting, Assert.Single(service.States).State);
            backend.MonotonicMilliseconds += 2000;
            await service.RefreshAsync();
            Assert.Single(backend.Writes);
            Assert.Equal(ColorControlState.Warm, Assert.Single(service.States).State);
        }

        [Fact]
        public async Task FirstTakeoverUsesFiveDurableMonotonicSteps()
        {
            var backend = new Backend();
            var store = new Store();
            var now = new DateTimeOffset(2026, 9, 17, 20, 0, 0, TimeSpan.Zero);
            var service = CreateService(backend, store, () => now);
            await service.ApplySettingsAsync(Enabled());
            Assert.Empty(backend.Writes);
            Assert.Equal(ColorControlState.Transitioning, Assert.Single(service.States).State);
            for (int i = 1; i <= 5; i++)
            {
                backend.MonotonicMilliseconds = i * 2000;
                now = now.AddSeconds(2);
                await service.RefreshAsync();
                var expected = new ColorTransition(Baseline(), GammaTarget.Create(Baseline(), 4500, 1), 0).Frame(i * 2000);
                Assert.True(expected.SameAs(backend.Value));
            }
            Assert.Equal(5, backend.Writes.Count);
            Assert.Equal(10, store.WriteCount);
            Assert.Equal(ColorControlState.Warm, Assert.Single(service.States).State);
            await service.StopAsync();
        }

        [Fact]
        public async Task ClockJumpDoesNotCompleteAnOngoingManualTransition()
        {
            var backend = new Backend();
            var now = new DateTimeOffset(2026, 9, 17, 20, 0, 0, TimeSpan.Zero);
            var service = CreateService(backend, new Store(), () => now);
            await service.ApplySettingsAsync(Enabled());
            now = now.AddHours(4);
            backend.MonotonicMilliseconds = 2000;
            await service.RefreshAsync();
            Assert.Equal(ColorControlState.Transitioning, Assert.Single(service.States).State);
            Assert.True(new ColorTransition(Baseline(), GammaTarget.Create(Baseline(), 4500, 1), 0).Frame(2000).SameAs(backend.Value));
            backend.MonotonicMilliseconds = 10000;
            await service.RefreshAsync();
            Assert.Equal(ColorControlState.Warm, Assert.Single(service.States).State);
            await service.StopAsync();
        }

        [Fact]
        public async Task JumpToDaytimeSmoothlyReturnsToBaselineWithoutReplayingNight()
        {
            var backend = new Backend();
            var store = new Store();
            var now = new DateTimeOffset(2026, 9, 17, 20, 0, 0, TimeSpan.Zero);
            var service = CreateService(backend, store, () => now);
            var settings = Enabled();
            settings.Mode = ColorTemperatureMode.Automatic;
            await ApplyReadyAsync(service, backend, settings);
            var warm = backend.Value.Copy();
            now = now.AddHours(16);
            backend.MonotonicMilliseconds += 1000;
            await service.RefreshAsync();
            Assert.True(warm.SameAs(backend.Value));
            Assert.Equal(ColorControlState.Transitioning, Assert.Single(service.States).State);
            backend.MonotonicMilliseconds += 10000;
            await service.RefreshAsync();
            Assert.True(Baseline().SameAs(backend.Value));
            Assert.Empty(store.Document.Records);
            Assert.Equal(ColorControlState.Original, Assert.Single(service.States).State);
            await service.StopAsync();
        }

        [Fact]
        public async Task SuspendedClockJumpDefersHardwareUntilResumeThenCatchesUp()
        {
            var backend = new Backend();
            var now = new DateTimeOffset(2026, 9, 17, 20, 0, 0, TimeSpan.Zero);
            var service = CreateService(backend, new Store(), () => now);
            var settings = Enabled(); settings.Mode = ColorTemperatureMode.Automatic;
            await ApplyReadyAsync(service, backend, settings);
            await service.SetSuspendedAsync(true);
            now = now.AddHours(16);
            backend.MonotonicMilliseconds += 100000;
            await service.RefreshAsync();
            Assert.Single(backend.Writes);
            await service.SetSuspendedAsync(false);
            Assert.Single(backend.Writes);
            backend.MonotonicMilliseconds += 2000;
            await service.RefreshAsync();
            Assert.Single(backend.Writes);
            backend.MonotonicMilliseconds += 10000;
            await service.RefreshAsync();
            Assert.True(Baseline().SameAs(backend.Value));
            await service.StopAsync();
        }

        [Fact]
        public async Task LockRequestBlocksWriteQueuedAfterAnInFlightRead()
        {
            var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var backend = new Backend { BeforeRead = async () => { entered.TrySetResult(); await release.Task; } };
            var service = CreateService(backend, new Store());
            var apply = service.ApplySettingsAsync(Enabled());
            await entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
            var suspend = service.SetSuspendedAsync(true);
            backend.MonotonicMilliseconds = 10000;
            release.SetResult();
            await Task.WhenAll(apply, suspend).WaitAsync(TimeSpan.FromSeconds(5));
            Assert.Empty(backend.Writes);
            Assert.Equal(ColorControlState.Paused, Assert.Single(service.States).State);
            await service.StopAsync();
        }

        [Fact]
        public async Task LivePreviewAndOriginalComparisonPreserveBaselineAndSavedTargets()
        {
            var backend = new Backend(); var store = new Store(); var service = CreateService(backend, store);
            await ApplyReadyAsync(service, backend, Enabled());
            Assert.True(await service.PreviewAsync(Scope.HardwareId, 5300));
            Assert.True(GammaTarget.Create(Baseline(), 5300, 1).SameAs(backend.Value));
            Assert.True(await service.PreviewOriginalAsync(Scope.HardwareId));
            Assert.True(Baseline().SameAs(backend.Value));
            Assert.Single(store.Document.Records);
            Assert.True(await service.PreviewAsync(Scope.HardwareId, 5800));
            Assert.True(GammaTarget.Create(Baseline(), 5800, 1).SameAs(backend.Value));
            Assert.True(Baseline().SameAs(Assert.Single(store.Document.Records).Baseline));
            await service.CancelPreviewAsync();
            Assert.True(GammaTarget.Create(Baseline(), 4500, 1).SameAs(backend.Value));
            Assert.Equal(4500, service.Settings.NightKelvin); Assert.False(service.HasPreview);
            await service.StopAsync();
        }

        [Fact]
        public async Task ExternalChangeDuringOriginalComparisonIsNotAdoptedOnRelease()
        {
            var backend = new Backend(); var store = new Store(); var service = CreateService(backend, store);
            Assert.True(await service.PreviewAsync(Scope.HardwareId, 4500));
            Assert.True(await service.PreviewOriginalAsync(Scope.HardwareId));
            backend.Value.Channels[260] = 999;
            var external = backend.Value.Copy(); var count = backend.Writes.Count;
            Assert.False(await service.PreviewAsync(Scope.HardwareId, 5300));
            await service.CancelPreviewAsync(); await service.StopAsync();
            Assert.Equal(count, backend.Writes.Count); Assert.True(external.SameAs(backend.Value));
            Assert.True(Assert.Single(store.Document.Records).Conflicted);
        }

        [Fact]
        public async Task CancelInvalidatesPreviewWaitingOnDeviceRead()
        {
            var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var backend = new Backend { BeforeRead = async () => { entered.TrySetResult(); await release.Task; } };
            var service = CreateService(backend, new Store());
            var preview = service.PreviewAsync(Scope.HardwareId, 5000);
            await entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
            var cancel = service.CancelPreviewAsync();
            release.SetResult(); await cancel; Assert.False(await preview);
            Assert.Empty(backend.Writes); Assert.False(service.HasPreview);
            await service.StopAsync();
        }

        [Fact]
        public async Task NewestPreviewWinsAndDoesNotDiscardQueuedSavedPolicy()
        {
            var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var backend = new Backend { BeforeRead = async () => { entered.TrySetResult(); await release.Task; } };
            var service = CreateService(backend, new Store());
            var first = service.PreviewAsync(Scope.HardwareId, 5000);
            await entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
            var saved = Enabled(); saved.DayUsesOriginal = false; saved.DayKelvin = 5800;
            var save = service.ApplySettingsAsync(saved);
            var newest = service.PreviewAsync(Scope.HardwareId, 6000);
            release.SetResult(); await Task.WhenAll(first, save, newest);
            Assert.False(await first); Assert.True(await newest);
            Assert.Equal(5800, service.Settings.DayKelvin); Assert.False(service.Settings.DayUsesOriginal);
            Assert.True(GammaTarget.Create(Baseline(), 6000, 1).SameAs(backend.Value));
            await service.CancelPreviewAsync();
            Assert.True(GammaTarget.Create(Baseline(), 4500, 1).SameAs(backend.Value));
            await service.StopAsync();
        }

        [Fact]
        public async Task PreviewTimeoutUsesMonotonicTimeAndRestoresCurrentWorkTarget()
        {
            var backend = new Backend(); var now = new DateTimeOffset(2026, 9, 18, 12, 0, 0, TimeSpan.Zero);
            var service = CreateService(backend, new Store(), () => now);
            var settings = Enabled(); settings.Mode = ColorTemperatureMode.Automatic; settings.DayUsesOriginal = false; settings.DayKelvin = 5700;
            await ApplyReadyAsync(service, backend, settings);
            Assert.True(await service.PreviewAsync(Scope.HardwareId, 4000));
            now = now.AddDays(2); backend.MonotonicMilliseconds += 14000;
            Assert.Equal(TimeSpan.FromSeconds(1), service.PreviewStatus!.Remaining);
            Assert.Equal(4000, service.PreviewStatus.Kelvin);
            Assert.Equal(Scope.HardwareId, service.PreviewStatus.HardwareId);
            await service.RefreshAsync(); Assert.True(service.HasPreview);
            backend.MonotonicMilliseconds += 1000;
            await service.RefreshAsync(); Assert.False(service.HasPreview);
            Assert.Null(service.PreviewStatus);
            Assert.True(GammaTarget.Create(Baseline(), 5700, 1).SameAs(backend.Value));
            await service.StopAsync();
        }

        [Fact]
        public async Task PreviewAndComparisonOnlyWriteTheChosenDisplay()
        {
            var second = Scope with { HardwareId = "monitor-2", ScopeId = "path-2", DeviceName = "DISPLAY2" };
            var backend = new Backend { OtherCapability = new(second, ColorCapabilityState.Supported, "fixture") };
            var service = CreateService(backend, new Store());
            var settings = Enabled(); settings.ParticipatingScreens[second.HardwareId] = true;
            await ApplyReadyAsync(service, backend, settings);
            var other = backend.OtherValue.Copy(); int otherWrites = backend.OtherWrites.Count;
            await service.PreviewAsync(Scope.HardwareId, 5300);
            await service.PreviewOriginalAsync(Scope.HardwareId);
            await service.CancelPreviewAsync();
            Assert.Equal(otherWrites, backend.OtherWrites.Count); Assert.True(other.SameAs(backend.OtherValue));
            await service.StopAsync();
        }

        [Fact]
        public async Task PreviewStillRequiresDurableRecoveryAndHonorsPause()
        {
            var backend = new Backend(); var store = new Store { FailOnWrite = 1 }; var service = CreateService(backend, store);
            Assert.False(await service.PreviewAsync(Scope.HardwareId, 5300)); Assert.Empty(backend.Writes);
            await service.StopAsync();
            backend = new(); service = CreateService(backend, new());
            await service.PauseAsync(Scope.HardwareId, DateTimeOffset.Now.AddHours(1));
            Assert.False(await service.PreviewAsync(Scope.HardwareId, 5300)); Assert.Empty(backend.Writes);
            await service.StopAsync();
        }

        [Theory]
        [InlineData("original")]
        [InlineData("external")]
        [InlineData("applied")]
        [InlineData("mode")]
        [InlineData("read-failure")]
        [InlineData("persist-failure")]
        [InlineData("unsafe-journal")]
        [InlineData("enabled")]
        public async Task RecheckOnlyClearsAnExactlyRestoredOriginalAndNeverWritesHardware(string scenario)
        {
            var baseline = Baseline(); var applied = GammaTarget.Create(baseline, 4500, 1);
            var backend = new Backend();
            var store = new Store { Document = new() { Records = [new() { Scope = Scope, SessionId = "old", Baseline = baseline, LastApplied = applied, Conflicted = true }] } };
            if (scenario == "external") backend.Value.Channels[400] = 17;
            if (scenario == "applied") backend.Value = applied.Copy();
            if (scenario == "mode") backend.Capability = backend.Capability with { Scope = Scope with { ModeFingerprint = "changed" } };
            if (scenario == "read-failure") backend.ReadFails = true;
            if (scenario == "persist-failure") store.FailOnWrite = 1;
            if (scenario == "unsafe-journal") store.IsSafe = false;
            var service = CreateService(backend, store);
            if (scenario == "enabled") await service.ApplySettingsAsync(Enabled());
            var message = await service.RecheckOriginalAsync(Scope.HardwareId);
            Assert.Empty(backend.Writes);
            if (scenario == "original")
            {
                Assert.Empty(store.Document.Records);
                Assert.Equal("已确认回到记录的原色，恢复阻塞已解除。", message);
                Assert.True(await service.PreviewAsync(Scope.HardwareId, 5300));
                await service.StopAsync();
            }
            else
            {
                Assert.True(Assert.Single(store.Document.Records).Conflicted);
                Assert.NotEqual("已确认回到记录的原色，恢复阻塞已解除。", message);
            }
        }

        [Fact]
        public async Task RecheckClearsInMemoryConflictOnlyAfterOriginalReturns()
        {
            var backend = new Backend(); var store = new Store(); var service = CreateService(backend, store);
            await ApplyReadyAsync(service, backend, Enabled());
            backend.Value.Channels[400] = 17;
            await service.RefreshAsync();
            Assert.True(Assert.Single(store.Document.Records).Conflicted);
            await service.ApplySettingsAsync(new());
            int writes = backend.Writes.Count;
            backend.Value = Baseline();
            await service.RecheckOriginalAsync(Scope.HardwareId);
            Assert.Equal(writes, backend.Writes.Count); Assert.Empty(store.Document.Records);
            Assert.True(await service.PreviewAsync(Scope.HardwareId, 5300));
            await service.StopAsync();
        }

        [Fact]
        public async Task PerDisplaySchedulesPreviewAndDisableDoNotOverwriteTheOtherDisplay()
        {
            var second = Scope with { HardwareId = "monitor-2", ScopeId = "stable-2", DeviceName = "DISPLAY2" };
            var backend = new Backend { OtherCapability = new(second, ColorCapabilityState.Supported, "fixture") };
            var store = new Store(); var now = new DateTimeOffset(2026, 9, 20, 12, 0, 0, TimeSpan.Zero);
            var service = CreateService(backend, store, () => now);
            var settings = new ColorTemperatureSettings
            {
                Schema = 2,
                ScreenProfiles = new()
                {
                    [Scope.HardwareId] = new() { Enabled = true, DayUsesOriginal = false, DayKelvin = 5400, TransitionMinutes = 0 },
                    [second.HardwareId] = new() { Enabled = true, MorningStart = "14:00", EveningStart = "22:00", NightKelvin = 3800, TransitionMinutes = 0 }
                }
            };
            try
            {
                await ApplyReadyAsync(service, backend, settings);
                Assert.True(GammaTarget.Create(Baseline(), 5400, 1).SameAs(backend.Value));
                Assert.True(GammaTarget.Create(Baseline(), 3800, 1).SameAs(backend.OtherValue));
                Assert.NotNull(service.ValidationBlockReason);
                int otherWrites = backend.OtherWrites.Count;
                Assert.True(await service.PreviewAsync(Scope.HardwareId, 4900));
                backend.MonotonicMilliseconds += ColorTransition.DurationMilliseconds; await service.RefreshAsync();
                await service.CancelPreviewAsync();
                backend.MonotonicMilliseconds += ColorTransition.DurationMilliseconds; await service.RefreshAsync();
                Assert.True(GammaTarget.Create(Baseline(), 5400, 1).SameAs(backend.Value));
                Assert.Equal(otherWrites, backend.OtherWrites.Count);
                settings.ScreenProfiles![Scope.HardwareId].Enabled = false;
                await ApplyReadyAsync(service, backend, settings);
                Assert.True(Baseline().SameAs(backend.Value));
                Assert.Equal(otherWrites, backend.OtherWrites.Count);
                Assert.Equal(second.HardwareId, Assert.Single(store.Document.Records).Scope.HardwareId);
                now = now.AddHours(3); await service.RefreshAsync();
                backend.MonotonicMilliseconds += ColorTransition.DurationMilliseconds; await service.RefreshAsync();
                Assert.True(Baseline().SameAs(backend.OtherValue));
                settings.DisableAll(); await service.ApplySettingsAsync(settings);
                Assert.Null(service.ValidationBlockReason);
            }
            finally { await service.StopAsync(); }
        }

        [Fact]
        public async Task RestOriginalRestoresCapturedBaselineAfterCustomWork()
        {
            var backend = new Backend(); var store = new Store();
            var now = new DateTimeOffset(2026, 9, 21, 12, 0, 0, TimeSpan.Zero);
            var service = CreateService(backend, store, () => now);
            var settings = Enabled(); settings.Mode = ColorTemperatureMode.Automatic;
            settings.DayUsesOriginal = false; settings.DayKelvin = 5300; settings.NightUsesOriginal = true; settings.TransitionMinutes = 0;
            try
            {
                await ApplyReadyAsync(service, backend, settings);
                Assert.True(GammaTarget.Create(Baseline(), 5300, 1).SameAs(backend.Value));
                now = now.AddHours(8); await service.RefreshAsync();
                backend.MonotonicMilliseconds += ColorTransition.DurationMilliseconds; await service.RefreshAsync();
                Assert.True(Baseline().SameAs(backend.Value)); Assert.Empty(store.Document.Records);
            }
            finally { await service.StopAsync(); }
        }

        private sealed class Backend : IColorTemperatureBackend
        {
            public ColorCapability Capability = new(Scope, ColorCapabilityState.Supported, "fixture qualified");
            public ColorValue Value = Baseline();
            public ColorCapability? OtherCapability;
            public ColorValue OtherValue = Baseline();
            public List<ColorValue> OtherWrites = [];
            public List<ColorValue> Writes = [];
            public bool ReadFails;
            public bool ReturnFalseAfterWriting;
            public Func<Task>? BeforeRead;
            public long MonotonicMilliseconds;
            public Task<IReadOnlyList<ColorCapability>> DiscoverAsync(CancellationToken token) => Task.FromResult<IReadOnlyList<ColorCapability>>(OtherCapability == null ? [Capability] : [Capability, OtherCapability]);
            public async Task<ColorValue?> ReadAsync(ColorDisplayScope scope, CancellationToken token)
            {
                if (BeforeRead != null) await BeforeRead();
                return ReadFails ? null : (scope.HardwareId == OtherCapability?.Scope.HardwareId ? OtherValue : Value).Copy();
            }
            public Task<bool> WriteAsync(ColorDisplayScope scope, ColorValue value, CancellationToken token)
            {
                if (scope.HardwareId == OtherCapability?.Scope.HardwareId)
                {
                    Assert.True(WindowsGammaBackend.SameScope(scope, OtherCapability.Scope));
                    OtherValue = value.Copy(); OtherWrites.Add(value.Copy()); return Task.FromResult(true);
                }
                Assert.True(WindowsGammaBackend.SameScope(scope, Capability.Scope));
                Value = value.Copy();
                Writes.Add(value.Copy());
                return Task.FromResult(!ReturnFalseAfterWriting);
            }
            public ColorValue CreateTarget(ColorValue baseline, int kelvin, double warmth) => GammaTarget.Create(baseline, kelvin, warmth);
        }

        private sealed class Store : IColorRecoveryStore
        {
            public ColorRecoveryDocument Document = new();
            public bool IsSafe = true;
            public int WriteCount;
            public int FailOnWrite;
            public Action? AfterWrite;
            public ColorRecoveryReadResult Read() => new(Document.Copy(), IsSafe);
            public bool TryWrite(ColorRecoveryDocument document)
            {
                if (++WriteCount == FailOnWrite) return false;
                Document = document.Copy();
                AfterWrite?.Invoke();
                return true;
            }
        }
    }
}
