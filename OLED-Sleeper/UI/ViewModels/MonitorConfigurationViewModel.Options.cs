using OLED_Sleeper.Features.MonitorPrivacy;
using OLED_Sleeper.UI.Localization;
using OLED_Sleeper.UI.Models;
using System.Globalization;

namespace OLED_Sleeper.UI.ViewModels;

public partial class MonitorConfigurationViewModel
{
    private PrivacyOptions _options = new(), _initialOptions = new();
    public PrivacyOptions Options { get => _options; private set { _options = value; OptionsChanged(); } }
    public System.Windows.Rect ScreenBounds => _monitorInfo.Bounds;
    public TimeUnit PrivacyUnit { get => Options.IdleUnit; set { var text = PrivacyIdleText; Options.IdleUnit = value; PrivacyIdleText = text; OptionsChanged(); } }
    public string PauseValueText { get => Options.PauseValue == 0 ? "" : Options.PauseValue.ToString(CultureInfo.InvariantCulture); set { Options.PauseValue = Parse(value); OptionsChanged(); } }
    public TimeUnit PauseUnit { get => Options.PauseUnit; set { Options.PauseUnit = value; OptionsChanged(); } }
    public bool UseImages { get => Options.UseImages; set { Options.UseImages = value; OptionsChanged(); } }
    public bool UseBuiltInBackground { get => !UseImages && Options.UseBuiltInBackground; set { if (value) { Options.UseImages = false; Options.UseBuiltInBackground = true; OptionsChanged(); } } }
    public bool UseSolid { get => !UseImages && !Options.UseBuiltInBackground; set { if (value) { Options.UseImages = false; Options.UseBuiltInBackground = false; OptionsChanged(); } } }
    public bool HasMultipleImages => Images.Length > 1;
    public string ImageCountText => string.Format(UiText.Get("已选 {0} 张"), Images.Length);
    public string PrivacyAppearanceSummary => UseBuiltInBackground ? UiText.Get("默认山林") : UseImages ? UiText.Get("自定义图片") + " · " + ImageCountText : UiText.Get("深绿纯色");
    public void ApplyPrivacyAppearance(PrivacyOptions appearance)
    {
        Options.UseImages = appearance.UseImages; Options.UseBuiltInBackground = appearance.UseBuiltInBackground;
        Options.Images = appearance.Images.ToArray(); Options.ContainImages = appearance.ContainImages;
        Options.SlideUnit = appearance.SlideUnit; Options.SlideSeconds = appearance.SlideSeconds;
        OptionsChanged();
    }
    public bool ContainImages { get => Options.ContainImages; set { Options.ContainImages = value; OptionsChanged(); } }
    public TimeUnit SlideUnit { get => Options.SlideUnit; set { var text = SlideValueText; Options.SlideUnit = value; SlideValueText = text; } }
    public string SlideValueText
    {
        get => Options.SlideSeconds == 0 ? "" : (Options.SlideSeconds / Math.Max(1, PrivacyOptions.Factor(SlideUnit))).ToString(CultureInfo.InvariantCulture);
        set { var number = Parse(value); Options.SlideSeconds = PrivacyOptions.ValidDuration(number, SlideUnit, 3600) ? number * PrivacyOptions.Factor(SlideUnit) : 0; OptionsChanged(); }
    }
    public string[] Images => Options.Images;
    public string ApplicationPath => Options.ApplicationPath;
    public bool HasApplication => PrivacyOptions.IsLocalExecutable(ApplicationPath);
    public string ApplicationName => HasApplication ? System.IO.Path.GetFileNameWithoutExtension(ApplicationPath) : UiText.Get("尚未选择应用");
    public string ApplicationDetail => Options.WindowLabel ?? UiText.Get(HasApplication ? "使用应用主窗口" : "选择一个适合公开展示的窗口");
    public string ChooseApplicationText => UiText.Get(HasApplication ? "更换" : "选择应用");
    public bool LaunchIfMissing { get => Options.LaunchIfMissing; set { Options.LaunchIfMissing = value; OptionsChanged(); } }
    public bool MoveFromOtherScreen { get => Options.MoveFromOtherScreen; set { Options.MoveFromOtherScreen = value; OptionsChanged(); } }
    public string ApplicationLabel => string.IsNullOrWhiteSpace(Options.WindowLabel) ? Options.ApplicationPath : Options.WindowLabel + "\n" + Options.ApplicationPath;
    public bool IsBlackout => Behavior == Features.MonitorBehavior.Models.MonitorBehaviorType.Blackout;
    public bool IsApplication => PrivacyMode == PrivacyMode.Application;
    public string SavedRule => UiText.Get("已保存规则") + " · " + (!_initialIsManaged ? UiText.Get("未启用管理") :
        UiText.Get(_initialBehavior switch { Features.MonitorBehavior.Models.MonitorBehaviorType.Blackout => "遮黑", Features.MonitorBehavior.Models.MonitorBehaviorType.Dim => "调暗", _ => "不休眠" }) +
        (_initialBehavior == Features.MonitorBehavior.Models.MonitorBehaviorType.None ? "" : " · " + _initialIdleValue + " " + UiText.Get(_initialSelectedTimeUnit switch { TimeUnit.Hours => "小时", TimeUnit.Minutes => "分钟", _ => "秒" })) +
        " · " + UiText.Get(_initialPrivacyMode switch { PrivacyMode.Curtain => "隐私幕", PrivacyMode.Desktop => "回到桌面", PrivacyMode.Application => "指定应用", _ => "隐私保护关闭" }));
    public void RetainWindowIdentityFrom(MonitorConfigurationViewModel source)
    {
        if (Options.ApplicationPath != source.Options.ApplicationPath || Options.WindowClass != source.Options.WindowClass) return;
        Options.WindowToken = _initialOptions.WindowToken = source.Options.WindowToken;
        Options.WindowLabel = _initialOptions.WindowLabel = source.Options.WindowLabel;
        OptionsChanged();
    }
    public void SetApplication(string path, string windowClass = "", string? token = null, string? label = null)
    {
        Options.ApplicationPath = path; Options.WindowClass = windowClass; Options.WindowToken = token; Options.WindowLabel = label; OptionsChanged();
    }
    public void SetImages(IEnumerable<string> images) { Options.Images = images.ToArray(); OptionsChanged(); }
    public void CopyTo(MonitorConfigurationViewModel target)
    {
        var settings = ToSettings();
        settings.IsManaged = target.IsManaged;
        if (!target.IsDimBehaviorEnabled && settings.Behavior == Features.MonitorBehavior.Models.MonitorBehaviorType.Dim)
            settings.Behavior = Features.MonitorBehavior.Models.MonitorBehaviorType.Blackout;
        target.ApplySettings(settings);
    }
    private static int Parse(string value) => int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var number) ? number : 0;
    private void OptionsChanged()
    {
        OnPropertyChanged(nameof(PrivacyAppearanceSummary));
        foreach (var name in new[] { nameof(QuietWhileInUse), nameof(QuietOnMousePosition), nameof(QuietOnActiveWindow), nameof(QuietOnInput), nameof(SavedRestRule), nameof(HasSavedRest), nameof(PrivacyUnit), nameof(PauseValueText), nameof(PauseUnit), nameof(UseImages), nameof(UseSolid), nameof(UseBuiltInBackground), nameof(HasMultipleImages), nameof(ImageCountText), nameof(ContainImages), nameof(SlideValueText), nameof(SlideUnit), nameof(Images), nameof(ApplicationPath), nameof(ApplicationLabel), nameof(ApplicationName), nameof(ApplicationDetail), nameof(ChooseApplicationText), nameof(HasApplication), nameof(LaunchIfMissing), nameof(MoveFromOtherScreen), nameof(SavedRule) }) OnPropertyChanged(name);
        RefreshPrivacy(); UpdateDirtyState();
    }
}
