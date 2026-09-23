using OLED_Sleeper.Features.MonitorBehavior.Commands;
using OLED_Sleeper.Features.MonitorIdleDetection.Services;
using OLED_Sleeper.Features.MonitorInformation.Models;
using OLED_Sleeper.Features.UserSettings.Models;
using OLED_Sleeper.Infrastructure.Runtime.Interfaces;
using OLED_Sleeper.Messaging.Interfaces;
using Serilog;
using System.Windows;
using System.Windows.Threading;

namespace OLED_Sleeper.Features.MonitorPrivacy;

/// <summary>All state is owned by the UI dispatcher; input callbacks only update timestamps.</summary>
public sealed class MonitorPrivacyService : IMonitorPrivacyService, IDisposable
{
    private sealed class Entry(string id, Rect bounds, PrivacyMode mode, int seconds, long now)
    {
        public string Id = id;
        public Rect Bounds = bounds;
        public PrivacyMode Mode = mode;
        public int Seconds = seconds;
        public PrivacyOptions Options = new();
        public PrivacyTimerState Timer = new(now);
        public bool WakeRequested, NeedsWake, Paused, DesktopFailed, CurtainFailed;
        public int Generation;
        public long? LastRealActivity;
        public long SeenWaitFrom;
        public string QuietReason = "";
        public CancellationTokenSource? DesktopOperation;
    }
    private readonly Dictionary<string, Entry> _entries = new();
    private readonly IDispatcher _dispatcher;
    private readonly IPrivacyActivitySource _input;
    private readonly IPrivacyCurtainHost _curtains;
    private readonly IScreenDesktopService _desktop;
    private readonly IMediator _mediator;
    private readonly IScreenApplicationService? _applications;
    private readonly TimeProvider _clock;
    private readonly ScreenSnoozeService _snooze;
    private DispatcherTimer? _poll;
    private bool _checking, _disposed, _inputFailed, _running, _activityChanged;
    public event Action? Changed;
    public bool IsProtected(string id) => _entries.TryGetValue(id, out var entry) && entry.Timer.IsProtected;
    public bool HasConfiguredScreens => _entries.Count > 0;

    public MonitorPrivacyService(IDispatcher dispatcher, IPrivacyActivitySource input, IPrivacyCurtainHost curtains,
        IScreenDesktopService desktop, IMediator mediator, TimeProvider? clock = null, IScreenApplicationService? applications = null, ScreenSnoozeService? snooze = null)
    {
        _applications = applications;
        _dispatcher = dispatcher;
        _input = input;
        _curtains = curtains;
        _desktop = desktop;
        _mediator = mediator;
        _clock = clock ?? TimeProvider.System;
        _snooze = snooze ?? new ScreenSnoozeService(_clock);
        _input.Activity += OnActivity;
    }
    private long Now => (long)_clock.GetElapsedTime(0, _clock.GetTimestamp()).TotalMilliseconds;
    public bool IsConfigured(string id) => _entries.ContainsKey(id);
    public bool IsPaused(string id) => _snooze.IsPaused(id, MonitorSettingsScope.Privacy);
    public PrivacyRuntimeSnapshot GetSnapshot(string? id)
    {
        var message = GetStatus(id);
        if (id == null || !_entries.TryGetValue(id, out var entry)) return new(PrivacyRuntimeState.Off, 0, message);
        if (_inputFailed || entry.CurtainFailed || entry.DesktopFailed) return new(PrivacyRuntimeState.Unavailable, 0, message);
        if (entry.Timer.IsProtected) return new(PrivacyRuntimeState.Protected, 0, message);
        if (IsPaused(id)) return new(PrivacyRuntimeState.Paused, 0, message);
        if (!_running) return new(PrivacyRuntimeState.Unavailable, 0, "当前自动保护已暂停。");
        if (entry.QuietReason.Length > 0) return new(PrivacyRuntimeState.Quiet, 0, message);
        if (entry.LastRealActivity is { } last && Now - last < 1000) return new(PrivacyRuntimeState.InUse, 0, "本屏操作会重新开始保护等待。");
        return new(PrivacyRuntimeState.Waiting, (int)Math.Clamp(Math.Ceiling(entry.Seconds - (Now - entry.Timer.LastActivity) / 1000d), 0, entry.Seconds), message);
    }
    public string GetStatus(string? id)
    {
        if (_inputFailed) return "输入监听不可用，请重启应用；自动保护仍会按时执行。";
        if (id == null || !_entries.TryGetValue(id, out var entry)) return "保存并启用本屏隐私保护后，可使用立即保护和暂缓。";
        if (entry.CurtainFailed) return "隐私幕无法显示，请使用 Windows 锁屏。";
        if (entry.DesktopFailed) return entry.Mode == PrivacyMode.Application ? _applications?.FailureFor(entry.Bounds) ?? "无法安全切换到指定应用，已保留隐私幕。" : "无法安全回到桌面，已保留隐私幕。";
        if (entry.Timer.IsProtected) return entry.Timer.RequiresExplicitRelease ? "隐私幕已开启，请在该屏幕明确解除。" : (entry.Mode == PrivacyMode.Application ? "已切换到指定应用，不会还原原窗口。" : "已回到桌面，不会自动还原原窗口。");
        return IsPaused(id) ? "自动隐私保护已暂缓，到期后自动恢复。" : entry.QuietReason.Length > 0 ? entry.QuietReason : "按本屏实际操作独立计时。";
    }
    public Task ConfigureAsync(List<MonitorSettings> settings, IReadOnlyList<MonitorInfo> monitors) =>
        _dispatcher.InvokeAsync(() =>
        {
            if (_disposed) return;
            _snooze.Configure(settings, monitors);
            var keep = new HashSet<string>();
            foreach (var setting in settings.Where(s => s.HasPrivacy))
            {
                var monitor = monitors.FirstOrDefault(m => m.HardwareId == setting.HardwareId);
                if (monitor == null) continue;
                keep.Add(setting.HardwareId);
                var mode = Enum.IsDefined(setting.PrivacyMode) ? setting.PrivacyMode : PrivacyMode.Curtain;
                if (mode == PrivacyMode.Application && settings.Any(other => other.HardwareId != setting.HardwareId && other.HasPrivacy && other.PrivacyMode == PrivacyMode.Application &&
                    PrivacyOptions.Conflicts(setting.PrivacyOptions ?? new(), other.PrivacyOptions ?? new()))) mode = PrivacyMode.Curtain;
                var seconds = setting.PrivacyIdleSeconds is >= 1 and <= 86400 ? setting.PrivacyIdleSeconds.Value : 30;
                if (_entries.TryGetValue(setting.HardwareId, out var current))
                {
                    if (current.Bounds != monitor.Bounds || current.Mode != mode || !current.Options.SameAs(setting.PrivacyOptions ?? new()))
                    { CancelDesktop(current); current.Generation++; current.Timer.Defer(Now); }
                    current.Bounds = monitor.Bounds;
                    current.Mode = mode;
                    current.Seconds = seconds;
                    _curtains.Move(current.Id, current.Bounds);
                }
                else _entries.Add(setting.HardwareId, new Entry(setting.HardwareId, monitor.Bounds, mode, seconds, Now));
                var options = (setting.PrivacyOptions ?? new()).Copy();
                _entries[setting.HardwareId].Options = options;
                _curtains.ConfigureAppearance(setting.HardwareId, options);
            }
            foreach (var id in _entries.Keys.Where(id => !keep.Contains(id)).ToArray())
            {
                CancelDesktop(_entries[id]);
                _curtains.Close(id);
                _entries.Remove(id);
            }
            if (_running) Start();
            Changed?.Invoke();
        });

    public void Start() => _ = _dispatcher.InvokeAsync(() =>
    {
        if (_disposed) return;
        _running = true;
        foreach (var entry in _entries.Values) entry.Timer.Defer(Now);
        try { if (HasConfiguredScreens) _input.Start(); else _input.Stop(); _inputFailed = false; }
        catch (Exception ex) { _inputFailed = true; Log.Error(ex, "Privacy input tracking could not start."); }
        _poll ??= new DispatcherTimer(TimeSpan.FromMilliseconds(200), DispatcherPriority.Background, async (_, _) => await CheckAsync(), Dispatcher.CurrentDispatcher);
        _poll.Start();
        Changed?.Invoke();
    });
    public void Stop() => _ = _dispatcher.InvokeAsync(() =>
    {
        if (_disposed) return;
        _running = false; _poll?.Stop(); _input.Stop();
        foreach (var entry in _entries.Values) CancelDesktop(entry);
    });
    private void OnActivity(Point point)
    {
        var now = Now;
        foreach (var entry in _entries.Values)
        {
            if (!entry.Bounds.Contains(point)) continue;
            entry.LastRealActivity = now;
            if (entry.Timer.IsProtected && entry.NeedsWake)
            {
                entry.WakeRequested = true;
                entry.NeedsWake = false;
            }
            if (entry.Timer.IsProtected && !entry.Timer.RequiresExplicitRelease) _activityChanged = true;
            entry.Timer.Activity(now);
        }
    }
    public async Task CheckAsync()
    {
        if (_checking || _disposed) return;
        _checking = true;
        try
        {
            if (_activityChanged) { _activityChanged = false; Changed?.Invoke(); }
            var usage = _input.ReadUsage();
            foreach (var entry in _entries.Values.ToArray())
            {
                if (!_entries.TryGetValue(entry.Id, out var current) || current != entry) continue;
                if (entry.WakeRequested)
                {
                    entry.WakeRequested = false;
                    await _mediator.SendAsync(new RestoreMonitorStateCommand { HardwareId = entry.Id });
                }
                var waitFrom = _snooze.WaitFrom(entry.Id, MonitorSettingsScope.Privacy);
                if (waitFrom != entry.SeenWaitFrom)
                { entry.Timer.RestartWaiting(Math.Max(Now, waitFrom)); entry.SeenWaitFrom = waitFrom; }
                entry.Timer.Defer(waitFrom);
                var quiet = QuietReason(entry, usage);
                if (entry.QuietReason != quiet)
                { if (entry.QuietReason.Length > 0) entry.Timer.Defer(Now); entry.QuietReason = quiet; Changed?.Invoke(); }
                if (!usage.SessionActive || quiet.Length > 0) { entry.Timer.Defer(Now); continue; }
                if (entry.Paused && !IsPaused(entry.Id)) { entry.Paused = false; Changed?.Invoke(); }
                if (entry.Timer.IsDue(Now, entry.Seconds)) await ProtectAsync(entry);
            }
        }
        catch (Exception ex) { Log.Error(ex, "Privacy monitoring failed; existing curtains are retained."); }
        finally { _checking = false; }
    }
    private async Task ProtectAsync(Entry entry)
    {
        if (_disposed) return;
        CancelDesktop(entry);
        var generation = ++entry.Generation;
        entry.Timer.Protect();
        // Hide gentle reminders before creating the curtain or operating user windows.
        Changed?.Invoke();
        entry.NeedsWake = true;
        entry.DesktopFailed = false;
        try
        {
            _curtains.Show(entry.Id, entry.Bounds, () => Release(entry.Id), entry.Mode is PrivacyMode.Desktop or PrivacyMode.Application);
            entry.CurtainFailed = false;
            Changed?.Invoke();
            if (entry.Mode is not (PrivacyMode.Desktop or PrivacyMode.Application)) return;
            using var operation = new CancellationTokenSource();
            entry.DesktopOperation = operation;
            bool succeeded;
            try
            {
                await _dispatcher.InvokeAfterInputAsync(() => { });
                if (entry.Generation != generation || operation.IsCancellationRequested) return;
                succeeded = entry.Mode == PrivacyMode.Application
                    ? _applications != null && await _applications.ShowAsync(entry.Options.Copy(), entry.Bounds, operation.Token)
                    : await _desktop.ShowDesktopAsync(entry.Bounds, operation.Token);
            }
            catch (OperationCanceledException) when (operation.IsCancellationRequested) { return; }
            catch (Exception ex) { succeeded = false; Log.Warning(ex, "Desktop privacy action failed; retaining curtain."); }
            finally { if (ReferenceEquals(entry.DesktopOperation, operation)) entry.DesktopOperation = null; }
            if (!_entries.TryGetValue(entry.Id, out var current) || current != entry || entry.Generation != generation || !entry.Timer.IsProtected) return;
            if (succeeded) { entry.Timer.DesktopReady(); _curtains.DesktopReady(entry.Id); }
            else { entry.DesktopFailed = true; _curtains.DesktopFailed(entry.Id); }
            Changed?.Invoke();
        }
        catch (Exception ex)
        {
            entry.CurtainFailed = true;
            // A failed presentation must not latch the screen as protected forever.
            entry.Timer.Release(Now);
            Log.Error(ex, "Could not show privacy curtain.");
            Changed?.Invoke();
        }
    }
    private void Release(string id)
    {
        if (!_entries.TryGetValue(id, out var entry)) return;
        CancelDesktop(entry);
        entry.Generation++;
        entry.Timer.Release(Now);
        entry.DesktopFailed = false;
        _curtains.Close(id);
        entry.WakeRequested = true;
        Changed?.Invoke();
    }
    private void CancelDesktop(Entry entry)
    {
        if (entry.DesktopOperation == null) return;
        entry.DesktopOperation.Cancel();
        entry.DesktopOperation = null;
        entry.Generation++;
        // An interrupted operation must leave an actionable curtain, never a false desktop success.
        entry.DesktopFailed = true;
        _curtains.DesktopFailed(entry.Id);
    }
    private static string QuietReason(Entry entry, PrivacyUsageSnapshot usage)
    {
        var options = entry.Options;
        if (!options.QuietWhileInUse) return "";
        if (options.QuietOnMousePosition && usage.Cursor is { } cursor && entry.Bounds.Contains(cursor)) return "使用时不打扰：鼠标停留在本屏";
        if (options.QuietOnActiveWindow && usage.Foreground is { } window && Rect.Intersect(entry.Bounds, window) is { IsEmpty: false, Width: > 0, Height: > 0 }) return "使用时不打扰：前台窗口在本屏";
        if (options.QuietOnInput && usage.RecentInput) return "使用时不打扰：电脑有键盘或鼠标输入";
        return "";
    }
    public async Task PrepareForSleepAsync(string id) => await TryPrepareForSleepAsync(id, () => true);
    public async Task<bool> TryPrepareForSleepAsync(string id, Func<bool> stillIdle)
    {
        Task pending = Task.CompletedTask;
        bool allowed = false;
        // Drain queued input before deciding to guard a screen for a pending sleep.
        await _dispatcher.InvokeAfterInputAsync(() =>
        {
            if (_disposed || !stillIdle()) return;
            if (!_entries.TryGetValue(id, out var entry)) { allowed = true; return; }
            var usage = _input.ReadUsage();
            if (!usage.SessionActive) return;
            if (!entry.Timer.IsProtected && !IsPaused(id) &&
                (QuietReason(entry, usage).Length > 0 || entry.LastRealActivity is { } last && Now - last < 1000))
            { entry.Timer.Defer(Now); return; }
            allowed = true;
            entry.NeedsWake = true;
            if (!entry.Timer.IsProtected && !IsPaused(id)) pending = ProtectAsync(entry);
        });
        await pending;
        return !_disposed && allowed && stillIdle();
    }
    public async Task ProtectAllAsync()
    {
        // Called by UI commands. Finish each guard action before reporting success.
        await Task.WhenAll(_entries.Values.ToArray().Select(ProtectAsync));
    }
    public Task ProtectScreenAsync(string id) => _entries.TryGetValue(id, out var entry) && !entry.Timer.IsProtected ? ProtectAsync(entry) : Task.CompletedTask;
    public void TogglePause(string id)
    {
        if (!_entries.TryGetValue(id, out var entry)) return;
        if (IsPaused(id)) { _snooze.Resume(id, MonitorSettingsScope.Privacy); entry.Timer.Resume(Now); entry.Paused = false; }
        else { Pause(id, PrivacyOptions.ValidDuration(entry.Options.PauseValue, entry.Options.PauseUnit, 86400) ? entry.Options.PauseValue * PrivacyOptions.Factor(entry.Options.PauseUnit) : 900); return; }
        Changed?.Invoke();
    }
    public void Pause(string id, int seconds)
    {
        if (!_entries.TryGetValue(id, out var entry)) return;
        _snooze.Pause(id, MonitorSettingsScope.Privacy, seconds); entry.Paused = true; Changed?.Invoke();
    }
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _running = false;
        _poll?.Stop();
        _input.Activity -= OnActivity;
        _input.Dispose();
        foreach (var entry in _entries.Values) CancelDesktop(entry);
        _curtains.Dispose();
        _entries.Clear();
    }
}
