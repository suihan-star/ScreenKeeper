namespace OLED_Sleeper.Features.HealthReminder;

public enum HealthReminderStyle { Scenic, EdgeGlow, Compact }
public enum HealthRestStyle { Night, Picture }

public sealed class HealthAppearance
{
    public HealthReminderStyle ReminderStyle { get; set; }
    public HealthRestStyle RestStyle { get; set; } = HealthRestStyle.Picture;
    public int FadeSeconds { get; set; } = 8;
    public int OverlayPercent { get; set; } = 30;
    public bool MotionEnabled { get; set; } = true;
    public int MotionStrength { get; set; } = 1;
    public string? Image { get; set; }
    public HealthAppearance Copy() => (HealthAppearance)MemberwiseClone();
    public string? Validate() => !Enum.IsDefined(ReminderStyle) || !Enum.IsDefined(RestStyle) ? "提醒样式无效。" :
        FadeSeconds is < 2 or > 20 ? "渐显时间须为 2–20 秒。" :
        OverlayPercent is < 10 or > 100 ? "覆盖程度须为 10–100%。" :
        MotionStrength is < 1 or > 2 ? "动态强度无效。" :
        Image != null && !MonitorPrivacy.PrivacyImageStore.IsAssetName(Image) ? "提醒图片引用无效。" : null;
}

/// <summary>A global rhythm, independent of per-monitor sleep and privacy management.</summary>
public sealed class HealthReminderSettings
{
    public int Schema { get; set; } = 1;
    public bool Enabled { get; set; }
    public int WorkMinutes { get; set; } = 50;
    public int BreakSeconds { get; set; } = 300;
    public int SnoozeMinutes { get; set; } = 5;
    public bool PauseOnIdle { get; set; } = true;
    public bool FullscreenDnd { get; set; } = true;
    public HealthAppearance Appearance { get; set; } = new();

    public HealthReminderSettings Copy()
    {
        var copy = (HealthReminderSettings)MemberwiseClone();
        copy.Appearance = Appearance?.Copy() ?? new();
        return copy;
    }

    public IReadOnlyList<string> Validate()
    {
        var errors = new List<string>();
        if (Schema != 1) errors.Add("健康提醒配置版本不受支持");
        if (WorkMinutes is < 5 or > 180) errors.Add("工作周期须为 5–180 分钟的整数");
        if (BreakSeconds is < 20 or > 900) errors.Add("休息时长须为 20–900 秒的整数");
        if (SnoozeMinutes is < 1 or > 30) errors.Add("稍后提醒须为 1–30 分钟的整数");
        if (Appearance == null) errors.Add("提醒样式无效。");
        else if (Appearance.Validate() is { } appearanceError) errors.Add(appearanceError);
        return errors;
    }
}
