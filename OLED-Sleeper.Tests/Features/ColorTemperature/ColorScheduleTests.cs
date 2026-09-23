using OLED_Sleeper.Features.ColorTemperature;
using System.Globalization;
using System.Text.Json;

namespace OLED_Sleeper.Tests.Features.ColorTemperature
{
    public class ColorScheduleTests
    {
        [Fact]
        public void OldConfigurationDefaultsAreIndependentAndDisabled()
        {
            var settings = new ColorTemperatureSettings();
            Assert.False(settings.Enabled);
            Assert.Equal(1, settings.Schema);
            Assert.Empty(settings.ParticipatingScreens);
            Assert.Equal(4500, settings.NightKelvin);
            Assert.Null(settings.Validate());
        }

        [Theory]
        [InlineData("17:59", 0, ColorSchedulePhase.Original)]
        [InlineData("18:00", 0, ColorSchedulePhase.Warming)]
        [InlineData("18:15", .5, ColorSchedulePhase.Warming)]
        [InlineData("18:30", 1, ColorSchedulePhase.Warm)]
        [InlineData("00:00", 1, ColorSchedulePhase.Warm)]
        [InlineData("06:59", 1, ColorSchedulePhase.Warm)]
        [InlineData("07:00", 1, ColorSchedulePhase.Cooling)]
        [InlineData("07:15", .5, ColorSchedulePhase.Cooling)]
        [InlineData("07:30", 0, ColorSchedulePhase.Original)]
        public void FixedScheduleCrossesMidnight(string time, double warmth, ColorSchedulePhase phase)
        {
            var result = ColorSchedule.Evaluate(new() { Enabled = true }, DateTime.ParseExact("2026-09-17 " + time, "yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture));
            Assert.Equal(warmth, result.Warmth, 8);
            Assert.Equal(phase, result.Phase);
        }

        [Theory]
        [InlineData("18:00", "18:00", 30)]
        [InlineData("18:00", "18:30", 30)]
        [InlineData("07:00", "06:30", 30)]
        [InlineData("18:00", "07:00", -1)]
        [InlineData("18:00", "07:00", 121)]
        [InlineData("6:00 PM", "07:00", 30)]
        [InlineData("24:00", "07:00", 30)]
        public void InvalidOrOverlappingSchedulesAreRejected(string evening, string morning, int transition)
        {
            Assert.NotNull(new ColorTemperatureSettings { EveningStart = evening, MorningStart = morning, TransitionMinutes = transition }.Validate());
        }

        [Fact]
        public void ZeroTransitionAndReverseShiftUseCurrentTimeWithoutReplay()
        {
            var settings = new ColorTemperatureSettings { Enabled = true, EveningStart = "07:00", MorningStart = "18:00", TransitionMinutes = 0 };
            Assert.Equal(1, ColorSchedule.Evaluate(settings, new(2026, 9, 17, 7, 0, 0)).Warmth);
            Assert.Equal(0, ColorSchedule.Evaluate(settings, new(2026, 9, 17, 18, 0, 0)).Warmth);
            Assert.Equal(0, ColorSchedule.Evaluate(settings, new(2026, 9, 27, 23, 0, 0)).Warmth);
        }

        [Fact]
        public void CopyDoesNotShareParticipantsAndPreservesOfflinePreference()
        {
            var original = new ColorTemperatureSettings { ParticipatingScreens = new() { ["offline"] = true } };
            var copy = original.Copy();
            copy.ParticipatingScreens["OFFLINE"] = false;
            Assert.True(original.ParticipatingScreens["offline"]);
            Assert.Single(copy.ParticipatingScreens);
        }

        [Fact]
        public void OldJsonKeepsOriginalWorkHoursAndCopyPreservesTwoTargets()
        {
            var old = JsonSerializer.Deserialize<ColorTemperatureSettings>("""{"Schema":1,"NightKelvin":4700}""")!;
            Assert.True(old.DayUsesOriginal); Assert.Equal(5300, old.DayKelvin); Assert.Equal(4700, old.NightKelvin);
            old.DayUsesOriginal = false; old.DayKelvin = 5800;
            var copy = JsonSerializer.Deserialize<ColorTemperatureSettings>(JsonSerializer.Serialize(old.Copy()))!;
            Assert.False(copy.DayUsesOriginal); Assert.Equal(5800, copy.DayKelvin); Assert.Equal(4700, copy.NightKelvin);
            copy.DayKelvin = 3499; Assert.NotNull(copy.Validate());
            copy.DayKelvin = 6501; Assert.NotNull(copy.Validate());
        }

        [Theory]
        [InlineData("17:59", 5800, ColorSchedulePhase.Work)]
        [InlineData("18:00", 5800, ColorSchedulePhase.Warming)]
        [InlineData("18:15", 5000, ColorSchedulePhase.Warming)]
        [InlineData("18:30", 4200, ColorSchedulePhase.Warm)]
        [InlineData("00:00", 4200, ColorSchedulePhase.Warm)]
        [InlineData("07:00", 4200, ColorSchedulePhase.Cooling)]
        [InlineData("07:15", 5000, ColorSchedulePhase.Cooling)]
        [InlineData("07:30", 5800, ColorSchedulePhase.Work)]
        public void WorkAndRestTargetsHaveIndependentFixedScheduleEndpoints(string time, int kelvin, ColorSchedulePhase phase)
        {
            var settings = new ColorTemperatureSettings { Enabled = true, DayUsesOriginal = false, DayKelvin = 5800, NightKelvin = 4200 };
            var result = ColorSchedule.Evaluate(settings, DateTime.ParseExact("2026-09-18 " + time, "yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture));
            Assert.Equal(1, result.Warmth); Assert.Equal(kelvin, result.Kelvin); Assert.Equal(phase, result.Phase);
            settings.Enabled = false; Assert.Equal(0, ColorSchedule.Evaluate(settings, DateTime.Now).Warmth);
        }

        [Fact]
        public void ReverseShiftAndWarmerWorkTargetAreAllowed()
        {
            var settings = new ColorTemperatureSettings { Enabled = true, MorningStart = "18:00", EveningStart = "07:00", DayUsesOriginal = false, DayKelvin = 4000, NightKelvin = 5600 };
            Assert.Equal(4000, ColorSchedule.Evaluate(settings, new(2026, 9, 18, 20, 0, 0)).Kelvin);
            Assert.Equal(5600, ColorSchedule.Evaluate(settings, new(2026, 9, 18, 12, 0, 0)).Kelvin);
            Assert.Equal(4800, ColorSchedule.Evaluate(settings, new(2026, 9, 18, 7, 15, 0)).Kelvin);
        }

        [Fact]
        public void OriginalMeansCapturedGammaNotLinearOr6500()
        {
            var baseline = new ColorValue { Channels = Enumerable.Range(0, 768).Select(i => (ushort)(i * 70)).ToArray() };
            Assert.True(baseline.SameAs(GammaTarget.Create(baseline, 4500, 0)));
            var warm = GammaTarget.Create(baseline, 4500, 1);
            Assert.Equal(baseline.Channels.Take(256), warm.Channels.Take(256));
            Assert.True(warm.Channels[767] < baseline.Channels[767]);
            Assert.NotSame(warm.Channels, baseline.Channels);
        }

        [Theory]
        [InlineData(3499)]
        [InlineData(6501)]
        public void UnverifiedCandidateRangeIsValidated(int kelvin) => Assert.NotNull(new ColorTemperatureSettings { NightKelvin = kelvin }.Validate());

        [Theory]
        [InlineData(-1, 0)]
        [InlineData(0, 0)]
        [InlineData(1999, 0)]
        [InlineData(2000, .2)]
        [InlineData(6000, .6)]
        [InlineData(10000, 1)]
        [InlineData(20000, 1)]
        public void ShortTransitionUsesBoundedMonotonicSteps(long elapsed, double progress)
        {
            var baseline = new ColorValue { Channels = Enumerable.Repeat((ushort)30000, 768).ToArray() };
            var target = new ColorValue { Channels = Enumerable.Repeat((ushort)10000, 768).ToArray() };
            var transition = new ColorTransition(baseline, target, 1000);
            Assert.Equal(progress, transition.Progress(1000 + elapsed), 10);
            Assert.All(transition.Frame(1000 + elapsed).Channels, v => Assert.Equal((ushort)(30000 - 20000 * progress), v));
            Assert.Equal((ushort)30000, baseline.Channels[0]);
        }
    }
}
