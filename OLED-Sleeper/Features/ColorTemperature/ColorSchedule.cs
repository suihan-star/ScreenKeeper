namespace OLED_Sleeper.Features.ColorTemperature
{
    public enum ColorSchedulePhase { Original, Warming, Warm, Cooling, Work }
    public sealed record ColorScheduleTarget(double Warmth, int Kelvin, ColorSchedulePhase Phase, DateTime NextChange);

    public static class ColorSchedule
    {
        // The date supplied is local wall time. No missed phases are queued across clock/time-zone changes.
        public static ColorScheduleTarget Evaluate(ColorTemperatureProfile settings, DateTime localTime)
        {
            if (settings.Validate() is { } error) throw new ArgumentException(error, nameof(settings));
            if (!settings.Enabled) return new(0, settings.NightKelvin, ColorSchedulePhase.Original, localTime);
            if (settings.Mode == ColorTemperatureMode.Manual)
                return new(1, settings.NightKelvin, ColorSchedulePhase.Warm, DateTime.MaxValue);

            ColorTemperatureSettings.TryTime(settings.EveningStart, out var evening);
            ColorTemperatureSettings.TryTime(settings.MorningStart, out var morning);
            double elapsed = (localTime.TimeOfDay - evening + TimeSpan.FromDays(1)).TotalMinutes % 1440;
            double nightLength = (morning - evening + TimeSpan.FromDays(1)).TotalMinutes % 1440;
            double transition = settings.TransitionMinutes;
            if (transition > 0 && elapsed < transition)
                return Target(elapsed / transition, ColorSchedulePhase.Warming, localTime.AddMinutes(transition - elapsed));
            if (elapsed < nightLength)
                return Target(1, ColorSchedulePhase.Warm, localTime.AddMinutes(nightLength - elapsed));
            if (transition > 0 && elapsed < nightLength + transition)
                return Target(1 - (elapsed - nightLength) / transition, ColorSchedulePhase.Cooling,
                    localTime.AddMinutes(nightLength + transition - elapsed));
            return Target(0, settings.DayUsesOriginal ? ColorSchedulePhase.Original : ColorSchedulePhase.Work, localTime.AddMinutes(1440 - elapsed));

            ColorScheduleTarget Target(double restFraction, ColorSchedulePhase phase, DateTime next)
            {
                if (settings.NightUsesOriginal)
                {
                    // Warmth scales a custom ramp against the captured original, never a synthetic 6500 K ramp.
                    double warmth = settings.DayUsesOriginal ? 0 : 1 - restFraction;
                    return new(warmth, settings.DayKelvin, warmth == 0 ? ColorSchedulePhase.Original : phase, next);
                }
                if (settings.DayUsesOriginal) return new(restFraction, settings.NightKelvin, phase, next);
                // Match the existing sixty schedule steps to bound durable hardware/journal writes.
                double fraction = Math.Round(restFraction * 60) / 60;
                int kelvin = (int)Math.Round(settings.DayKelvin + (settings.NightKelvin - settings.DayKelvin) * fraction);
                return new(1, kelvin, phase, next);
            }
        }

        public static DateTime NextMorning(ColorTemperatureProfile settings, DateTime localTime)
        {
            if (!ColorTemperatureSettings.TryTime(settings.MorningStart, out var morning))
                throw new ArgumentException("请输入 24 小时时间，例如 18:00。", nameof(settings));
            var next = localTime.Date + morning;
            return next > localTime ? next : next.AddDays(1);
        }
    }
}
