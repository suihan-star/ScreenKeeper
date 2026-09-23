using System.Globalization;
using System.Text.Json.Serialization;

namespace OLED_Sleeper.Features.ColorTemperature;

public enum ColorTemperatureMode { Automatic, Manual }

public class ColorTemperatureProfile
{
    public bool Enabled { get; set; }
    public ColorTemperatureMode Mode { get; set; }
    public string EveningStart { get; set; } = "18:00";
    public string MorningStart { get; set; } = "07:00";
    public int TransitionMinutes { get; set; } = 30;
    public int NightKelvin { get; set; } = 4500;
    public int DayKelvin { get; set; } = 5300;
    public bool DayUsesOriginal { get; set; } = true;
    public bool NightUsesOriginal { get; set; }

    public ColorTemperatureProfile CopyProfile() => new()
    {
        Enabled = Enabled,
        Mode = Mode,
        EveningStart = EveningStart,
        MorningStart = MorningStart,
        TransitionMinutes = TransitionMinutes,
        NightKelvin = NightKelvin,
        DayKelvin = DayKelvin,
        DayUsesOriginal = DayUsesOriginal,
        NightUsesOriginal = NightUsesOriginal
    };
    public virtual string? Validate()
    {
        if (!Enum.IsDefined(Mode)) return "请选择有效的色温模式。";
        if (!ColorTemperatureSettings.TryTime(EveningStart, out var evening) || !ColorTemperatureSettings.TryTime(MorningStart, out var morning))
            return "请输入 24 小时时间，例如 18:00。";
        if (evening == morning) return "转暖与恢复时间不能相同。";
        if (TransitionMinutes is < 0 or > 120) return "过渡时间须为 0–120 分钟。";
        var night = (morning - evening + TimeSpan.FromDays(1)).TotalMinutes % 1440;
        if (TransitionMinutes >= Math.Min(night, 1440 - night)) return "过渡时间须短于相邻时点的间隔。";
        if (NightKelvin is < 3500 or > 6500) return "暖色目标须为 3500–6500 K。";
        if (DayKelvin is < 3500 or > 6500) return "工作色温须为 3500–6500 K。";
        return null;
    }
}

public sealed class ColorTemperatureSettings : ColorTemperatureProfile
{
    public int Schema { get; set; } = 1;
    // Schema 1 is retained for read compatibility only. Schema 2 uses ScreenProfiles exclusively.
    public Dictionary<string, bool> ParticipatingScreens { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, ColorTemperatureProfile>? ScreenProfiles { get; set; }
    [JsonIgnore] public IEnumerable<string> ScreenIds => Schema == 2 ? ScreenProfiles!.Keys : ParticipatingScreens.Keys;
    [JsonIgnore] public bool AnyEnabled => Schema == 2 ? ScreenProfiles!.Values.Any(p => p.Enabled) : Enabled;
    public ColorTemperatureProfile ForScreen(string id)
    {
        if (Schema == 2) return ScreenProfiles!.GetValueOrDefault(id)?.CopyProfile() ?? new();
        var profile = CopyProfile();
        profile.Enabled = Enabled && ParticipatingScreens.GetValueOrDefault(id);
        return profile;
    }
    public ColorTemperatureSettings Copy() => new()
    {
        Schema = Schema,
        Enabled = Enabled,
        Mode = Mode,
        EveningStart = EveningStart,
        MorningStart = MorningStart,
        TransitionMinutes = TransitionMinutes,
        NightKelvin = NightKelvin,
        DayKelvin = DayKelvin,
        DayUsesOriginal = DayUsesOriginal,
        NightUsesOriginal = NightUsesOriginal,
        ParticipatingScreens = new(ParticipatingScreens ?? [], StringComparer.OrdinalIgnoreCase),
        ScreenProfiles = ScreenProfiles?.ToDictionary(p => p.Key, p => p.Value.CopyProfile(), StringComparer.OrdinalIgnoreCase)
    };
    public ColorTemperatureSettings ToPerScreen()
    {
        var next = Copy();
        if (Schema == 1) next.ScreenProfiles = ParticipatingScreens.Keys.ToDictionary(id => id, ForScreen, StringComparer.OrdinalIgnoreCase);
        next.Schema = 2;
        next.Enabled = false;
        next.ParticipatingScreens.Clear();
        return next;
    }
    public void DisableAll()
    {
        Enabled = false;
        if (ScreenProfiles != null) foreach (var profile in ScreenProfiles.Values) profile.Enabled = false;
    }
    public override string? Validate()
    {
        if (Schema is not (1 or 2)) return "色温设置版本不受支持。";
        if (Schema == 1)
        {
            if (ScreenProfiles != null) return "色温设置版本不受支持。";
            if (ParticipatingScreens == null || ParticipatingScreens.Keys.Any(string.IsNullOrWhiteSpace)) return "参与屏幕的身份无效。";
            return base.Validate();
        }
        if (ScreenProfiles == null || ScreenProfiles.Keys.Any(string.IsNullOrWhiteSpace) ||
            ScreenProfiles.Keys.Distinct(StringComparer.OrdinalIgnoreCase).Count() != ScreenProfiles.Count)
            return "屏幕色温配置无效。";
        foreach (var profile in ScreenProfiles.Values)
            if (profile == null || profile.Validate() != null) return profile?.Validate() ?? "屏幕色温配置无效。";
        return null;
    }
    public static bool TryTime(string? value, out TimeSpan time)
    {
        bool valid = TimeOnly.TryParseExact(value, "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed);
        time = valid ? parsed.ToTimeSpan() : TimeSpan.Zero;
        return valid;
    }
}
