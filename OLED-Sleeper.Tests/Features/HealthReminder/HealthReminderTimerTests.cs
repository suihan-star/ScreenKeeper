using OLED_Sleeper.Features.HealthReminder;

namespace OLED_Sleeper.Tests.Features.HealthReminder;

public sealed class HealthReminderTimerTests
{
    private static readonly HealthActivitySnapshot Active = new(true, TimeSpan.FromSeconds(3), true, true, false,
        new HealthScreen("screen-a", 0, 0, 1920, 1040));

    [Fact]
    public void DefaultsAreOffAndCopiesAreIndependent()
    {
        var settings = new HealthReminderSettings();
        Assert.False(settings.Enabled);
        Assert.Empty(settings.Validate());
        var copy = settings.Copy();
        copy.WorkMinutes = 90;
        Assert.Equal(50, settings.WorkMinutes);
    }

    [Theory]
    [InlineData(4, 300, 5)]
    [InlineData(181, 300, 5)]
    [InlineData(50, 19, 5)]
    [InlineData(50, 901, 5)]
    [InlineData(50, 300, 0)]
    [InlineData(50, 300, 31)]
    public void InvalidDurationsAreRejected(int work, int rest, int snooze)
    {
        var settings = new HealthReminderSettings { WorkMinutes = work, BreakSeconds = rest, SnoozeMinutes = snooze };
        Assert.NotEmpty(settings.Validate());
    }

    [Fact]
    public void UnknownVersionIsNotAccepted() => Assert.NotEmpty(new HealthReminderSettings { Schema = 2 }.Validate());

    [Fact]
    public void ScreenChangesNeverCreateOrResetTheGlobalCycle()
    {
        var f = new Fixture();
        f.Advance(100);
        f.Activity = Active with { ForegroundScreen = new HealthScreen("screen-b", -1600, 0, 0, 900) };
        f.Advance(100);
        f.ScreenAvailable = false;
        f.Advance(100);
        Assert.Equal(TimeSpan.FromMinutes(5), f.Timer.Snapshot.WorkElapsed);
        Assert.Equal(HealthReminderState.Deferred, f.Timer.Snapshot.State);
        f.ScreenAvailable = true;
        f.Advance(1);
        Assert.Equal(HealthReminderState.Reminder, f.Timer.Snapshot.State);
    }

    [Fact]
    public void WallClockChangesDoNotAddWork()
    {
        var f = new Fixture();
        f.Advance(10);
        f.Clock.UtcNow += TimeSpan.FromDays(2);
        f.Tick();
        Assert.Equal(TimeSpan.FromSeconds(10), f.Timer.Snapshot.WorkElapsed);
        f.Clock.UtcNow -= TimeSpan.FromDays(4);
        f.Advance(1);
        Assert.Equal(TimeSpan.FromSeconds(11), f.Timer.Snapshot.WorkElapsed);
    }

    [Fact]
    public void TimerGapsAreNotCountedAndRequireThirtyStableSeconds()
    {
        var f = new Fixture();
        f.Advance(10);
        f.Clock.Elapsed += TimeSpan.FromHours(2);
        f.Tick();
        Assert.Equal(HealthReminderState.Paused, f.Timer.Snapshot.State);
        Assert.Equal(TimeSpan.FromSeconds(10), f.Timer.Snapshot.WorkElapsed);
        f.Advance(29);
        Assert.Equal(HealthReminderState.Paused, f.Timer.Snapshot.State);
        f.Advance(1);
        Assert.Equal(HealthReminderState.Working, f.Timer.Snapshot.State);
    }

    [Fact]
    public void BackwardsMonotonicClockDoesNotSubtractOrAccumulateWork()
    {
        var f = new Fixture();
        f.Advance(10);
        f.Clock.Elapsed -= TimeSpan.FromSeconds(5);
        f.Tick();
        Assert.Equal(TimeSpan.FromSeconds(10), f.Timer.Snapshot.WorkElapsed);
        Assert.Equal(HealthReminderState.Paused, f.Timer.Snapshot.State);
    }

    [Fact]
    public void IdleRetainsTheCycleWithoutClaimingCompletedRest()
    {
        var f = new Fixture();
        f.Advance(120);
        f.Activity = Active with { IdleFor = TimeSpan.FromMinutes(5) };
        f.Advance(600);
        Assert.Equal("因无输入暂停", f.Timer.Snapshot.ReasonKey);
        Assert.Equal(TimeSpan.FromSeconds(120), f.Timer.Snapshot.WorkElapsed);
        Assert.False(f.Timer.Snapshot.TimerEnded);
        f.Activity = Active;
        f.Advance(1);
        Assert.Equal(TimeSpan.FromSeconds(121), f.Timer.Snapshot.WorkElapsed);
    }

    [Fact]
    public void IdleHeuristicCanBeDisabledForReading()
    {
        var f = new Fixture(new() { Enabled = true, WorkMinutes = 5, PauseOnIdle = false });
        f.Activity = Active with { IdleFor = TimeSpan.FromMinutes(15) };
        f.Advance(300);
        Assert.Equal(HealthReminderState.Reminder, f.Timer.Snapshot.State);
    }

    [Fact]
    public void InputReadFailurePausesEvenWhenIdleHeuristicIsDisabled()
    {
        var f = new Fixture(new() { Enabled = true, WorkMinutes = 5, PauseOnIdle = false });
        f.Advance(20);
        f.Activity = Active with { InputAvailable = false };
        f.Advance(200);
        Assert.Equal("无法读取输入时间，计时已暂停", f.Timer.Snapshot.ReasonKey);
        Assert.Equal(TimeSpan.FromSeconds(20), f.Timer.Snapshot.WorkElapsed);
        f.Activity = Active;
        f.Advance(1);
        Assert.Equal("正在等待会话稳定", f.Timer.Snapshot.ReasonKey);
    }

    [Fact]
    public void UnlockKeepsWorkAndWaitsThirtySeconds()
    {
        var f = new Fixture();
        f.Advance(280);
        f.Activity = Active with { SessionAvailable = false };
        f.Advance(100);
        f.Activity = Active;
        f.Advance(30);
        Assert.Equal(TimeSpan.FromSeconds(280), f.Timer.Snapshot.WorkElapsed);
        f.Advance(21);
        Assert.Equal(HealthReminderState.Reminder, f.Timer.Snapshot.State);
    }

    [Fact]
    public void FullscreenDefersOnlyOneReminderAndWaitsAfterExit()
    {
        var f = new Fixture();
        f.Activity = Active with { IsFullscreen = true };
        f.Advance(1000);
        Assert.Equal("因全屏延后", f.Timer.Snapshot.ReasonKey);
        Assert.Equal(TimeSpan.FromMinutes(5), f.Timer.Snapshot.WorkElapsed);
        f.Activity = Active;
        f.Advance(30);
        Assert.Equal(HealthReminderState.Paused, f.Timer.Snapshot.State);
        f.Advance(1);
        Assert.Equal(HealthReminderState.Reminder, f.Timer.Snapshot.State);
    }

    [Fact]
    public void UnknownFullscreenStateConservativelyDefers()
    {
        var f = new Fixture();
        f.Activity = Active with { FullscreenKnown = false };
        f.Advance(301);
        Assert.Equal("无法确认全屏状态，提醒已延后", f.Timer.Snapshot.ReasonKey);
    }

    [Fact]
    public void ContinuousTypingDoesNotPostponeForeverOutsideFullscreen()
    {
        var f = new Fixture();
        f.Activity = Active with { IdleFor = TimeSpan.Zero };
        f.Advance(300);
        Assert.Equal(HealthReminderState.Deferred, f.Timer.Snapshot.State);
        f.Advance(60);
        Assert.Equal(HealthReminderState.Reminder, f.Timer.Snapshot.State);
    }

    [Fact]
    public void ReminderTimeoutMeansSnoozeNotCompletion()
    {
        var f = new Fixture();
        f.Advance(300);
        f.Advance(20);
        Assert.Equal(HealthReminderState.Snoozed, f.Timer.Snapshot.State);
        Assert.False(f.Timer.Snapshot.TimerEnded);
        Assert.Equal(TimeSpan.FromMinutes(5), f.Timer.Snapshot.WorkElapsed);
    }

    [Fact]
    public void SnoozeUsesEffectiveTimeAndPreservesTheOriginalCycle()
    {
        var f = new Fixture();
        f.Advance(300);
        f.Timer.Snooze();
        f.Tick();
        f.Advance(30);
        f.Activity = Active with { IdleFor = TimeSpan.FromMinutes(5) };
        f.Advance(600);
        Assert.Equal(TimeSpan.FromSeconds(30), f.Timer.Snapshot.Remaining);
        f.Activity = Active;
        f.Advance(30);
        Assert.Equal(HealthReminderState.Reminder, f.Timer.Snapshot.State);
    }

    [Fact]
    public void SkipStartsANewCycleWithoutCompletingARest()
    {
        var f = new Fixture();
        f.Advance(300);
        f.Timer.Skip();
        f.Tick();
        Assert.Equal(TimeSpan.Zero, f.Timer.Snapshot.WorkElapsed);
        Assert.Equal(TimeSpan.FromMinutes(5), f.Timer.Snapshot.Remaining);
        Assert.False(f.Timer.Snapshot.TimerEnded);
    }

    [Fact]
    public void InputDoesNotDismissAnActiveCountdown()
    {
        var f = new Fixture();
        f.Advance(100);
        f.Timer.StartBreak();
        f.Activity = Active with { IdleFor = TimeSpan.Zero };
        f.Advance(10);
        Assert.Equal(HealthReminderState.Breaking, f.Timer.Snapshot.State);
        Assert.Equal(TimeSpan.FromSeconds(10), f.Timer.Snapshot.Remaining);
        Assert.Equal(TimeSpan.FromSeconds(100), f.Timer.Snapshot.WorkElapsed);
    }

    [Fact]
    public void CountdownCompletesAcrossSuspendWithoutShowingAWindow()
    {
        var f = new Fixture();
        f.Timer.StartBreak();
        f.Activity = Active with { SessionAvailable = false };
        f.Clock.Elapsed += TimeSpan.FromMinutes(3);
        f.Tick();
        Assert.True(f.Timer.Snapshot.TimerEnded);
        Assert.False(f.Timer.Snapshot.CanPresent);
        Assert.Equal(TimeSpan.Zero, f.Timer.Snapshot.WorkElapsed);
    }

    [Fact]
    public void EarlyEndSnoozesWithoutClaimingCompletion()
    {
        var f = new Fixture();
        f.Advance(100);
        f.Timer.StartBreak();
        f.Advance(10);
        f.Timer.Snooze();
        f.Tick();
        Assert.Equal(HealthReminderState.Snoozed, f.Timer.Snapshot.State);
        Assert.Equal(TimeSpan.FromSeconds(100), f.Timer.Snapshot.WorkElapsed);
        Assert.False(f.Timer.Snapshot.TimerEnded);
        f.Advance(60);
        Assert.Equal(HealthReminderState.Reminder, f.Timer.Snapshot.State);
    }

    [Fact]
    public void ManualPauseCancelsTheCountdownButKeepsWork()
    {
        var f = new Fixture();
        f.Advance(100);
        f.Timer.StartBreak();
        f.Advance(10);
        f.Timer.PauseUntil(f.Clock.UtcNow + TimeSpan.FromHours(1));
        f.Advance(60);
        Assert.Equal(HealthReminderState.Paused, f.Timer.Snapshot.State);
        f.Timer.Resume();
        f.Tick();
        Assert.Equal(HealthReminderState.Working, f.Timer.Snapshot.State);
        Assert.Equal(TimeSpan.FromSeconds(100), f.Timer.Snapshot.WorkElapsed);
    }

    [Fact]
    public void RestoredFuturePauseAndExpiryDoNotCountPausedTime()
    {
        var f = new Fixture();
        f.Timer.RestorePause(f.Clock.UtcNow + TimeSpan.FromSeconds(60));
        f.Advance(59);
        Assert.True(f.Timer.Snapshot.IsManuallyPaused);
        f.Advance(1);
        Assert.False(f.Timer.Snapshot.IsManuallyPaused);
        Assert.Equal(TimeSpan.Zero, f.Timer.Snapshot.WorkElapsed);
    }

    [Fact]
    public void SavingNewDurationsDoesNotRewriteCurrentWorkBreakOrSnooze()
    {
        var f = new Fixture();
        f.Advance(100);
        var next = new HealthReminderSettings { Enabled = true, WorkMinutes = 10, BreakSeconds = 60, SnoozeMinutes = 2 };
        f.Timer.ApplySaved(next);
        f.Tick();
        Assert.Equal(TimeSpan.FromSeconds(200), f.Timer.Snapshot.Remaining);
        f.Timer.StartBreak();
        f.Advance(10);
        next.BreakSeconds = 120;
        f.Timer.ApplySaved(next);
        f.Tick();
        Assert.Equal(TimeSpan.FromSeconds(50), f.Timer.Snapshot.Remaining);
        f.Timer.Snooze();
        f.Advance(10);
        next.SnoozeMinutes = 3;
        f.Timer.ApplySaved(next);
        f.Tick();
        Assert.Equal(TimeSpan.FromSeconds(110), f.Timer.Snapshot.Remaining);
        f.Timer.Skip();
        f.Tick();
        Assert.Equal(TimeSpan.FromMinutes(10), f.Timer.Snapshot.Remaining);
    }

    [Fact]
    public void PrivacyInterruptionDefersAReminderAndDoesNotCancelABreak()
    {
        var f = new Fixture();
        f.Advance(300);
        f.Timer.DeferPresentation();
        f.ScreenAvailable = false;
        f.Advance(40);
        Assert.Equal(HealthReminderState.Deferred, f.Timer.Snapshot.State);
        f.Timer.StartBreak();
        f.Advance(10);
        f.Timer.DeferPresentation();
        f.Tick();
        Assert.Equal(HealthReminderState.Breaking, f.Timer.Snapshot.State);
        Assert.Equal(TimeSpan.FromSeconds(10), f.Timer.Snapshot.Remaining);
    }

    [Fact]
    public void PreviewDefersDueReminderWithoutCompletingOrSnoozingCycle()
    {
        var f = new Fixture();
        f.Advance(299);
        f.Clock.Elapsed += TimeSpan.FromSeconds(1);
        f.Timer.Tick(Active, true, suppressReminder: true);
        Assert.Equal(HealthReminderState.Deferred, f.Timer.Snapshot.State);
        Assert.True(f.Timer.IsPresentationAllowed);
        f.Tick();
        Assert.Equal(HealthReminderState.Reminder, f.Timer.Snapshot.State);
    }

    [Fact]
    public void DisablingCancelsPendingAndReenablingStartsFresh()
    {
        var f = new Fixture();
        f.Advance(300);
        f.Timer.ApplySaved(new HealthReminderSettings());
        f.Tick();
        Assert.Equal(HealthReminderState.Disabled, f.Timer.Snapshot.State);
        f.Timer.ApplySaved(new HealthReminderSettings { Enabled = true });
        f.Tick();
        Assert.Equal(TimeSpan.FromMinutes(50), f.Timer.Snapshot.Remaining);
    }

    private sealed class Fixture
    {
        public FakeHealthClock Clock { get; } = new();
        public HealthReminderTimer Timer { get; }
        public HealthActivitySnapshot Activity { get; set; } = Active;
        public bool ScreenAvailable { get; set; } = true;
        public Fixture(HealthReminderSettings? settings = null)
        {
            Timer = new HealthReminderTimer(Clock);
            Timer.ApplySaved(settings ?? new() { Enabled = true, WorkMinutes = 5, BreakSeconds = 20, SnoozeMinutes = 1 });
            Tick();
        }
        public void Tick() => Timer.Tick(Activity, ScreenAvailable);
        public void Advance(int seconds)
        {
            for (int i = 0; i < seconds; i++)
            {
                Clock.Elapsed += TimeSpan.FromSeconds(1);
                Clock.UtcNow += TimeSpan.FromSeconds(1);
                Tick();
            }
        }
    }
}

internal sealed class FakeHealthClock : IHealthClock
{
    public TimeSpan Elapsed { get; set; }
    public DateTimeOffset UtcNow { get; set; } = new(2026, 9, 17, 0, 0, 0, TimeSpan.Zero);
}
