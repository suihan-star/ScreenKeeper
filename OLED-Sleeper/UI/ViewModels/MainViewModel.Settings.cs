using OLED_Sleeper.Features.MonitorPrivacy;
using OLED_Sleeper.UI.Views;
using System.Windows;

namespace OLED_Sleeper.UI.ViewModels;

public partial class MainViewModel
{
    private readonly Themes.UiTheme _theme;
    public string Theme
    {
        get => _theme.Theme;
        set
        {
            if (value == Theme || value is not ("forest" or "fog")) return;
            if (_preferences == null) _theme.SetTheme(value);
            else if (!_preferences.TrySetTheme(value))
            {
                _dialogs?.ShowError(Localization.UiText.Get("主题偏好未能保存，请检查配置目录的写入权限后重试。"), Localization.UiText.Get("设置未能保存"));
                OnPropertyChanged();
            }
        }
    }
    public string ThemeIcon => Theme == "fog" ? "/ScreenKeeper;component/Assets/icon.ico" : "/ScreenKeeper;component/Assets/icon-dark.ico";
    public PrivacyImageStore? Images { get; }
    public IApplicationWindowAccess? ApplicationWindows { get; }
    public IScreenApplicationService? Applications { get; }
    public IScreenDesktopService? Desktop { get; }
    private void PauseSelected()
    {
        if (_snooze != null) { PauseScreen(Features.UserSettings.Models.MonitorSettingsScope.Privacy); return; }
        if (SelectedMonitor == null || _privacy == null) return;
        var id = SelectedMonitor.HardwareId;
        if (_privacy.IsPaused(id)) { _privacy.TogglePause(id); return; }
        if (Application.Current?.MainWindow is { } window)
            SettingsDialogs.Pause(window, SelectedMonitor.Configuration.SavedPrivacyOptions, seconds => _privacy.Pause(id, seconds));
        else _privacy.TogglePause(id);
    }
}
