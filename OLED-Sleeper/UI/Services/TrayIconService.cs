using Hardcodet.Wpf.TaskbarNotification;
using Microsoft.Win32;
using OLED_Sleeper.Features.MonitorIdleDetection.Services;
using OLED_Sleeper.Features.MonitorPrivacy;
using OLED_Sleeper.Features.UserSettings.Models;
using OLED_Sleeper.Infrastructure;
using OLED_Sleeper.Infrastructure.Helpers;
using OLED_Sleeper.Native;
using OLED_Sleeper.UI.Helpers;
using OLED_Sleeper.UI.Localization;
using OLED_Sleeper.UI.Services.Interfaces;
using OLED_Sleeper.UI.ViewModels;
using Serilog;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;

namespace OLED_Sleeper.UI.Services
{
    /// <summary>
    /// Manages the tray icon, its context menu, and related events for the application.
    /// </summary>
    public class TrayIconService : ITrayIconService
    {
        private readonly IMonitorPrivacyService? _privacy;
        private readonly ScreenSnoozeService? _snooze;
        private readonly Infrastructure.Hosting.WellbeingCoordinator? _wellbeing;
        public TrayIconService(IMonitorPrivacyService? privacy = null, Infrastructure.Hosting.WellbeingCoordinator? wellbeing = null, ScreenSnoozeService? snooze = null) { _privacy = privacy; _wellbeing = wellbeing; _snooze = snooze; }
        private TaskbarIcon? _notifyIcon;
        private System.Drawing.Icon? _ownedIcon;
        private string? _iconResource;
        private Action? _showMainWindowAction;
        private Action? _exitApplicationAction;

        /// <summary>
        /// Initializes and displays the tray icon and its context menu.
        /// Also wires up left-click and double-click to show the main window.
        /// </summary>
        /// <param name="showMainWindowAction">Action to show the main window.</param>
        /// <param name="exitApplicationAction">Action to exit the application.</param>
        public void Initialize(Action showMainWindowAction, Action exitApplicationAction)
        {
            _showMainWindowAction = showMainWindowAction ?? throw new ArgumentNullException(nameof(showMainWindowAction));
            _exitApplicationAction = exitApplicationAction ?? throw new ArgumentNullException(nameof(exitApplicationAction));

            _notifyIcon = new TaskbarIcon { ToolTipText = UiText.Get(ProductIdentity.DisplayName) };

            PropertyChangedEventManager.AddHandler(UiText.Current, OnLanguageChanged, string.Empty);
            if (_privacy != null) _privacy.Changed += SetupContextMenu;
            if (_wellbeing != null) _wellbeing.Health.StateChanged += RefreshHealthTooltip;
            WireTrayIconEvents();
            SetupContextMenu();
            SetTrayIcon();
            SystemEvents.UserPreferenceChanged += OnUserPreferenceChanged;
        }

        /// <summary>
        /// Wires up tray icon mouse events for left-click and double-click actions.
        /// </summary>
        private void WireTrayIconEvents()
        {
            if (_notifyIcon == null) return;

            _notifyIcon.TrayMouseDoubleClick += (_, _) => _showMainWindowAction?.Invoke();
            _notifyIcon.TrayLeftMouseUp += (_, _) => _showMainWindowAction?.Invoke();
        }

        /// <summary>
        /// Sets up the tray icon's context menu.
        /// </summary>
        private void SetupContextMenu()
        {
            if (_notifyIcon == null) return;

            var contextMenu = new ContextMenu();

            contextMenu.Items.Add(CreateShowSettingsMenuItem());
            contextMenu.Items.Add(CreateStartupMenuItem());
            var protect = new MenuItem { Header = UiText.Get("立即保护隐私屏幕"), IsEnabled = _privacy?.HasConfiguredScreens == true };
            protect.Click += async (_, _) => { if (_privacy != null) await _privacy.ProtectAllAsync(); };
            contextMenu.Items.Add(protect);
            var screens = new MenuItem { Header = UiText.Get("按屏幕暂缓") };
            contextMenu.Items.Add(screens);
            contextMenu.Opened += (_, _) => FillScreenSnoozes(screens);
            if (_wellbeing != null)
            {
                contextMenu.Items.Add(new Separator());
                var start = new MenuItem { Header = UiText.Get("开始休息") };
                start.Click += (_, _) => _wellbeing.Health.StartBreak();
                var pause = new MenuItem();
                pause.Click += (_, _) =>
                {
                    if (!_wellbeing.ToggleHealthPause())
                        new UI.Views.AppDialog(UiText.Get("暂停状态保存失败，当前计时保持不变。"), UiText.Get("健康提醒"), false).ShowDialog();
                };
                var original = new MenuItem();
                original.Click += async (_, _) => await _wellbeing.ToggleOriginalAsync();
                contextMenu.Items.Add(start); contextMenu.Items.Add(pause); contextMenu.Items.Add(original);
                contextMenu.Opened += (_, _) =>
                {
                    start.IsEnabled = _wellbeing.Health.Snapshot.State != Features.HealthReminder.HealthReminderState.Disabled;
                    pause.IsEnabled = start.IsEnabled;
                    pause.Header = UiText.Get(_wellbeing.Health.Snapshot.IsManuallyPaused ? "恢复健康提醒" : "暂停提醒 1 小时");
                    original.IsEnabled = _wellbeing.Color.Capabilities.Any(c => c.State == Features.ColorTemperature.ColorCapabilityState.Supported);
                    original.Header = UiText.Get(_wellbeing.Color.States.Any(s => s.State == Features.ColorTemperature.ColorControlState.Paused) ? "恢复已保存策略" : "原色 1 小时");
                };
            }
            contextMenu.Items.Add(new Separator());
            contextMenu.Items.Add(CreateExitMenuItem());

            _notifyIcon.ContextMenu = contextMenu;
        }

        /// <summary>
        /// Creates the menu item used to show the main application settings window.
        /// </summary>
        /// <returns>A configured MenuItem for showing settings.</returns>
        private void FillScreenSnoozes(MenuItem parent)
        {
            parent.Items.Clear();
            foreach (var screen in _snooze?.Screens ?? [])
            {
                var menu = new MenuItem { Header = screen.Name };
                foreach (var scope in new[] { MonitorSettingsScope.Rest, MonitorSettingsScope.Privacy })
                {
                    bool paused = _snooze!.IsPaused(screen.Id, scope), joint = _snooze.IsJoint(screen.Id);
                    var item = new MenuItem { Header = UiText.Get(joint ? "恢复两项" : paused ? scope == MonitorSettingsScope.Rest ? "恢复屏幕休息" : "恢复自动保护" : scope == MonitorSettingsScope.Rest ? "暂缓休息…" : "暂缓保护…"), IsEnabled = _snooze.IsEnabled(screen.Id, scope) };
                    item.Click += (_, _) =>
                    {
                        if (_snooze.IsPaused(screen.Id, scope)) { _snooze.Resume(screen.Id, scope, _snooze.IsJoint(screen.Id)); return; }
                        _showMainWindowAction?.Invoke();
                        if (Application.Current?.MainWindow?.DataContext is MainViewModel main)
                        { main.SelectedMonitor = main.Monitors.FirstOrDefault(m => m.HardwareId == screen.Id); main.CurrentModule = scope == MonitorSettingsScope.Rest ? "Displays" : "Privacy"; main.PauseScreen(scope); }
                    };
                    menu.Items.Add(item);
                    if (joint) break;
                }
                var settings = new MenuItem { Header = UiText.Get("打开设置") };
                settings.Click += (_, _) => { _showMainWindowAction?.Invoke(); if (Application.Current?.MainWindow?.DataContext is MainViewModel main) { main.SelectedMonitor = main.Monitors.FirstOrDefault(m => m.HardwareId == screen.Id); main.CurrentModule = "Displays"; } };
                menu.Items.Add(settings); parent.Items.Add(menu);
            }
            parent.IsEnabled = parent.Items.Count > 0;
        }

        private MenuItem CreateShowSettingsMenuItem()
        {
            var item = new MenuItem { Header = UiText.Get("打开设置") };
            item.Click += (_, _) => _showMainWindowAction?.Invoke();
            return item;
        }

        /// <summary>
        /// Creates the menu item used to toggle the run-at-startup registry setting.
        /// </summary>
        /// <returns>A configured MenuItem for the startup toggle.</returns>
        private MenuItem CreateStartupMenuItem()
        {
            var item = new MenuItem
            {
                Header = UiText.Get("开机启动"),
                IsCheckable = true,
                IsChecked = StartupHelper.IsRunAtStartupEnabled()
            };

            item.Click += HandleStartupMenuItemClick;
            return item;
        }

        /// <summary>
        /// Handles the click event for the startup menu item, updating the registry and reverting UI state on failure.
        /// </summary>
        private void HandleStartupMenuItemClick(object sender, RoutedEventArgs e)
        {
            if (sender is not MenuItem item) return;

            try
            {
                StartupHelper.SetRunAtStartup(item.IsChecked);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to toggle startup registry key.");
                item.IsChecked = !item.IsChecked; // Revert UI state on failure
            }
        }

        /// <summary>
        /// Creates the menu item used to exit the application.
        /// </summary>
        /// <returns>A configured MenuItem for exiting.</returns>
        private MenuItem CreateExitMenuItem()
        {
            var item = new MenuItem { Header = UiText.Get("退出") };
            item.Click += (_, _) => _exitApplicationAction?.Invoke();
            return item;
        }

        /// <summary>
        /// Loads and sets the tray icon from resources.
        /// </summary>
        private void SetTrayIcon()
        {
            if (_notifyIcon == null) return;

            try
            {
                var resource = SystemTaskbarTheme.IconResource;
                if (_iconResource == resource) return;

                var iconUri = new Uri($"pack://application:,,,/Assets/{resource}", UriKind.Absolute);
                using var stream = Application.GetResourceStream(iconUri)!.Stream;
                // Select a native small-icon frame instead of downsampling the default 32 px frame.
                var size = Math.Max(16, NativeMethods.GetSystemMetrics(49)); // SM_CXSMICON
                using var loaded = new System.Drawing.Icon(stream, size, size);
                var replacement = (System.Drawing.Icon)loaded.Clone();
                try
                {
                    _notifyIcon.Icon = replacement;
                }
                catch
                {
                    replacement.Dispose();
                    throw;
                }
                var previous = _ownedIcon;
                _ownedIcon = replacement;
                _iconResource = resource;
                previous?.Dispose();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to load tray icon from resources.");
            }
        }

        private void OnUserPreferenceChanged(object sender, UserPreferenceChangedEventArgs e)
        {
            var dispatcher = _notifyIcon?.Dispatcher;
            if (dispatcher == null || dispatcher.HasShutdownStarted) return;
            dispatcher.BeginInvoke(new Action(SetTrayIcon));
        }

        /// <summary>
        /// Disposes the tray icon and releases resources.
        /// </summary>
        private void OnLanguageChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            RefreshHealthTooltip();
            SetupContextMenu();
        }
        private void RefreshHealthTooltip()
        {
            if (_notifyIcon == null) return;
            var state = _wellbeing?.Health.Snapshot;
            _notifyIcon.ToolTipText = UiText.Get(ProductIdentity.DisplayName) +
                (state?.State == Features.HealthReminder.HealthReminderState.Breaking ? " · " + UiText.Get("休息倒计时") + $" {(int)state.Remaining.TotalMinutes:00}:{state.Remaining.Seconds:00}" : "");
        }

        public void Dispose()
        {
            if (_privacy != null) _privacy.Changed -= SetupContextMenu;
            if (_wellbeing != null) _wellbeing.Health.StateChanged -= RefreshHealthTooltip;
            SystemEvents.UserPreferenceChanged -= OnUserPreferenceChanged;
            PropertyChangedEventManager.RemoveHandler(UiText.Current, OnLanguageChanged, string.Empty);
            _notifyIcon?.Dispose();
            _notifyIcon = null;
            _ownedIcon?.Dispose();
            _ownedIcon = null;
            _iconResource = null;
        }
    }
}
