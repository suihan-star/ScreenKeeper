using OLED_Sleeper.UI.Models;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace OLED_Sleeper.Features.MonitorPrivacy;

/// <summary>Optional schema-1 extension. Runtime window identity never goes into the settings file.</summary>
public sealed class PrivacyOptions
{
    public PrivacyMode LastEnabledMode { get; set; } = PrivacyMode.Curtain;
    public TimeUnit IdleUnit { get; set; } = TimeUnit.Seconds;
    public int PauseValue { get; set; } = 15;
    public TimeUnit PauseUnit { get; set; } = TimeUnit.Minutes;
    public bool QuietWhileInUse { get; set; }
    public bool QuietOnMousePosition { get; set; } = true;
    public bool QuietOnActiveWindow { get; set; } = true;
    public bool QuietOnInput { get; set; }
    public bool UseImages { get; set; }
    public bool UseBuiltInBackground { get; set; } = true;
    public string[] Images { get; set; } = [];
    public bool ContainImages { get; set; }
    public TimeUnit SlideUnit { get; set; } = TimeUnit.Seconds;
    public int SlideSeconds { get; set; } = 30;
    public string ApplicationPath { get; set; } = string.Empty;
    public string WindowClass { get; set; } = string.Empty;
    public bool LaunchIfMissing { get; set; }
    public bool MoveFromOtherScreen { get; set; }
    [JsonIgnore] public string? WindowLabel { get; set; }
    [JsonIgnore] public string? WindowToken { get; set; }
    public PrivacyOptions Copy()
    {
        var copy = (PrivacyOptions)MemberwiseClone();
        copy.Images = Images?.ToArray() ?? [];
        return copy;
    }
    public bool SameAs(PrivacyOptions other) => WindowToken == other.WindowToken && JsonSerializer.Serialize(this) == JsonSerializer.Serialize(other);
    public static int Factor(TimeUnit unit) => unit switch { TimeUnit.Seconds => 1, TimeUnit.Minutes => 60, TimeUnit.Hours => 3600, _ => 0 };
    public static bool ValidDuration(int? value, TimeUnit unit, int maximumSeconds) => Factor(unit) > 0 && value > 0 && (long)value * Factor(unit) <= maximumSeconds;
    public static bool IsLocalExecutable(string? path) => !string.IsNullOrWhiteSpace(path) && Path.IsPathFullyQualified(path) &&
        path.Length >= 4 && char.IsAsciiLetter(path[0]) && path[1] == ':' && !path.Contains('"') && path.EndsWith(".exe", StringComparison.OrdinalIgnoreCase);
    public string? Validate(bool app, bool curtain = true)
    {
        if (!Enum.IsDefined(IdleUnit)) return "请选择有效的时间单位。";
        if (QuietWhileInUse && !QuietOnMousePosition && !QuietOnActiveWindow && !QuietOnInput) return "请至少选择一项使用时不打扰的条件。";
        if (ValidatePause() is { } pause) return pause;
        if (curtain && ValidateAppearance() is { } appearance) return appearance;
        if (app && !IsLocalExecutable(ApplicationPath)) return "请选择本地应用程序（.exe）。";
        return null;
    }
    public string? ValidatePause() => PauseUnit == TimeUnit.Seconds || !ValidDuration(PauseValue, PauseUnit, 86400)
        ? "暂缓时长必须是 1 分钟至 24 小时的整数。" : null;
    public string? ValidateAppearance()
    {
        if (UseImages && (Images == null || Images.Length is < 1 or > 12 || Images.Any(p => !PrivacyImageStore.IsAssetName(p)))) return "请选择 1–12 张有效的隐私幕图片。";
        if (UseImages && Images.Length > 1 && (SlideUnit is not (TimeUnit.Seconds or TimeUnit.Minutes) || SlideSeconds is < 1 or > 3600)) return "图片轮换间隔必须是 1–3600 秒的整数。";
        return null;
    }
    public static bool Conflicts(PrivacyOptions a, PrivacyOptions b) =>
        string.Equals(a.ApplicationPath, b.ApplicationPath, StringComparison.OrdinalIgnoreCase) &&
        (string.IsNullOrEmpty(a.WindowClass) || string.IsNullOrEmpty(b.WindowClass) || a.WindowClass == b.WindowClass) &&
        (a.WindowToken == null || b.WindowToken == null || a.WindowToken == b.WindowToken);
}
