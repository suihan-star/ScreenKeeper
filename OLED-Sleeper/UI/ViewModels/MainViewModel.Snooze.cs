using OLED_Sleeper.Features.MonitorIdleDetection.Services;
using OLED_Sleeper.Features.UserSettings.Models;
using OLED_Sleeper.UI.Commands;
using OLED_Sleeper.UI.Localization;
using OLED_Sleeper.UI.Views;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace OLED_Sleeper.UI.ViewModels;

public partial class MainViewModel
{
    private ScreenSnoozeService? _snooze;
    private DispatcherTimer? _snoozeTimer;
    public ICommand PauseRestCommand { get; private set; } = null!;
    private void InitializeSnooze(ScreenSnoozeService? snooze)
    {
        _snooze = snooze;
        PauseRestCommand = new RelayCommand(() => PauseScreen(MonitorSettingsScope.Rest), () => SelectedMonitor is { } monitor && _snooze?.IsEnabled(monitor.HardwareId, MonitorSettingsScope.Rest) == true);
        if (_snooze == null) return;
        _snooze.Changed += () => _ = _dispatcher.InvokeAsync(RefreshSnooze);
        _snoozeTimer = new DispatcherTimer(DispatcherPriority.Background) { Interval = TimeSpan.FromSeconds(1) };
        _snoozeTimer.Tick += (_, _) => RefreshSnooze(); _snoozeTimer.Start();
    }
    private void RefreshSnooze()
    {
        OnPropertyChanged(nameof(PauseRestText)); OnPropertyChanged(nameof(RestRuntimeNotice));
        RefreshPrivacyStatus();
    }
    private string PauseText(MonitorSettingsScope scope)
    {
        if (SelectedMonitor is { } monitor && _snooze != null)
        {
            if (_snooze.IsJoint(monitor.HardwareId)) return UiText.Get("恢复两项");
            if (_snooze.IsPaused(monitor.HardwareId, scope)) return UiText.Get(scope == MonitorSettingsScope.Rest ? "恢复屏幕休息" : "恢复自动保护");
        }
        return UiText.Get(scope == MonitorSettingsScope.Rest ? "暂缓休息…" : "暂缓保护…");
    }
    private string SnoozeNotice(MonitorSettingsScope scope)
    {
        if (SelectedMonitor is not { } monitor || _snooze?.IsPaused(monitor.HardwareId, scope) != true) return "";
        var left = TimeSpan.FromMilliseconds(Math.Max(0, _snooze.Until(monitor.HardwareId, scope) - _snooze.Now));
        return string.Format(UiText.Get(_snooze.IsJoint(monitor.HardwareId) ? "休息与保护已暂缓，剩余 {0}" : "已暂缓，剩余 {0}"), $"{(int)left.TotalHours:00}:{left.Minutes:00}:{left.Seconds:00}");
    }
    public string PauseRestText => PauseText(MonitorSettingsScope.Rest);
    public string RestRuntimeNotice => SnoozeNotice(MonitorSettingsScope.Rest);
    public void PauseScreen(MonitorSettingsScope scope)
    {
        if (SelectedMonitor is not { } monitor || _snooze == null || !_snooze.IsEnabled(monitor.HardwareId, scope)) return;
        if (_snooze.IsPaused(monitor.HardwareId, scope))
        { _snooze.Resume(monitor.HardwareId, scope, _snooze.IsJoint(monitor.HardwareId)); return; }
        if (Application.Current?.MainWindow is not { } owner) return;
        var defaults = monitor.Configuration.SavedPrivacyOptions;
        int seconds = scope == MonitorSettingsScope.Privacy && Features.MonitorPrivacy.PrivacyOptions.ValidDuration(defaults.PauseValue, defaults.PauseUnit, 86400)
            ? defaults.PauseValue * Features.MonitorPrivacy.PrivacyOptions.Factor(defaults.PauseUnit) : 900;
        SettingsDialogs.Snooze(owner, monitor.MonitorTitle, scope, seconds,
            _snooze.IsEnabled(monitor.HardwareId, scope == MonitorSettingsScope.Rest ? MonitorSettingsScope.Privacy : MonitorSettingsScope.Rest),
            (duration, both) => _snooze.Pause(monitor.HardwareId, scope, duration, both));
    }
}
