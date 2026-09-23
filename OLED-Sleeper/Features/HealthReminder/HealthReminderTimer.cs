namespace OLED_Sleeper.Features.HealthReminder;

/// <summary>Pure global timer. Wall time is used only for the explicit persisted pause deadline.</summary>
public sealed class HealthReminderTimer
{
    private enum Phase { Working, Snoozed, Reminder, Breaking }
    private readonly IHealthClock _clock;
    private HealthReminderSettings _settings = new();
    private Phase _phase;
    private TimeSpan _lastTick, _workElapsed, _workTarget, _snoozeRemaining, _breakEnd, _reminderEnd;
    private TimeSpan _stableUntil, _quietWait;
    private bool _haveTick, _wasSessionBlocked, _wasFullscreenBlocked, _manualPause, _timerEnded, _reminderDue;
    private DateTimeOffset? _pauseUntil;
    public HealthReminderSnapshot Snapshot { get; private set; } = new(HealthReminderState.Disabled, "健康提醒已关闭", TimeSpan.Zero, TimeSpan.Zero, null, false, false, false);
    public bool IsPresentationAllowed { get; private set; }

    public HealthReminderTimer(IHealthClock clock) => _clock = clock;

    public void ApplySaved(HealthReminderSettings settings)
    {
        if (settings.Validate().Count != 0) throw new ArgumentException("Invalid health reminder settings.", nameof(settings));
        bool newlyEnabled = settings.Enabled && !_settings.Enabled;
        _settings = settings.Copy();
        if (newlyEnabled || !settings.Enabled)
        {
            NewCycle();
            _haveTick = false;
            _timerEnded = false;
        }
        if (!settings.Enabled) Snapshot = Create(HealthReminderState.Disabled, "健康提醒已关闭", TimeSpan.Zero, false);
    }

    public HealthReminderSnapshot Tick(HealthActivitySnapshot activity, bool screenAvailable, bool suppressReminder = false)
    {
        var now = _clock.Elapsed;
        var delta = _haveTick ? now - _lastTick : TimeSpan.Zero;
        _lastTick = now;
        _haveTick = true;
        if (delta < TimeSpan.Zero || delta > TimeSpan.FromSeconds(5))
        {
            delta = TimeSpan.Zero;
            _stableUntil = now + TimeSpan.FromSeconds(30);
        }
        if (_manualPause && _pauseUntil.HasValue && _clock.UtcNow >= _pauseUntil.Value) { Resume(); delta = TimeSpan.Zero; }
        bool sessionBlocked = !activity.SessionAvailable || !activity.InputAvailable;
        bool fullscreenBlocked = _settings.FullscreenDnd && (!activity.FullscreenKnown || activity.IsFullscreen);
        if ((_wasSessionBlocked && !sessionBlocked) || (_wasFullscreenBlocked && !fullscreenBlocked))
            _stableUntil = now + TimeSpan.FromSeconds(30);
        _wasSessionBlocked = sessionBlocked;
        _wasFullscreenBlocked = fullscreenBlocked;
        bool idle = _settings.PauseOnIdle && activity.IdleFor >= TimeSpan.FromMinutes(5);
        bool stable = now < _stableUntil;
        bool present = !sessionBlocked && !fullscreenBlocked && !idle && !stable && !_manualPause && screenAvailable;
        IsPresentationAllowed = present;
        if (!_settings.Enabled) return Snapshot = Create(HealthReminderState.Disabled, "健康提醒已关闭", TimeSpan.Zero, false);

        if (_manualPause) return Snapshot = Create(HealthReminderState.Paused, "健康提醒已手动暂停", Remaining(), false);
        // A user-started countdown may include lock/suspend time. Completion never proves actual rest.
        if (_phase == Phase.Breaking)
        {
            if (now >= _breakEnd)
            {
                NewCycle();
                _timerEnded = true;
                delta = TimeSpan.Zero;
            }
            else return Snapshot = Create(HealthReminderState.Breaking, "休息倒计时", _breakEnd - now, present);
        }

        string? pauseReason = !activity.SessionAvailable ? "因锁屏或系统休眠暂停" :
            !activity.InputAvailable ? "无法读取输入时间，计时已暂停" :
            idle ? "因无输入暂停" : stable ? "正在等待会话稳定" : null;
        if (pauseReason != null)
        {
            HoldReminder();
            return Snapshot = Create(HealthReminderState.Paused, pauseReason, Remaining(), false);
        }
        if (_phase == Phase.Reminder)
        {
            if (!present || suppressReminder) HoldReminder();
            else if (now >= _reminderEnd) { Snooze(); delta = TimeSpan.Zero; }
            else return Snapshot = Create(HealthReminderState.Reminder, "该休息一下了", _reminderEnd - now, true);
        }
        if (_phase == Phase.Snoozed)
        {
            _snoozeRemaining = MaxZero(_snoozeRemaining - delta);
            if (_snoozeRemaining > TimeSpan.Zero)
                return Snapshot = Create(HealthReminderState.Snoozed, "稍后再提醒", _snoozeRemaining, false);
            _phase = Phase.Working;
            _quietWait = TimeSpan.Zero;
            _reminderDue = true;
        }
        else _workElapsed = Min(_workTarget, _workElapsed + delta);

        if (_workElapsed < _workTarget && !_reminderDue)
            return Snapshot = Create(HealthReminderState.Working, _timerEnded ? "计时已结束，开始新的工作周期" : "工作计时中", _workTarget - _workElapsed, false);
        if (!present || suppressReminder)
        {
            var reason = !activity.FullscreenKnown && _settings.FullscreenDnd ? "无法确认全屏状态，提醒已延后" :
                fullscreenBlocked ? "因全屏延后" : suppressReminder ? "预览结束后提醒" : "等待可用屏幕";
            return Snapshot = Create(HealthReminderState.Deferred, reason, TimeSpan.Zero, false);
        }
        _quietWait += delta;
        if (activity.IdleFor < TimeSpan.FromSeconds(2) && _quietWait < TimeSpan.FromSeconds(60))
            return Snapshot = Create(HealthReminderState.Deferred, "等待较安静的提醒时机", TimeSpan.FromSeconds(60) - _quietWait, false);
        _phase = Phase.Reminder;
        _reminderEnd = now + TimeSpan.FromSeconds(20);
        return Snapshot = Create(HealthReminderState.Reminder, "该休息一下了", TimeSpan.FromSeconds(20), true);
    }

    public void StartBreak()
    {
        if (!_settings.Enabled || _manualPause || _phase == Phase.Breaking) return;
        _phase = Phase.Breaking;
        _breakEnd = _clock.Elapsed + TimeSpan.FromSeconds(_settings.BreakSeconds);
        _timerEnded = false;
    }

    public void Snooze()
    {
        if (!_settings.Enabled) return;
        _phase = Phase.Snoozed;
        _snoozeRemaining = TimeSpan.FromMinutes(_settings.SnoozeMinutes);
        _quietWait = TimeSpan.Zero;
        _lastTick = _clock.Elapsed;
    }

    public void Skip() { if (_settings.Enabled) { NewCycle(); _timerEnded = false; _lastTick = _clock.Elapsed; } }

    public void PauseUntil(DateTimeOffset? until)
    {
        _manualPause = true;
        _pauseUntil = until;
        if (_phase == Phase.Breaking) _phase = Phase.Working;
        HoldReminder();
    }

    public void RestorePause(DateTimeOffset? until)
    {
        if (until > _clock.UtcNow) PauseUntil(until);
        else Resume();
    }

    public void Resume()
    {
        _manualPause = false;
        _pauseUntil = null;
        _lastTick = _clock.Elapsed;
    }

    public void DeferPresentation()
    {
        HoldReminder();
        _stableUntil = _clock.Elapsed + TimeSpan.FromSeconds(30);
    }

    private void HoldReminder()
    {
        if (_phase == Phase.Reminder) { _phase = Phase.Working; _quietWait = TimeSpan.FromSeconds(60); _reminderDue = true; }
    }
    private void NewCycle()
    {
        _phase = Phase.Working;
        _workElapsed = _quietWait = TimeSpan.Zero;
        _reminderDue = false;
        _workTarget = TimeSpan.FromMinutes(_settings.WorkMinutes);
    }
    private TimeSpan Remaining() => _phase == Phase.Breaking ? MaxZero(_breakEnd - _clock.Elapsed) :
        _phase == Phase.Snoozed ? _snoozeRemaining : MaxZero(_workTarget - _workElapsed);
    private HealthReminderSnapshot Create(HealthReminderState state, string reason, TimeSpan remaining, bool present) =>
        new(state, reason, remaining, _workElapsed, _pauseUntil, _manualPause, present, _timerEnded);
    private static TimeSpan MaxZero(TimeSpan time) => time < TimeSpan.Zero ? TimeSpan.Zero : time;
    private static TimeSpan Min(TimeSpan left, TimeSpan right) => left < right ? left : right;
}
