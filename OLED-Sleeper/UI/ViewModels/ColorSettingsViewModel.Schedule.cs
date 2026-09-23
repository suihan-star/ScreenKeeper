using OLED_Sleeper.Features.ColorTemperature;
using OLED_Sleeper.UI.Localization;
using System.Globalization;
using System.Windows;

namespace OLED_Sleeper.UI.ViewModels;

public sealed partial class ColorSettingsViewModel
{
    public int ParticipatingScreenCount => _drafts.Values.Count(d => d.Profile.Enabled);
    public bool HasValidSchedule => ColorTemperatureSettings.TryTime(MorningStart, out var morning) &&
        ColorTemperatureSettings.TryTime(EveningStart, out var evening) && morning != evening;
    public string ScheduleInputHint => HasValidSchedule ? "" : UiText.Get("请输入有效且不同的工作、休息开始时间，例如 07:00 和 18:00。");
    public string WorkScheduleLabel => UiText.Get("工作时段") + " · " + MorningStart + " → " + EveningStart + " · " +
        (DayUsesOriginal ? UiText.Get("原色") : DayKelvin + " K");
    public string RestScheduleLabel => UiText.Get("休息时段") + " · " + EveningStart + " → " + MorningStart + " · " + (NightUsesOriginal ? UiText.Get("原色") : NightKelvin + " K");
    public string ScheduleTransitionLabel => UiText.Get("时段交界处按设置逐渐过渡；色块表示时段，不代表实际屏幕颜色。");

    public IReadOnlyList<SchedulePeriod> SchedulePeriods
    {
        get
        {
            if (!HasValidSchedule) return [];
            ColorTemperatureSettings.TryTime(MorningStart, out var morning);
            ColorTemperatureSettings.TryTime(EveningStart, out var evening);
            double first = Math.Min(morning.TotalMinutes, evening.TotalMinutes);
            double second = Math.Max(morning.TotalMinutes, evening.TotalMinutes);
            bool startsAtRest = morning < evening;
            // Split at the two real boundaries, so equal Kelvin targets and short periods remain visible.
            return [Period(0, first, startsAtRest), Period(first, second, !startsAtRest), Period(second, 1440, startsAtRest)];
        }
    }
    private SchedulePeriod Period(double start, double end, bool rest) => new(end - start, rest,
        (rest ? RestScheduleLabel : WorkScheduleLabel) + " · " + FormatMinute(start) + "–" + FormatMinute(end));
    private static string FormatMinute(double minutes) => minutes == 1440 ? "24:00" : TimeSpan.FromMinutes(minutes).ToString(@"hh\:mm", CultureInfo.InvariantCulture);
    public sealed record SchedulePeriod(double Minutes, bool IsRest, string Label)
    {
        public GridLength Width => new(Minutes, GridUnitType.Star);
    }
}
