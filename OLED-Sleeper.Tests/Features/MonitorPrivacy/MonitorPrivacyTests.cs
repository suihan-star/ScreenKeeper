using Moq;
using OLED_Sleeper.Features.MonitorBehavior.Commands;
using OLED_Sleeper.Features.MonitorBlackout.Services;
using OLED_Sleeper.Features.MonitorBlackout.Services.Interfaces;
using OLED_Sleeper.Features.MonitorInformation.Models;
using OLED_Sleeper.Features.MonitorPrivacy;
using OLED_Sleeper.Features.UserSettings.Models;
using OLED_Sleeper.Infrastructure.Runtime.Interfaces;
using OLED_Sleeper.Messaging.Interfaces;
using OLED_Sleeper.UI.Localization;
using OLED_Sleeper.UI.ViewModels;
using System.Text.Json;
using System.Windows;

namespace OLED_Sleeper.Tests.Features.MonitorPrivacy;

[Collection("UI language")]
public class MonitorPrivacyTests
{
    private sealed class Clock : TimeProvider
    {
        public long Tick;
        public override long TimestampFrequency => 1000;
        public override long GetTimestamp() => Tick;
    }
    private sealed class Dispatcher : IDispatcher
    {
        public bool IsOnUiThread => true;
        public void Invoke(Action action) => action();
        public Task InvokeAsync(Action action) { action(); return Task.CompletedTask; }
        public Task InvokeAfterInputAsync(Action action) { action(); return Task.CompletedTask; }
    }
    private sealed class Input : IPrivacyActivitySource
    {
        public PrivacyUsageSnapshot Usage = new(null, null, false, true);
        public PrivacyUsageSnapshot ReadUsage() => Usage;
        public event Action<Point>? Activity;
        public void Emit(double x) => Activity?.Invoke(new Point(x, 50));
        public void Start() { }
        public void Stop() { }
        public void Dispose() { }
    }
    private sealed class Curtains : IPrivacyCurtainHost
    {
        public Dictionary<string, Action> Open = new();
        public Dictionary<string, Rect> Bounds = new();
        public HashSet<string> Failed = new();
        public HashSet<string> FailToShow = new();
        public void Show(string id, Rect bounds, Action release, bool desktopPending)
        {
            if (FailToShow.Contains(id)) throw new InvalidOperationException("Simulated presentation failure");
            Open[id] = release; Bounds[id] = bounds;
        }
        public void Move(string id, Rect bounds) { if (Open.ContainsKey(id)) Bounds[id] = bounds; }
        public void Close(string id) => Open.Remove(id);
        public void DesktopFailed(string id) => Failed.Add(id);
        public void DesktopReady(string id) => Close(id);
        public bool SetSleeping(string id, bool sleeping) => Open.ContainsKey(id);
        public bool IsCurtainWindow(nint handle) => false;
        public void Dispose() => Open.Clear();
    }
    private sealed class Desktop : IScreenDesktopService
    {
        public bool Succeeds = true;
        public Func<Task<bool>>? Operation;
        public CancellationToken LastCancellation;
        public Task<bool> ShowDesktopAsync(Rect bounds, CancellationToken cancellationToken = default)
        {
            LastCancellation = cancellationToken;
            return Operation?.Invoke() ?? Task.FromResult(Succeeds);
        }
    }
    private sealed class Fixture : IDisposable
    {
        public Clock Clock = new();
        public Input Input = new();
        public Curtains Curtains = new();
        public Desktop Desktop = new();
        public Mock<IMediator> Mediator = new();
        public MonitorPrivacyService Service;
        public List<MonitorSettings> Settings = new()
        {
            new() { HardwareId = "A", IsManaged = true, PrivacyMode = PrivacyMode.Curtain, PrivacyIdleSeconds = 30 },
            new() { HardwareId = "B", IsManaged = true, PrivacyMode = PrivacyMode.Curtain, PrivacyIdleSeconds = 30 }
        };
        public List<MonitorInfo> Monitors = new()
        {
            new() { HardwareId = "A", Bounds = new Rect(0, 0, 100, 100) },
            new() { HardwareId = "B", Bounds = new Rect(100, 0, 100, 100) }
        };
        public OLED_Sleeper.Features.MonitorIdleDetection.Services.ScreenSnoozeService Snooze;
        public Fixture(IScreenApplicationService? apps = null)
        { Snooze = new(Clock); Service = new(new Dispatcher(), Input, Curtains, Desktop, Mediator.Object, Clock, apps, Snooze); }
        public Task Configure() => Service.ConfigureAsync(Settings, Monitors);
        public void Dispose() => Service.Dispose();
    }

    [Fact]
    public async Task DisposeCancelsPendingDesktopAndRejectsLateProtectionRequests()
    {
        using var f = new Fixture();
        f.Settings[0].PrivacyMode = PrivacyMode.Desktop;
        await f.Configure();
        var operation = new TaskCompletionSource<bool>();
        f.Desktop.Operation = () => operation.Task;
        var pending = f.Service.ProtectScreenAsync("A");
        Assert.Single(f.Curtains.Open);

        f.Service.Dispose();
        Assert.True(f.Desktop.LastCancellation.IsCancellationRequested);
        Assert.Empty(f.Curtains.Open);
        operation.SetResult(true); await pending;
        await f.Configure();
        f.Service.Start(); f.Service.Stop();
        await f.Service.ProtectAllAsync();
        await f.Service.ProtectScreenAsync("A");
        Assert.False(await f.Service.TryPrepareForSleepAsync("A", () => true));
        Assert.Empty(f.Curtains.Open);
        Assert.False(f.Service.HasConfiguredScreens);
    }

    [Fact]
    public async Task SelectedProtectionAndRuntimeSnapshotUseOnlyTheSavedTarget()
    {
        using var f = new Fixture(); await f.Configure(); f.Service.Start();
        Assert.Equal(PrivacyRuntimeState.Off, f.Service.GetSnapshot("missing").State);
        f.Clock.Tick = 10000;
        Assert.Equal(20, f.Service.GetSnapshot("A").RemainingSeconds);
        f.Snooze.Pause("A", MonitorSettingsScope.Privacy, 60); await f.Service.CheckAsync();
        f.Snooze.Resume("A", MonitorSettingsScope.Privacy);
        Assert.Equal(30, f.Service.GetSnapshot("A").RemainingSeconds);
        f.Input.Emit(50); Assert.Equal(PrivacyRuntimeState.InUse, f.Service.GetSnapshot("A").State);
        await f.Service.ProtectScreenAsync("A");
        Assert.Equal(new[] { "A" }, f.Curtains.Open.Keys);
        Assert.Equal(PrivacyRuntimeState.Protected, f.Service.GetSnapshot("A").State);
        Assert.Equal(PrivacyRuntimeState.Waiting, f.Service.GetSnapshot("B").State);
        f.Snooze.Pause("A", MonitorSettingsScope.Privacy, 60);
        Assert.Equal(PrivacyRuntimeState.Protected, f.Service.GetSnapshot("A").State);
        await f.Service.ProtectScreenAsync("missing"); Assert.Single(f.Curtains.Open);
        f.Curtains.Open["A"](); Assert.Equal(PrivacyRuntimeState.Paused, f.Service.GetSnapshot("A").State);
        f.Service.Stop();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public async Task EachSelectedQuietConditionDefersProtectionAndSleep_ThenRestartsWaiting(int condition)
    {
        using var f = new Fixture();
        var options = f.Settings[0].PrivacyOptions!; options.QuietWhileInUse = true;
        options.QuietOnMousePosition = condition == 0; options.QuietOnActiveWindow = condition == 1; options.QuietOnInput = condition == 2;
        await f.Configure();
        f.Input.Usage = condition switch
        {
            0 => new(new Point(50, 50), null, false, true),
            1 => new(null, new Rect(0, 0, 90, 90), false, true),
            _ => new(null, null, true, true)
        };
        f.Clock.Tick = 60000; await f.Service.CheckAsync();
        Assert.DoesNotContain("A", f.Curtains.Open.Keys); Assert.Contains("B", f.Curtains.Open.Keys);
        Assert.False(await f.Service.TryPrepareForSleepAsync("A", () => true));
        f.Input.Usage = new(null, null, false, true); f.Clock.Tick = 61000; await f.Service.CheckAsync();
        f.Clock.Tick = 90999; await f.Service.CheckAsync(); Assert.DoesNotContain("A", f.Curtains.Open.Keys);
        f.Clock.Tick = 91000; await f.Service.CheckAsync(); Assert.Contains("A", f.Curtains.Open.Keys);
        f.Input.Usage = new(new Point(50, 50), new Rect(0, 0, 90, 90), true, true); await f.Service.CheckAsync();
        Assert.Contains("A", f.Curtains.Open.Keys);
    }
    [Fact]
    public async Task RealInputAndStaleSleepRequestsCannotTriggerNewCurtains()
    {
        using var f = new Fixture(); await f.Configure();
        f.Clock.Tick = 29999; f.Input.Emit(50);
        Assert.False(await f.Service.TryPrepareForSleepAsync("A", () => true));
        Assert.False(await f.Service.TryPrepareForSleepAsync("B", () => false));
        Assert.Empty(f.Curtains.Open);
        f.Clock.Tick = 30001; await f.Service.CheckAsync(); Assert.DoesNotContain("A", f.Curtains.Open.Keys);
        f.Clock.Tick = 59999; await f.Service.CheckAsync(); Assert.Contains("A", f.Curtains.Open.Keys);
    }
    [Fact]
    public async Task SharedSnoozeResumeDoesNotWaitUntilTheOldDeadline_AndNeverReleasesACurtain()
    {
        using var f = new Fixture(); await f.Configure();
        f.Snooze.Pause("A", MonitorSettingsScope.Privacy, 3600); await f.Service.CheckAsync();
        f.Clock.Tick = 10000; f.Snooze.Resume("A", MonitorSettingsScope.Privacy); await f.Service.CheckAsync();
        f.Clock.Tick = 39999; await f.Service.CheckAsync(); Assert.DoesNotContain("A", f.Curtains.Open.Keys);
        f.Clock.Tick = 40000; await f.Service.CheckAsync(); Assert.Contains("A", f.Curtains.Open.Keys);
        f.Snooze.Pause("A", MonitorSettingsScope.Privacy, 60); await f.Service.CheckAsync(); Assert.Contains("A", f.Curtains.Open.Keys);
    }
    [Fact]
    public async Task IndependentPrivacyRunsWithoutRest_AndSessionReturnWaitsAgain()
    {
        using var f = new Fixture(); f.Settings[0].IsManaged = false; f.Settings[0].PrivacyEnabled = true; await f.Configure();
        f.Input.Usage = new(null, null, false, false); f.Clock.Tick = 60000; await f.Service.CheckAsync(); Assert.Empty(f.Curtains.Open);
        f.Input.Usage = new(null, null, false, true); f.Clock.Tick = 60001; await f.Service.CheckAsync(); Assert.Empty(f.Curtains.Open);
        f.Clock.Tick = 90001; await f.Service.CheckAsync(); Assert.Contains("A", f.Curtains.Open.Keys);
    }

    [Fact]
    public async Task PresentationFailureRemainsVisiblePerScreen_AndRetriesAfterTimeout()
    {
        using var f = new Fixture(); await f.Configure();
        f.Curtains.FailToShow.Add("A");
        await f.Service.ProtectAllAsync();
        Assert.Contains("Windows", f.Service.GetStatus("A"));
        Assert.DoesNotContain("Windows", f.Service.GetStatus("B"));
        Assert.DoesNotContain("A", f.Curtains.Open.Keys);
        f.Curtains.FailToShow.Clear();
        f.Clock.Tick = 30000; await f.Service.CheckAsync();
        Assert.Contains("A", f.Curtains.Open.Keys);
        Assert.DoesNotContain("Windows", f.Service.GetStatus("A"));
    }

    [Fact]
    public async Task OtherScreenInputAndNoInputDoNotKeepScreenAwake()
    {
        using var f = new Fixture(); await f.Configure();
        f.Clock.Tick = 29000; f.Input.Emit(150);
        f.Clock.Tick = 30000; await f.Service.CheckAsync();
        Assert.Contains("A", f.Curtains.Open.Keys);
        Assert.DoesNotContain("B", f.Curtains.Open.Keys);
        f.Clock.Tick = 59000; await f.Service.CheckAsync();
        Assert.Contains("B", f.Curtains.Open.Keys);
    }

    [Fact]
    public async Task InputWakesButNeverDismissesCurtain_ExplicitReleaseRestartsTimer()
    {
        using var f = new Fixture(); await f.Configure();
        await f.Service.ProtectAllAsync();
        f.Clock.Tick = 1000; f.Input.Emit(50); await f.Service.CheckAsync();
        Assert.Contains("A", f.Curtains.Open.Keys);
        f.Curtains.Open["A"]();
        Assert.DoesNotContain("A", f.Curtains.Open.Keys);
        f.Clock.Tick = 30999; await f.Service.CheckAsync();
        Assert.DoesNotContain("A", f.Curtains.Open.Keys);
        f.Clock.Tick = 31000; await f.Service.CheckAsync();
        Assert.Contains("A", f.Curtains.Open.Keys);
    }

    [Fact]
    public async Task ContinuousInputWakesOnce_AnotherSleepRearmsWake()
    {
        using var f = new Fixture(); await f.Configure();
        await f.Service.PrepareForSleepAsync("A");
        for (var i = 0; i < 5; i++)
        {
            f.Input.Emit(50); await f.Service.CheckAsync();
        }
        f.Mediator.Verify(m => m.SendAsync(It.IsAny<RestoreMonitorStateCommand>()), Times.Once);
        await f.Service.PrepareForSleepAsync("A");
        f.Input.Emit(50); await f.Service.CheckAsync();
        f.Mediator.Verify(m => m.SendAsync(It.IsAny<RestoreMonitorStateCommand>()), Times.Exactly(2));
        Assert.Contains("A", f.Curtains.Open.Keys);
    }

    [Fact]
    public async Task PauseSuppressesAutomaticProtection_ButManualProtectionStillWorks()
    {
        using var f = new Fixture(); await f.Configure();
        f.Service.TogglePause("A");
        f.Clock.Tick = 60000; await f.Service.CheckAsync();
        Assert.DoesNotContain("A", f.Curtains.Open.Keys);
        Assert.True(f.Service.IsPaused("A"));
        await f.Service.ProtectAllAsync();
        Assert.Contains("A", f.Curtains.Open.Keys);
        f.Service.TogglePause("A");
        Assert.Contains("A", f.Curtains.Open.Keys);
        Assert.False(f.Service.IsPaused("A"));
    }

    [Fact]
    public async Task SleepIsGuardedEvenWhenItsTimeoutIsEarlierThanPrivacyTimeout()
    {
        using var f = new Fixture(); await f.Configure();
        await f.Service.PrepareForSleepAsync("A");
        Assert.Contains("A", f.Curtains.Open.Keys);
        Assert.DoesNotContain("B", f.Curtains.Open.Keys);
    }

    [Fact]
    public async Task DesktopFailureRetainsCurtain_AndInputDoesNotDismissIt()
    {
        using var f = new Fixture(); f.Settings[0].PrivacyMode = PrivacyMode.Desktop;
        f.Desktop.Succeeds = false; await f.Configure();
        await f.Service.PrepareForSleepAsync("A");
        Assert.Contains("A", f.Curtains.Failed);
        f.Clock.Tick = 500; f.Input.Emit(50); await f.Service.CheckAsync();
        Assert.Contains("A", f.Curtains.Open.Keys);
    }

    [Fact]
    public async Task DesktopSuccessHidesCurtainOnlyAfterCompletion_AndRearmsOnActualActivity()
    {
        using var f = new Fixture(); f.Settings[0].PrivacyMode = PrivacyMode.Desktop;
        f.Desktop.Operation = () =>
        {
            Assert.Contains("A", f.Curtains.Open.Keys);
            return Task.FromResult(true);
        };
        await f.Configure(); await f.Service.PrepareForSleepAsync("A");
        Assert.DoesNotContain("A", f.Curtains.Open.Keys);
        Assert.Contains("桌面", f.Service.GetStatus("A"));
        f.Clock.Tick = 1000; f.Input.Emit(50); await f.Service.CheckAsync();
        Assert.Contains("计时", f.Service.GetStatus("A"));
    }

    [Fact]
    public async Task SavingAndRepositioningPreserveCurtain_ExplicitDisableRemovesIt()
    {
        using var f = new Fixture(); await f.Configure(); await f.Service.ProtectAllAsync();
        f.Monitors[0].Bounds = new Rect(-100, 0, 100, 100);
        await f.Configure();
        Assert.Contains("A", f.Curtains.Open.Keys);
        Assert.Equal(-100, f.Curtains.Bounds["A"].X);
        f.Settings[0].PrivacyMode = PrivacyMode.None; await f.Configure();
        Assert.DoesNotContain("A", f.Curtains.Open.Keys);
        Assert.Contains("B", f.Curtains.Open.Keys);
        f.Monitors.Clear(); await f.Configure();
        Assert.Empty(f.Curtains.Open);
    }

    [Fact]
    public async Task StaleDesktopCompletionCannotDismissNewProtection()
    {
        using var f = new Fixture(); f.Settings[0].PrivacyMode = PrivacyMode.Desktop;
        var completion = new TaskCompletionSource<bool>();
        f.Desktop.Operation = () => completion.Task;
        await f.Configure(); var work = f.Service.PrepareForSleepAsync("A");
        f.Settings[0].PrivacyMode = PrivacyMode.Curtain;
        await f.Configure();
        Assert.True(f.Desktop.LastCancellation.IsCancellationRequested);
        completion.SetResult(true); await work;
        Assert.Contains("A", f.Curtains.Open.Keys);
    }

    [Fact]
    public async Task ExplicitReleaseCancelsPendingDesktopWork()
    {
        using var f = new Fixture(); f.Settings[0].PrivacyMode = PrivacyMode.Desktop;
        var completion = new TaskCompletionSource<bool>();
        f.Desktop.Operation = () => completion.Task;
        await f.Configure(); var work = f.Service.PrepareForSleepAsync("A");
        f.Curtains.Open["A"]();
        Assert.True(f.Desktop.LastCancellation.IsCancellationRequested);
        Assert.DoesNotContain("A", f.Curtains.Open.Keys);
        completion.SetResult(true); await work;
        Assert.Contains("计时", f.Service.GetStatus("A"));
    }

    [Fact]
    public async Task UnknownModeAndInvalidTimeoutUseSafeRuntimeDefaults()
    {
        using var f = new Fixture(); f.Settings[0].PrivacyMode = (PrivacyMode)99;
        f.Settings[0].PrivacyIdleSeconds = int.MaxValue; await f.Configure();
        f.Clock.Tick = 30000; await f.Service.CheckAsync();
        Assert.Contains("A", f.Curtains.Open.Keys);
    }

    [Fact]
    public void OldJsonDefaultsToOff_NewFieldsRoundTrip()
    {
        var old = JsonSerializer.Deserialize<MonitorSettings>("{\"HardwareId\":\"A\",\"IsManaged\":true}")!;
        Assert.Equal(PrivacyMode.None, old.PrivacyMode);
        Assert.Equal(30, old.PrivacyIdleSeconds);
        old.PrivacyMode = PrivacyMode.Desktop; old.PrivacyIdleSeconds = 45;
        var copy = JsonSerializer.Deserialize<MonitorSettings>(JsonSerializer.Serialize(old))!;
        Assert.Equal(PrivacyMode.Desktop, copy.PrivacyMode);
        Assert.Equal(45, copy.PrivacyIdleSeconds);
    }

    [StaFact]
    public void PrivacyOnlySettingsValidate_PreserveInvalidDraftAndDiscardBaseline()
    {
        var vm = new MonitorConfigurationViewModel(new MonitorInfo { HardwareId = "A" });
        vm.IsManaged = true; vm.PrivacyMode = PrivacyMode.Curtain;
        vm.IsActiveOnMousePosition = false; vm.IdleValue = null;
        Assert.True(vm.IsValid);
        vm.MarkAsSaved();
        vm.PrivacyIdleText = "bad";
        var resized = new MonitorConfigurationViewModel(new MonitorInfo { HardwareId = "A" });
        resized.PreserveDraftFrom(vm);
        Assert.Equal("bad", resized.PrivacyIdleText);
        Assert.False(resized.IsValid); Assert.True(resized.IsDirty);
        resized.Revert();
        Assert.Equal(30, resized.PrivacyIdleSeconds);
        Assert.True(resized.IsValid); Assert.False(resized.IsDirty);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-1")]
    [InlineData("86401")]
    [InlineData("2.5")]
    [InlineData("")]
    public void InvalidPrivacyTimeoutCannotBeSaved(string text)
    {
        var vm = new MonitorConfigurationViewModel(new MonitorInfo { HardwareId = "A" });
        vm.IsManaged = true; vm.PrivacyMode = PrivacyMode.Curtain; vm.PrivacyIdleText = text;
        Assert.False(vm.IsValid); Assert.NotEmpty(vm.PrivacyError);
    }

    [Theory]
    [InlineData(0, 100, true, true)]
    [InlineData(90, 100, true, false)]
    [InlineData(-1, 100, true, false)]
    [InlineData(0, 100, false, true)]
    public void DesktopPolicyRejectsSpanningWindows_RegardlessOfTitleBarButtons(double x, double width, bool minimizes, bool expected)
    {
        Assert.Equal(expected, DesktopWindowPolicy.FitsScreen(new DesktopWindow(1, new Rect(x, 0, width, 100), minimizes), new Rect(0, 0, 100, 100)));
    }

    [Fact]
    public async Task BlackoutAndRestoreUseCurtainWithoutClosingIt()
    {
        var host = new Mock<IPrivacyCurtainHost>();
        host.Setup(h => h.SetSleeping("A", true)).Returns(true);
        var factory = new Mock<IOverlayWindowFactory>();
        var blackout = new MonitorBlackoutService(new Dispatcher(), factory.Object, host.Object);
        await blackout.ShowBlackoutOverlayAsync("A", new Rect(0, 0, 100, 100));
        factory.Verify(f => f.Create(), Times.Never);
        await blackout.HideBlackoutOverlayAsync("A");
        host.Verify(h => h.SetSleeping("A", false), Times.Once);
        host.Verify(h => h.Close(It.IsAny<string>()), Times.Never);
    }
    [Fact]
    public async Task SavingNewPauseDefaultDoesNotResetAnActivePause()
    {
        using var f = new Fixture(); await f.Configure();
        f.Service.Pause("A", 3600);
        f.Clock.Tick = 60000;
        f.Settings[0].PrivacyOptions!.PauseValue = 2;
        await f.Configure();
        f.Clock.Tick = 3599999; Assert.True(f.Service.IsPaused("A"));
        f.Clock.Tick = 3600000; Assert.False(f.Service.IsPaused("A"));
    }
    [Fact]
    public async Task ApplicationFailureKeepsCurtain_AndSuccessNeverRestoresPrivateWindows()
    {
        var apps = new Mock<IScreenApplicationService>();
        apps.Setup(a => a.ShowAsync(It.IsAny<PrivacyOptions>(), It.IsAny<Rect>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);
        using var f = new Fixture(apps.Object);
        f.Settings[0].PrivacyMode = PrivacyMode.Application;
        f.Settings[0].PrivacyOptions!.ApplicationPath = @"C:\Apps\calendar.exe";
        await f.Configure(); await f.Service.ProtectAllAsync();
        Assert.Contains("A", f.Curtains.Open.Keys); Assert.Contains("A", f.Curtains.Failed);
        apps.Setup(a => a.ShowAsync(It.IsAny<PrivacyOptions>(), It.IsAny<Rect>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        await f.Service.ProtectAllAsync();
        Assert.DoesNotContain("A", f.Curtains.Open.Keys);
        f.Input.Emit(50);
        Assert.DoesNotContain("A", f.Curtains.Open.Keys);
    }
    [Fact]
    public async Task AmbiguousSavedAppAssignmentsDegradeToCurtainsAfterRestart()
    {
        var apps = new Mock<IScreenApplicationService>();
        using var f = new Fixture(apps.Object);
        foreach (var setting in f.Settings)
        {
            setting.PrivacyMode = PrivacyMode.Application;
            setting.PrivacyOptions!.ApplicationPath = @"C:\Apps\calendar.exe";
        }
        await f.Configure(); await f.Service.ProtectAllAsync();
        Assert.Equal(2, f.Curtains.Open.Count);
        apps.Verify(a => a.ShowAsync(It.IsAny<PrivacyOptions>(), It.IsAny<Rect>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
