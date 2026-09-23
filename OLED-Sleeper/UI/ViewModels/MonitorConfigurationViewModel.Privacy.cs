using OLED_Sleeper.Features.MonitorPrivacy;
using OLED_Sleeper.UI.Localization;
using System.Globalization;

namespace OLED_Sleeper.UI.ViewModels;

public partial class MonitorConfigurationViewModel
{
    private PrivacyMode _privacyMode, _initialPrivacyMode;
    private int? _privacyIdleSeconds = 30, _initialPrivacySeconds = 30;
    private string? _privacyIdleText;
    public PrivacyMode PrivacyMode
    {
        get => _privacyMode;
        set
        {
            _privacyMode = value;
            if (value is PrivacyMode.Curtain or PrivacyMode.Desktop or PrivacyMode.Application) Options.LastEnabledMode = value;
            RefreshPrivacy(); UpdateDirtyState();
        }
    }
    public bool IsPrivacyEnabled
    {
        get => PrivacyMode != PrivacyMode.None;
        set => PrivacyMode = value
            ? Options.LastEnabledMode is PrivacyMode.Curtain or PrivacyMode.Desktop or PrivacyMode.Application ? Options.LastEnabledMode : PrivacyMode.Curtain
            : PrivacyMode.None;
    }
    public bool IsPrivacyDisabled => !IsPrivacyEnabled;
    public bool IsCurtain => PrivacyMode == PrivacyMode.Curtain;
    public bool IsDesktop => PrivacyMode == PrivacyMode.Desktop;
    public bool UsesFallbackCurtain => IsDesktop || IsApplication;
    public bool HasSavedPrivacy => _initialPrivacyMode != PrivacyMode.None;
    public string PrivacyEditStatus => PrivacyDraftHint.Length > 0 ? PrivacyDraftHint : UiText.Get(IsPrivacyDirty ? "本屏有未保存修改" : "按已保存设置运行");
    public string QuietConditionsError => IsPrivacyEnabled && QuietWhileInUse && !QuietOnMousePosition && !QuietOnActiveWindow && !QuietOnInput ? UiText.Get("请至少选择一项使用时不打扰的条件。") : "";
    public PrivacyOptions SavedPrivacyOptions => _initialOptions.Copy();
    public string SavedPrivacyRule => UiText.Get("已生效") + " · " + UiText.Get(_initialPrivacyMode switch
    {
        PrivacyMode.Desktop => "回到桌面",
        PrivacyMode.Application => "指定应用",
        _ => "隐私幕"
    }) + " · " + _initialPrivacySeconds / Math.Max(1, PrivacyOptions.Factor(_initialOptions.IdleUnit)) + " " +
        UiText.Get(_initialOptions.IdleUnit switch { UI.Models.TimeUnit.Hours => "小时", UI.Models.TimeUnit.Minutes => "分钟", _ => "秒" });
    public string PrivacyDraftHint => IsPrivacyEnabled == (_initialPrivacyMode != PrivacyMode.None) ? "" :
        UiText.Get(IsPrivacyEnabled ? "保存后启用本屏隐私保护。" : "保存后停用；当前已保存的保护规则仍在运行。");
    public string PrivacyIdleError => IsPrivacyEnabled && PrivacyIdleSeconds is not (>= 1 and <= 86400)
        ? UiText.Get("隐私等待时间必须是 1–86400 秒的整数。") : "";
    public string PauseError => IsPrivacyEnabled && Options.ValidatePause() is { } error ? UiText.Get(error) : "";
    public string AppearanceError => IsCurtain && Options.ValidateAppearance() is { } error ? UiText.Get(error) : "";
    public string ApplicationError => IsApplication && !PrivacyOptions.IsLocalExecutable(ApplicationPath) ? UiText.Get("请选择本地应用程序（.exe）。") : "";
    public bool IsSleepEnabled => Behavior != Features.MonitorBehavior.Models.MonitorBehaviorType.None;
    public int? PrivacyIdleSeconds
    {
        get => _privacyIdleSeconds;
        set { _privacyIdleSeconds = value; _privacyIdleText = null; RefreshPrivacy(); UpdateDirtyState(); }
    }
    public string PrivacyIdleText
    {
        get => _privacyIdleText ?? (PrivacyIdleSeconds / Math.Max(1, PrivacyOptions.Factor(PrivacyUnit)))?.ToString(CultureInfo.InvariantCulture) ?? string.Empty;
        set
        {
            var valid = int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var parsed);
            PrivacyIdleSeconds = valid && PrivacyOptions.ValidDuration(parsed, PrivacyUnit, 86400) ? parsed * PrivacyOptions.Factor(PrivacyUnit) : null;
            _privacyIdleText = value;
            OnPropertyChanged();
        }
    }
    public string PrivacyError => ValidatePrivacy() ?? string.Empty;
    private string? ValidatePrivacy()
    {
        if (!Enum.IsDefined(PrivacyMode)) return UiText.Get("请选择有效的隐私保护方式。");
        if (IsPrivacyEnabled && PrivacyIdleSeconds is not (>= 1 and <= 86400))
            return UiText.Get("隐私等待时间必须是 1–86400 秒的整数。");
        if (!IsPrivacyEnabled) return null;
        return Options.Validate(IsApplication, IsCurtain) is { } error ? UiText.Get(error) : null;
    }
    private void RefreshPrivacy()
    {
        OnPropertyChanged(nameof(PrivacyEditStatus)); OnPropertyChanged(nameof(QuietConditionsError));
        foreach (var name in new[] { nameof(PrivacyMode), nameof(IsApplication), nameof(IsCurtain), nameof(IsDesktop), nameof(UsesFallbackCurtain), nameof(IsPrivacyEnabled), nameof(IsPrivacyDisabled), nameof(HasSavedPrivacy), nameof(SavedPrivacyRule), nameof(PrivacyDraftHint), nameof(PrivacyIdleError), nameof(PauseError), nameof(AppearanceError), nameof(ApplicationError), nameof(PrivacyIdleSeconds), nameof(PrivacyIdleText), nameof(PrivacyError), nameof(BehaviorError), nameof(IdleValueError), nameof(ActiveConditionsError) })
            OnPropertyChanged(name);
    }
}
