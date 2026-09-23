using OLED_Sleeper.Features.ColorTemperature;
using OLED_Sleeper.Storage;
using OLED_Sleeper.Tests.TestDoubles;
using System.Text.Json;

namespace OLED_Sleeper.Tests.Features.ColorTemperature;

public sealed class ColorProfileTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void LegacyMigrationPreservesEffectiveStateAndCopiesAllTargets(bool enabled)
    {
        var old = new ColorTemperatureSettings
        {
            Enabled = enabled,
            MorningStart = "08:30",
            EveningStart = "21:15",
            TransitionMinutes = 20,
            DayUsesOriginal = false,
            DayKelvin = 5400,
            NightKelvin = 3900,
            ParticipatingScreens = new() { ["one"] = true, ["two"] = false }
        };
        var migrated = old.ToPerScreen();
        Assert.Null(migrated.Validate()); Assert.Equal(2, migrated.Schema);
        Assert.Equal(enabled, migrated.ForScreen("ONE").Enabled); Assert.False(migrated.ForScreen("two").Enabled);
        foreach (var profile in migrated.ScreenProfiles!.Values)
        {
            Assert.Equal("08:30", profile.MorningStart); Assert.Equal("21:15", profile.EveningStart);
            Assert.Equal(20, profile.TransitionMinutes); Assert.Equal(5400, profile.DayKelvin);
            Assert.Equal(3900, profile.NightKelvin); Assert.False(profile.DayUsesOriginal);
        }
        migrated.ScreenProfiles["one"].DayKelvin = 6000;
        Assert.Equal(5400, migrated.ForScreen("two").DayKelvin); Assert.Equal(5400, old.DayKelvin);
        var copy = migrated.Copy(); copy.ScreenProfiles!["two"].Enabled = true;
        Assert.False(migrated.ForScreen("two").Enabled); Assert.False(migrated.ForScreen("new").Enabled);
    }

    [Fact]
    public void SchemaTwoRoundTripsAndOlderReaderRefusesToFallBackToLegacyBackup()
    {
        var fs = new FakeFileSystem(); var root = new StorageRoot(fs, false); var files = new AppDataFileStore(fs, root);
        var current = new IndependentSettingsStore<ColorTemperatureSettings>(files, fs, root, "color-temperature.json", () => new(), s => s.Validate(), 2);
        Assert.True(current.TrySave(new() { Enabled = true, ParticipatingScreens = new() { ["one"] = true } }));
        var migrated = current.Load().ToPerScreen();
        migrated.ScreenProfiles!["one"].NightKelvin = 3800;
        Assert.True(current.TrySave(migrated));
        var reopened = current.Load(); Assert.Equal(2, reopened.Schema); Assert.Equal(3800, reopened.ForScreen("one").NightKelvin);
        var old = new IndependentSettingsStore<ColorTemperatureSettings>(files, fs, root, "color-temperature.json", () => new(), s => s.Validate());
        Assert.False(old.Load().AnyEnabled); Assert.False(old.IsAvailable); Assert.False(old.TrySave(new()));
        Assert.Equal(3800, current.Load().ForScreen("one").NightKelvin);
    }

    [Theory]
    [InlineData("{\"Schema\":2}")]
    [InlineData("{\"Schema\":2,\"ScreenProfiles\":{\"one\":null}}")]
    [InlineData("{\"Schema\":2,\"ScreenProfiles\":{\"one\":{},\"ONE\":{}}}")]
    [InlineData("{\"Schema\":2,\"ScreenProfiles\":{\"one\":{\"MorningStart\":\"bad\"}}}")]
    public void MalformedPerDisplayDocumentsAreRejected(string json) =>
        Assert.NotNull(JsonSerializer.Deserialize<ColorTemperatureSettings>(json)!.Validate());

    [Fact]
    public void NextMorningUsesTheSelectedDisplaySchedule()
    {
        var now = new DateTime(2026, 9, 20, 8, 0, 0);
        var document = new ColorTemperatureSettings
        {
            Schema = 2,
            ScreenProfiles = new()
            {
                ["one"] = new() { MorningStart = "07:00" },
                ["two"] = new() { MorningStart = "09:30" }
            }
        };
        Assert.Equal(now.Date.AddDays(1).AddHours(7), ColorSchedule.NextMorning(document.ForScreen("one"), now));
        Assert.Equal(now.Date.AddHours(9.5), ColorSchedule.NextMorning(document.ForScreen("two"), now));
    }
}
