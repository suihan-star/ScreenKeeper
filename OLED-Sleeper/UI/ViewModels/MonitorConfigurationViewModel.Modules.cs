using OLED_Sleeper.Features.MonitorBehavior.Models;
using OLED_Sleeper.Features.MonitorPrivacy;
using OLED_Sleeper.Features.UserSettings.Models;
using OLED_Sleeper.UI.Localization;

namespace OLED_Sleeper.UI.ViewModels;

public partial class MonitorConfigurationViewModel
{
    public bool RestEnabled { get => IsManaged; set { if (value && Behavior == MonitorBehaviorType.None) Behavior = MonitorBehaviorType.Blackout; IsManaged = value; } }
    public bool IsRestDirty => IsManaged != _initialIsManaged || Behavior != _initialBehavior || Math.Abs(DimLevel - _initialDimLevel) > .0001 ||
        LowerBrightnessOnBlackout != _initialLowerBrightnessOnBlackout || IdleValue != _initialIdleValue || SelectedTimeUnit != _initialSelectedTimeUnit ||
        IsActiveOnInput != _initialIsActiveOnInput || IsActiveOnMousePosition != _initialIsActiveOnMousePosition || IsActiveOnActiveWindow != _initialIsActiveOnActiveWindow;
    public bool IsPrivacyDirty => !Options.SameAs(_initialOptions) || PrivacyMode != _initialPrivacyMode || PrivacyIdleSeconds != _initialPrivacySeconds;
    public bool QuietWhileInUse { get => Options.QuietWhileInUse; set { Options.QuietWhileInUse = value; OptionsChanged(); } }
    public bool QuietOnMousePosition { get => Options.QuietOnMousePosition; set { Options.QuietOnMousePosition = value; OptionsChanged(); } }
    public bool QuietOnActiveWindow { get => Options.QuietOnActiveWindow; set { Options.QuietOnActiveWindow = value; OptionsChanged(); } }
    public bool QuietOnInput { get => Options.QuietOnInput; set { Options.QuietOnInput = value; OptionsChanged(); } }
    public bool HasSavedRest => _initialIsManaged && _initialBehavior != MonitorBehaviorType.None;
    public string SavedRestRule => !HasSavedRest ? UiText.Get("屏幕休息未开启") : UiText.Get("已保存规则") + " · " +
        UiText.Get(_initialBehavior == MonitorBehaviorType.Dim ? "调暗" : "遮黑") + " · " + _initialIdleValue + " " +
        UiText.Get(_initialSelectedTimeUnit switch { UI.Models.TimeUnit.Hours => "小时", UI.Models.TimeUnit.Minutes => "分钟", _ => "秒" });

    public MonitorSettings ToSettings(MonitorSettingsScope scope)
    {
        var result = ToSettings();
        if (scope == MonitorSettingsScope.Rest)
        {
            result.PrivacyMode = _initialPrivacyMode; result.PrivacyEnabled = _initialPrivacyMode != PrivacyMode.None;
            result.PrivacyIdleSeconds = _initialPrivacySeconds; result.PrivacyOptions = _initialOptions.Copy();
        }
        if (scope == MonitorSettingsScope.Privacy)
        {
            result.IsManaged = _initialIsManaged; result.Behavior = _initialBehavior; result.DimLevel = _initialDimLevel;
            result.LowerBrightnessOnBlackout = _initialLowerBrightnessOnBlackout; result.IdleValue = _initialIdleValue; result.IdleUnit = _initialSelectedTimeUnit;
            result.IsActiveOnInput = _initialIsActiveOnInput; result.IsActiveOnMousePosition = _initialIsActiveOnMousePosition; result.IsActiveOnActiveWindow = _initialIsActiveOnActiveWindow;
        }
        return result;
    }
    public MonitorConfigurationViewModel ForScope(MonitorSettingsScope scope)
    {
        if (scope == MonitorSettingsScope.All) return this;
        var copy = new MonitorConfigurationViewModel(_monitorInfo);
        copy.ApplySettings(ToSettings(scope));
        return copy;
    }
    public void CopyTo(MonitorConfigurationViewModel target, MonitorSettingsScope scope)
    {
        if (scope == MonitorSettingsScope.All) { CopyTo(target); return; }
        if (scope == MonitorSettingsScope.Privacy)
        {
            bool enabled = target.IsPrivacyEnabled;
            target.Options = Options.Copy(); target.PrivacyIdleSeconds = PrivacyIdleSeconds;
            target.PrivacyMode = enabled ? (IsPrivacyEnabled ? PrivacyMode : Options.LastEnabledMode) : PrivacyMode.None;
        }
        else
        {
            target.Behavior = Behavior == MonitorBehaviorType.None || Behavior == MonitorBehaviorType.Dim && !target.IsDimBehaviorEnabled ? MonitorBehaviorType.Blackout : Behavior;
            target.DimLevel = DimLevel; target.LowerBrightnessOnBlackout = LowerBrightnessOnBlackout && target.IsDimBehaviorEnabled;
            target.IdleValue = IdleValue; target.SelectedTimeUnit = SelectedTimeUnit;
            target.IsActiveOnInput = IsActiveOnInput; target.IsActiveOnMousePosition = IsActiveOnMousePosition; target.IsActiveOnActiveWindow = IsActiveOnActiveWindow;
        }
        target.UpdateDirtyState();
    }
}
