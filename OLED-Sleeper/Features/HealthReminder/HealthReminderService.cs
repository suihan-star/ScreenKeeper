using OLED_Sleeper.Native;
using OLED_Sleeper.UI.Views;
using System.Windows.Threading;

namespace OLED_Sleeper.Features.HealthReminder;

public enum HealthCardAction { StartBreak, Snooze, Skip, EndEarly, Collapse, Close }
public enum HealthCardKind { Reminder, Countdown, Preview, PreviewCountdown }

public interface IHealthReminderPresenter
{
    bool IsVisible { get; }
    string? ScreenDeviceName { get; }
    event Action<HealthCardAction>? ActionRequested;
    void Configure(HealthAppearance appearance) { }
    void Show(HealthCardKind kind, HealthScreen screen, TimeSpan remaining);
    void Update(TimeSpan remaining);
    void Hide();
}

/// <summary>Dispatcher-owned coordination; only this service may present the single global health card.</summary>
public sealed class HealthReminderService : IDisposable
{
    private readonly Dispatcher _dispatcher;
    private readonly Func<string, bool> _isScreenAvailable;
    private readonly IUserActivitySnapshot _activity;
    private readonly IHealthClock _clock;
    private readonly HealthReminderTimer _engine;
    private readonly IHealthReminderPresenter _presenter;
    private readonly DispatcherTimer _poll;
    private bool _started, _disposed, _countdownCollapsed, _preview;
    private TimeSpan _previewEnd;
    private HealthCardKind? _visibleKind;
    private HealthScreen? _presentedScreen;
    private HealthAppearance _appearance = new();
    public string PreviewError { get; private set; } = "";
    public HealthReminderSnapshot Snapshot { get; private set; }
    public bool IsPreviewVisible => _preview;
    public event Action? StateChanged;

    public HealthReminderService(Dispatcher dispatcher, Func<string, bool> isScreenAvailable,
        IUserActivitySnapshot? activity = null, IHealthClock? clock = null, IHealthReminderPresenter? presenter = null)
    {
        _dispatcher = dispatcher;
        _isScreenAvailable = isScreenAvailable;
        _activity = activity ?? new WindowsHealthActivitySnapshot();
        _clock = clock ?? new SystemHealthClock();
        _presenter = presenter ?? new HealthReminderPresenter();
        _engine = new HealthReminderTimer(_clock);
        Snapshot = _engine.Snapshot;
        _poll = new DispatcherTimer(TimeSpan.FromSeconds(1), DispatcherPriority.Background, (_, _) => Refresh(), dispatcher);
        _poll.Stop();
        _activity.Changed += ActivityChanged;
        _presenter.ActionRequested += CardAction;
    }

    public void Start()
    {
        _dispatcher.VerifyAccess();
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_started) return;
        _started = true;
        _activity.Start();
        _poll.Start();
        Refresh();
    }

    public void Stop()
    {
        _dispatcher.VerifyAccess();
        _poll.Stop();
        _activity.Stop();
        _started = false;
        Hide();
    }

    public void ApplySaved(HealthReminderSettings settings)
    {
        _dispatcher.VerifyAccess();
        _engine.ApplySaved(settings);
        _appearance = settings.Appearance.Copy();
        if (!_preview) _presenter.Configure(_appearance);
        if (!settings.Enabled) { Hide(); _countdownCollapsed = false; }
        Refresh();
    }

    public void PauseUntil(DateTimeOffset? until) { _dispatcher.VerifyAccess(); _engine.PauseUntil(until); Hide(); Refresh(); }
    public void RestorePause(DateTimeOffset? until) { _dispatcher.VerifyAccess(); _engine.RestorePause(until); Refresh(); }
    public void Resume() { _dispatcher.VerifyAccess(); _engine.Resume(); Refresh(); }
    public void StartBreak()
    {
        _dispatcher.VerifyAccess();
        var activity = ReadActivity();
        var screen = _presentedScreen ?? activity.ForegroundScreen;
        Hide();
        _countdownCollapsed = screen == null || !_isScreenAvailable(screen.DeviceName);
        _engine.StartBreak();
        var state = _engine.Tick(activity, !_countdownCollapsed, suppressReminder: true);
        // Preserve the clicked card's host before destroying its HWND. Foreground can briefly be null during that handoff.
        if (_started && state.State == HealthReminderState.Breaking && !_countdownCollapsed && screen != null && activity.SessionAvailable && activity.InputAvailable)
        {
            Present(HealthCardKind.Countdown, screen, state.Remaining);
            Publish();
            return;
        }
        Refresh();
    }

    /// <returns>False when privacy, display availability or do-not-disturb blocks a safe preview.</returns>
    public bool Preview(HealthAppearance? appearance = null, bool rest = false)
    {
        _dispatcher.VerifyAccess();
        var snapshot = ReadActivity();
        var screen = snapshot.ForegroundScreen;
        Refresh();
        PreviewError = !_started || !snapshot.SessionAvailable || !snapshot.InputAvailable ? "当前会话不可显示预览，请解锁后重试。" :
            screen == null ? "未找到当前使用的屏幕，请重试。" :
            !_isScreenAvailable(screen.DeviceName) ? "当前屏幕正在休息或受隐私保护，请恢复后重试。" :
            Snapshot.State is HealthReminderState.Reminder or HealthReminderState.Breaking ? "请先结束当前提醒或休息，再预览效果。" : "";
        if (PreviewError.Length > 0 || screen == null) return false;
        _preview = true;
        _presenter.Configure(appearance ?? _appearance);
        _previewEnd = _clock.Elapsed + TimeSpan.FromSeconds(20);
        _visibleKind = rest ? HealthCardKind.PreviewCountdown : HealthCardKind.Preview;
        _presentedScreen = screen;
        _presenter.Show(_visibleKind.Value, screen, TimeSpan.FromSeconds(20));
        Publish();
        return true;
    }

    /// <summary>Privacy/topology can hide a card immediately. An active countdown continues in the tray.</summary>
    public void DismissWindow()
    {
        _dispatcher.VerifyAccess();
        if (_engine.Snapshot.State == HealthReminderState.Breaking) _countdownCollapsed = true;
        else if (!_preview && _engine.Snapshot.State == HealthReminderState.Reminder) _engine.DeferPresentation();
        Hide();
        Refresh();
    }

    public void Refresh()
    {
        _dispatcher.VerifyAccess();
        if (_disposed) return;
        var activity = ReadActivity();
        var screen = activity.ForegroundScreen;
        bool available = screen != null && _isScreenAvailable(screen.DeviceName);
        var snapshot = _engine.Tick(activity, available, suppressReminder: _preview);
        if (!_started) { Publish(); return; }
        bool hostAvailable = _presenter.ScreenDeviceName == null || _isScreenAvailable(_presenter.ScreenDeviceName);
        if (!hostAvailable)
        {
            if (snapshot.State == HealthReminderState.Breaking) _countdownCollapsed = true;
            else if (snapshot.State == HealthReminderState.Reminder)
            {
                // Foreground may already be on another screen. Invalidate the old presentation decision
                // before hiding the original host, otherwise this tick would recreate the card elsewhere.
                _engine.DeferPresentation();
                _engine.Tick(activity, available);
            }
            Hide();
            Publish();
            return;
        }
        if (_preview)
        {
            if (!activity.SessionAvailable || !activity.InputAvailable || !available || _clock.Elapsed >= _previewEnd) Hide();
            else
            {
                _presenter.Update(_previewEnd - _clock.Elapsed);
                Publish();
                return;
            }
        }
        if (!_engine.IsPresentationAllowed)
        {
            if (snapshot.State == HealthReminderState.Breaking && _presenter.IsVisible) _countdownCollapsed = true;
            Hide();
        }
        if (snapshot.State == HealthReminderState.Reminder && snapshot.CanPresent && screen != null)
            Present(HealthCardKind.Reminder, screen, snapshot.Remaining);
        else if (snapshot.State == HealthReminderState.Breaking && snapshot.CanPresent && !_countdownCollapsed && screen != null)
            Present(HealthCardKind.Countdown, screen, snapshot.Remaining);
        else Hide();
        Publish();
    }

    private HealthActivitySnapshot ReadActivity()
    {
        try { return _activity.Read(); }
        catch { return HealthActivitySnapshot.Unavailable; }
    }

    private void Present(HealthCardKind kind, HealthScreen screen, TimeSpan remaining)
    {
        if (!_presenter.IsVisible || _visibleKind != kind)
        {
            _visibleKind = kind;
            _presentedScreen = screen;
            _presenter.Show(kind, screen, remaining);
        }
        else _presenter.Update(remaining);
    }

    private void Hide() { _preview = false; _visibleKind = null; _presentedScreen = null; _presenter.Hide(); _presenter.Configure(_appearance); }

    private void CardAction(HealthCardAction action)
    {
        if (_preview)
        {
            var activity = ReadActivity();
            if (action == HealthCardAction.StartBreak && (_presentedScreen ?? activity.ForegroundScreen) is { } screen &&
                activity.SessionAvailable && activity.InputAvailable && _isScreenAvailable(screen.DeviceName))
            {
                _previewEnd = _clock.Elapsed + TimeSpan.FromSeconds(20);
                _visibleKind = HealthCardKind.PreviewCountdown;
                _presenter.Show(HealthCardKind.PreviewCountdown, screen, TimeSpan.FromSeconds(20));
            }
            else Hide();
            Publish(); return;
        }
        switch (action)
        {
            case HealthCardAction.StartBreak: StartBreak(); return;
            case HealthCardAction.Skip: _engine.Skip(); break;
            case HealthCardAction.Collapse: _countdownCollapsed = true; break;
            default: _engine.Snooze(); break;
        }
        Hide();
        Refresh();
    }

    private void Publish()
    {
        Snapshot = _engine.Snapshot with { CurrentScreenDeviceName = _presenter.ScreenDeviceName };
        StateChanged?.Invoke();
    }

    private void ActivityChanged()
    {
        if (!_disposed && !_dispatcher.HasShutdownStarted) _dispatcher.BeginInvoke(DispatcherPriority.Send, new Action(Refresh));
    }

    public void Dispose()
    {
        if (_disposed) return;
        Stop();
        _disposed = true;
        _activity.Changed -= ActivityChanged;
        _presenter.ActionRequested -= CardAction;
        _activity.Dispose();
    }
}
