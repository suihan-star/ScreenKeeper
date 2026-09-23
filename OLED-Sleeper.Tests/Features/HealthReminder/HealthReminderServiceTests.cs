using OLED_Sleeper.Features.HealthReminder;
using System.Windows.Threading;

namespace OLED_Sleeper.Tests.Features.HealthReminder;

public sealed class HealthReminderServiceTests
{
    [WpfFact]
    public void DirectRestPreviewKeepsWorkCycleAndHonorsPrivacyGate()
    {
        using var f = new Fixture();
        f.Enable(); f.Advance(5);
        Assert.True(f.Service.Preview(rest: true));
        Assert.Equal(HealthCardKind.PreviewCountdown, f.Presenter.Kind);
        Assert.Equal(HealthReminderState.Working, f.Service.Snapshot.State);
        f.Advance(3);
        Assert.Equal(TimeSpan.FromSeconds(8), f.Service.Snapshot.WorkElapsed);
        f.BlockedScreen = "screen-a"; f.Advance(1);
        Assert.False(f.Presenter.IsVisible);
        Assert.False(f.Service.Preview(rest: true));
    }

    [WpfFact]
    public void PreviewWorksWhileDisabledAndNeverStartsRealCountdown()
    {
        using var f = new Fixture();
        Assert.True(f.Service.Preview());
        Assert.Equal(HealthCardKind.Preview, f.Presenter.Kind);
        f.Presenter.Click(HealthCardAction.StartBreak);
        Assert.Equal(HealthCardKind.PreviewCountdown, f.Presenter.Kind);
        Assert.True(f.Presenter.IsVisible);
        Assert.Equal(HealthReminderState.Disabled, f.Service.Snapshot.State);
        f.Advance(20);
        Assert.False(f.Presenter.IsVisible);
        Assert.False(f.Service.Snapshot.TimerEnded);
    }

    [WpfFact]
    public void PreviewDoesNotResetOrPauseWorkAndCanBeClosed()
    {
        using var f = new Fixture();
        f.Enable();
        f.Advance(5);
        Assert.True(f.Service.Preview());
        f.Advance(5);
        f.Service.DismissWindow();
        Assert.False(f.Presenter.IsVisible);
        Assert.Equal(TimeSpan.FromSeconds(10), f.Service.Snapshot.WorkElapsed);
        f.Advance(1);
        Assert.Equal(TimeSpan.FromSeconds(11), f.Service.Snapshot.WorkElapsed);
    }

    [WpfFact]
    public void PreviewCannotReplaceAnActiveCountdown()
    {
        using var f = new Fixture();
        f.Enable();
        f.Service.StartBreak();
        Assert.False(f.Service.Preview());
        Assert.Equal(HealthCardKind.Countdown, f.Presenter.Kind);
        Assert.Equal(HealthReminderState.Breaking, f.Service.Snapshot.State);
    }

    [WpfFact]
    public void ManualPreviewBypassesFullscreenButStillRespectsPrivacyAndSession()
    {
        using var f = new Fixture();
        f.ScreenAvailable = false;
        Assert.False(f.Service.Preview());
        f.ScreenAvailable = true;
        f.Activity.Current = f.Activity.Current with { IsFullscreen = true };
        Assert.True(f.Service.Preview());
        f.Advance(3);
        Assert.True(f.Presenter.IsVisible);
        f.Activity.Current = f.Activity.Current with { SessionAvailable = false };
        f.Advance(1);
        Assert.False(f.Presenter.IsVisible);
        Assert.False(f.Service.Preview());
    }

    [WpfFact]
    public void PrivacyCanDismissCountdownImmediatelyWithoutCancellingOrMigratingIt()
    {
        using var f = new Fixture();
        f.Enable();
        f.Service.StartBreak();
        f.Advance(5);
        f.Service.DismissWindow();
        Assert.False(f.Presenter.IsVisible);
        Assert.Equal(HealthReminderState.Breaking, f.Service.Snapshot.State);
        f.Activity.Current = f.Activity.Current with { ForegroundScreen = new HealthScreen("screen-b", 1920, 0, 3840, 1040) };
        f.Advance(10);
        Assert.False(f.Presenter.IsVisible);
        Assert.Equal(TimeSpan.FromSeconds(5), f.Service.Snapshot.Remaining);
    }

    [WpfFact]
    public void ProtectingTheCardHostClosesItEvenAfterForegroundMovesToOtherScreen()
    {
        using var f = new Fixture();
        f.Enable();
        f.Service.StartBreak();
        Assert.Equal("screen-a", f.Service.Snapshot.CurrentScreenDeviceName);
        f.Activity.Current = f.Activity.Current with { ForegroundScreen = new HealthScreen("screen-b", 1920, 0, 3840, 1040) };
        f.BlockedScreen = "screen-a";
        f.Advance(1);
        Assert.False(f.Presenter.IsVisible);
        Assert.Equal(HealthReminderState.Breaking, f.Service.Snapshot.State);
    }

    [WpfFact]
    public void VisibleReminderStaysOnItsOriginalScreenWhenForegroundChanges()
    {
        using var f = new Fixture();
        f.Enable();
        f.Advance(300);
        Assert.Equal(HealthCardKind.Reminder, f.Presenter.Kind);
        f.Activity.Current = f.Activity.Current with { ForegroundScreen = new HealthScreen("screen-b", 1920, 0, 3840, 1040) };
        f.Advance(5);
        Assert.Equal("screen-a", f.Service.Snapshot.CurrentScreenDeviceName);
        Assert.Equal(1, f.Presenter.ShowCount);
        Assert.Equal(["screen-a"], f.Presenter.ShownScreens);
    }

    [WpfFact]
    public void UnavailableReminderHostDoesNotImmediatelyReopenOnOtherForegroundScreen()
    {
        using var f = new Fixture();
        f.Enable();
        f.Advance(300);
        f.Activity.Current = f.Activity.Current with { ForegroundScreen = new HealthScreen("screen-b", 1920, 0, 3840, 1040) };
        f.BlockedScreen = "screen-a";
        f.Advance(1);
        Assert.False(f.Presenter.IsVisible);
        Assert.Null(f.Service.Snapshot.CurrentScreenDeviceName);
        Assert.NotEqual(HealthReminderState.Reminder, f.Service.Snapshot.State);
        f.Advance(5);
        Assert.Equal(["screen-a"], f.Presenter.ShownScreens);
        Assert.False(f.Presenter.IsVisible);
        Assert.Equal(TimeSpan.FromMinutes(5), f.Service.Snapshot.WorkElapsed);
    }

    [WpfFact]
    public void ForegroundSwitchingToProtectedScreenNeverMovesCountdownThere()
    {
        using var f = new Fixture();
        f.Enable();
        f.Service.StartBreak();
        f.Advance(5);
        f.Activity.Current = f.Activity.Current with { ForegroundScreen = new HealthScreen("screen-b", 1920, 0, 3840, 1040) };
        f.BlockedScreen = "screen-b";
        f.Advance(1);
        Assert.False(f.Presenter.IsVisible);
        f.BlockedScreen = null;
        f.Advance(5);
        Assert.False(f.Presenter.IsVisible);
        Assert.Equal(["screen-a"], f.Presenter.ShownScreens);
        Assert.Equal(TimeSpan.FromSeconds(9), f.Service.Snapshot.Remaining);
        Assert.Equal(HealthReminderState.Breaking, f.Service.Snapshot.State);
    }

    [WpfFact]
    public void PrivacyCallbackHidesVisibleReminderBeforeTheNextPollingTick()
    {
        using var f = new Fixture();
        f.Enable();
        f.Advance(300);
        var timestamp = f.Clock.Elapsed;
        f.BlockedScreen = "screen-a";
        f.Service.DismissWindow();
        Assert.False(f.Presenter.IsVisible);
        Assert.Equal(timestamp, f.Clock.Elapsed);
        Assert.Null(f.Service.Snapshot.CurrentScreenDeviceName);
        Assert.Equal(1, f.Presenter.ShowCount);
    }

    [WpfFact]
    public void FullscreenHidesCountdownAndExitDoesNotReopenItAutomatically()
    {
        using var f = new Fixture();
        f.Service.ApplySaved(new() { Enabled = true, BreakSeconds = 120 });
        f.Service.StartBreak();
        f.Advance(5);
        f.Activity.Current = f.Activity.Current with { IsFullscreen = true };
        f.Advance(1);
        Assert.False(f.Presenter.IsVisible);
        Assert.Equal(HealthReminderState.Breaking, f.Service.Snapshot.State);
        f.Activity.Current = f.Activity.Current with { IsFullscreen = false };
        f.Advance(40);
        Assert.False(f.Presenter.IsVisible);
        Assert.Equal(TimeSpan.FromSeconds(74), f.Service.Snapshot.Remaining);
        Assert.Equal(1, f.Presenter.ShowCount);
    }

    [WpfFact]
    public void RejectedPreviewDoesNotRestartOrSnoozeTheExistingCountdown()
    {
        using var f = new Fixture();
        f.Enable();
        f.Service.StartBreak();
        f.Advance(7);
        Assert.False(f.Service.Preview());
        Assert.Equal(TimeSpan.FromSeconds(13), f.Service.Snapshot.Remaining);
        Assert.Equal(1, f.Presenter.ShowCount);
        f.Service.StartBreak();
        Assert.Equal(TimeSpan.FromSeconds(13), f.Service.Snapshot.Remaining);
        f.Advance(13);
        Assert.True(f.Service.Snapshot.TimerEnded);
        Assert.Equal(HealthReminderState.Working, f.Service.Snapshot.State);
    }

    [WpfFact]
    public void PreviewWhileManuallyPausedCannotResumeOrResetWork()
    {
        using var f = new Fixture();
        f.Enable();
        f.Advance(100);
        var until = f.Clock.UtcNow + TimeSpan.FromHours(1);
        f.Service.PauseUntil(until);
        Assert.True(f.Service.Preview());
        f.Advance(10);
        Assert.Equal(HealthReminderState.Paused, f.Service.Snapshot.State);
        Assert.Equal(until, f.Service.Snapshot.PauseUntil);
        Assert.Equal(TimeSpan.FromSeconds(100), f.Service.Snapshot.WorkElapsed);
        Assert.Equal(1, f.Presenter.ShowCount);
        Assert.True(f.Presenter.IsVisible);
        f.Advance(10);
        Assert.False(f.Presenter.IsVisible);
    }

    [WpfFact]
    public void PreviewActionsCannotStartOrSkipARealSnoozedCycle()
    {
        using var f = new Fixture();
        f.Enable();
        f.Advance(300);
        f.Presenter.Click(HealthCardAction.Snooze);
        f.Advance(10);
        Assert.True(f.Service.Preview());
        f.Presenter.Click(HealthCardAction.Skip);
        Assert.Equal(HealthReminderState.Snoozed, f.Service.Snapshot.State);
        Assert.Equal(TimeSpan.FromSeconds(50), f.Service.Snapshot.Remaining);
        Assert.Equal(TimeSpan.FromMinutes(5), f.Service.Snapshot.WorkElapsed);
        f.Advance(50);
        Assert.Equal(HealthReminderState.Reminder, f.Service.Snapshot.State);
    }

    [WpfFact]
    public void AllScreensUnavailableNeverShowsACard()
    {
        using var f = new Fixture();
        f.ScreenAvailable = false;
        f.Enable();
        f.Advance(400);
        Assert.Equal(0, f.Presenter.ShowCount);
        Assert.Equal(HealthReminderState.Deferred, f.Service.Snapshot.State);
        f.Service.StartBreak();
        f.ScreenAvailable = true;
        f.Advance(1);
        Assert.Equal(0, f.Presenter.ShowCount);
        Assert.Equal(HealthReminderState.Breaking, f.Service.Snapshot.State);
    }

    [WpfFact]
    public void ContinuousRefreshUpdatesOneCardRatherThanRecreatingWindows()
    {
        using var f = new Fixture();
        f.Enable();
        f.Service.StartBreak();
        f.Advance(10);
        Assert.Equal(1, f.Presenter.ShowCount);
        Assert.Equal(TimeSpan.FromSeconds(10), f.Presenter.Remaining);
    }

    [WpfFact]
    public void CountdownCollapseKeepsTimerWhileEscapeEndsEarly()
    {
        using var f = new Fixture();
        f.Enable();
        f.Service.StartBreak();
        f.Presenter.Click(HealthCardAction.Collapse);
        Assert.False(f.Presenter.IsVisible);
        Assert.Equal(HealthReminderState.Breaking, f.Service.Snapshot.State);
        f.Service.StartBreak();
        f.Presenter.Click(HealthCardAction.EndEarly);
        Assert.Equal(HealthReminderState.Snoozed, f.Service.Snapshot.State);
        Assert.False(f.Service.Snapshot.TimerEnded);
    }

    [WpfFact]
    public void ManualPauseHidesCardAndExpiredRestorationDoesNotPause()
    {
        using var f = new Fixture();
        f.Enable();
        f.Service.StartBreak();
        f.Service.PauseUntil(f.Clock.UtcNow + TimeSpan.FromHours(1));
        Assert.False(f.Presenter.IsVisible);
        Assert.True(f.Service.Snapshot.IsManuallyPaused);
        f.Service.RestorePause(f.Clock.UtcNow - TimeSpan.FromMinutes(1));
        Assert.False(f.Service.Snapshot.IsManuallyPaused);
    }

    [WpfFact]
    public void StopHidesAllHealthWindowsAndStopsActivitySubscriptions()
    {
        using var f = new Fixture();
        Assert.True(f.Service.Preview());
        f.Service.Stop();
        Assert.False(f.Activity.Started);
        Assert.False(f.Presenter.IsVisible);
        Assert.False(f.Service.Preview());
    }

    private sealed class Fixture : IDisposable
    {
        public FakeHealthClock Clock { get; } = new();
        public FakeActivity Activity { get; } = new();
        public FakePresenter Presenter { get; } = new();
        public HealthReminderService Service { get; }
        public bool ScreenAvailable { get; set; } = true;
        public string? BlockedScreen { get; set; }
        public Fixture()
        {
            Service = new(Dispatcher.CurrentDispatcher, name => ScreenAvailable && name != BlockedScreen, Activity, Clock, Presenter);
            Service.Start();
        }
        public void Enable() => Service.ApplySaved(new() { Enabled = true, WorkMinutes = 5, BreakSeconds = 20, SnoozeMinutes = 1 });
        public void Advance(int seconds)
        {
            for (int i = 0; i < seconds; i++)
            {
                Clock.Elapsed += TimeSpan.FromSeconds(1);
                Clock.UtcNow += TimeSpan.FromSeconds(1);
                Service.Refresh();
            }
        }
        public void Dispose() => Service.Dispose();
    }

    private sealed class FakeActivity : IUserActivitySnapshot
    {
        public bool Started { get; private set; }
        public HealthActivitySnapshot Current { get; set; } = new(true, TimeSpan.FromSeconds(3), true, true, false, new HealthScreen("screen-a", 0, 0, 1920, 1040));
        public event Action? Changed { add { } remove { } }
        public void Start() => Started = true;
        public void Stop() => Started = false;
        public HealthActivitySnapshot Read() => Current;
        public void Dispose() => Stop();
    }

    private sealed class FakePresenter : IHealthReminderPresenter
    {
        public bool IsVisible { get; private set; }
        public string? ScreenDeviceName { get; private set; }
        public HealthCardKind Kind { get; private set; }
        public TimeSpan Remaining { get; private set; }
        public int ShowCount { get; private set; }
        public List<string> ShownScreens { get; } = [];
        public event Action<HealthCardAction>? ActionRequested;
        public void Show(HealthCardKind kind, HealthScreen screen, TimeSpan remaining)
        {
            IsVisible = true;
            ScreenDeviceName = screen.DeviceName;
            Kind = kind;
            Remaining = remaining;
            ShowCount++;
            ShownScreens.Add(screen.DeviceName);
        }
        public void Update(TimeSpan remaining) => Remaining = remaining;
        public void Hide() { IsVisible = false; ScreenDeviceName = null; }
        public void Click(HealthCardAction action) => ActionRequested?.Invoke(action);
    }
}
