using OLED_Sleeper.Features.UserSettings.Models;
using OLED_Sleeper.UI.Localization;
using System.Windows.Input;

namespace OLED_Sleeper.UI.ViewModels;

public partial class MainViewModel
{
    private string _currentModule = "Displays";
    public HealthSettingsViewModel? Health { get; }
    public ColorSettingsViewModel? Color { get; }
    public ICommand SelectModuleCommand { get; }
    public string CurrentModule
    {
        get => _currentModule;
        set
        {
            if (_currentModule == value || value is not ("Displays" or "Privacy" or "Health" or "Color")) return;
            if (_currentModule == "Health") Health?.LeavePage();
            if (_currentModule == "Color") Color?.LeavePage();
            _currentModule = value; _saveButtonText = "保存全部设置"; ModulesChanged();
        }
    }
    public bool IsDisplaysPage => CurrentModule == "Displays";
    public bool IsPrivacyPage => CurrentModule == "Privacy";
    public bool IsScreenPage => IsDisplaysPage || IsPrivacyPage;
    public MonitorSettingsScope ScreenScope => IsPrivacyPage ? MonitorSettingsScope.Privacy : MonitorSettingsScope.Rest;
    public string ScreenPageTitle => UiText.Get(IsPrivacyPage ? "隐私保护" : "屏幕休息");
    public string ScreenPageDescription => UiText.Get(IsPrivacyPage ? "离开这块屏幕后，自动隐藏私人内容。" : "每一块屏幕，都有合适的休息方式。");
    public bool IsHealthPage => CurrentModule == "Health";
    public bool IsColorPage => CurrentModule == "Color";
    public bool CurrentModuleDirty => CurrentModule switch { "Health" => Health?.IsDirty == true, "Color" => Color?.IsDirty == true, "Privacy" => Monitors.Any(m => m.Configuration.IsPrivacyDirty), _ => Monitors.Any(m => m.Configuration.IsRestDirty) };
    private bool _compactScreens;
    public bool UseScreenDropdown => _compactScreens || Monitors.Count > 3;
    public bool UseScreenChips => !UseScreenDropdown;
    public void SetCompactLayout(bool compact)
    {
        _compactScreens = compact;
        Color?.SetCompactLayout(compact);
        OnPropertyChanged(nameof(UseScreenDropdown)); OnPropertyChanged(nameof(UseScreenChips));
    }
    public string DisplaysNavigation => UiText.Get("屏幕休息") + (Monitors.Any(m => m.Configuration.IsRestDirty) ? " •" : "");
    public string PrivacyNavigation => UiText.Get("隐私保护") + (Monitors.Any(m => m.Configuration.IsPrivacyDirty) ? " •" : "");
    public string SaveScopeText => IsColorPage && Color?.IsDirty == true ? Color.DraftSummary : UiText.Get(IsScreenPage ? "保存当前模块全部屏幕的修改" : "仅保存当前模块");
    public string HealthNavigation => UiText.Get("健康提醒") + (Health?.IsDirty == true ? " •" : "");
    public string ColorNavigation => UiText.Get("色温调节") + (Color?.IsDirty == true ? " •" : "");
    public string DirtyModules => string.Join(" / ", new[] { Monitors.Any(m => m.Configuration.IsRestDirty) ? UiText.Get("屏幕休息") : null, Monitors.Any(m => m.Configuration.IsPrivacyDirty) ? UiText.Get("隐私保护") : null,
        Health?.IsDirty == true ? UiText.Get("健康提醒") : null, Color?.IsDirty == true ? UiText.Get("色温调节") : null }.Where(s => s != null));
    private void ModulesChanged()
    {
        foreach (var name in new[] { nameof(CurrentModule), nameof(IsDisplaysPage), nameof(IsPrivacyPage), nameof(IsScreenPage), nameof(ScreenPageTitle), nameof(ScreenPageDescription), nameof(PrivacyNavigation), nameof(IsHealthPage), nameof(IsColorPage), nameof(CurrentModuleDirty),
            nameof(DisplaysNavigation), nameof(SaveScopeText), nameof(HealthNavigation), nameof(ColorNavigation), nameof(DirtyModules), nameof(IsDirty), nameof(SaveButtonText), nameof(StatusText), nameof(ConfigurationError) }) OnPropertyChanged(name);
        WindowTitle = UiText.Get(Infrastructure.ProductIdentity.SettingsTitle) + (IsDirty ? "*" : "");
        CommandManager.InvalidateRequerySuggested();
    }
    private void DiscardCurrentModule()
    {
        if (IsHealthPage) Health?.Discard();
        else if (IsColorPage) Color?.Discard();
        else foreach (var monitor in Monitors) monitor.Configuration.Revert(ScreenScope);
        CheckDirtyState(); ModulesChanged();
    }
    public void LeaveSettingsWindow() { Health?.LeavePage(); Color?.LeavePage(); }
}
