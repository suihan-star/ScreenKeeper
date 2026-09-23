using OLED_Sleeper.Features.MonitorBehavior.Models;
using OLED_Sleeper.Features.MonitorInformation.Models;
using OLED_Sleeper.Features.MonitorPrivacy;
using OLED_Sleeper.Features.UserSettings.Models;
using OLED_Sleeper.UI.Models;
using OLED_Sleeper.UI.ViewModels;
using System.Text.Json;
using System.Windows;

namespace OLED_Sleeper.Tests.Features.MonitorPrivacy;

[Collection("UI language")]
public class SettingsV3Tests
{
    private static MonitorConfigurationViewModel Config() => new(new MonitorInfo
    {
        HardwareId = "privacy-v3-fixture",
        Bounds = new Rect(0, 0, 1920, 1080),
        Capabilities = new DdcCiCapabilities(true, 100)
    });
    [Fact]
    public void DisabledSavedSettingsRememberModeAcrossRestart_WithoutChangingSleep()
    {
        var config = Config();
        config.IsManaged = true; config.Behavior = MonitorBehaviorType.Blackout;
        config.PrivacyMode = PrivacyMode.Application;
        config.SetApplication(@"C:\Apps\calendar.exe");
        config.PrivacyIdleText = "120";
        config.SetImages([Guid.NewGuid().ToString("N") + ".png"]);
        config.UseImages = true; config.LaunchIfMissing = true;
        config.MarkAsSaved();
        config.IsPrivacyEnabled = false;
        Assert.True(config.HasSavedPrivacy); Assert.True(config.IsDirty);
        Assert.Equal(PrivacyMode.None, config.ToSettings().PrivacyMode);
        var restarted = Config();
        restarted.ApplySettings(JsonSerializer.Deserialize<MonitorSettings>(JsonSerializer.Serialize(config.ToSettings()))!);
        restarted.MarkAsSaved();
        Assert.False(restarted.IsPrivacyEnabled); Assert.False(restarted.HasSavedPrivacy); Assert.True(restarted.IsValid);
        restarted.IsPrivacyEnabled = true;
        Assert.Equal(PrivacyMode.Application, restarted.PrivacyMode);
        Assert.Equal(MonitorBehaviorType.Blackout, restarted.Behavior);
        Assert.Equal(120, restarted.PrivacyIdleSeconds); Assert.True(restarted.LaunchIfMissing);
        Assert.Equal(config.Images, restarted.Images); Assert.Equal(config.ApplicationPath, restarted.ApplicationPath);
    }
    [Fact]
    public void OldAndUnknownRememberedModesEnableCurtain_WhileOldDesktopModeIsRetained()
    {
        var config = Config();
        config.ApplySettings(JsonSerializer.Deserialize<MonitorSettings>("{}")!);
        Assert.False(config.IsPrivacyEnabled);
        config.Options.LastEnabledMode = (PrivacyMode)99;
        config.IsPrivacyEnabled = true;
        Assert.Equal(PrivacyMode.Curtain, config.PrivacyMode);
        config.ApplySettings(JsonSerializer.Deserialize<MonitorSettings>("{\"PrivacyMode\":2}")!);
        config.IsPrivacyEnabled = false; config.IsPrivacyEnabled = true;
        Assert.Equal(PrivacyMode.Desktop, config.PrivacyMode);
    }
    [Fact]
    public void DraftRebuildDiscardAndCopyRetainTheirOwnRememberedMode()
    {
        var config = Config(); config.PrivacyMode = PrivacyMode.Desktop; config.IsPrivacyEnabled = false; config.MarkAsSaved();
        config.IsPrivacyEnabled = true; config.PrivacyMode = PrivacyMode.Application; config.SetApplication(@"C:\Apps\calendar.exe");
        config.IsPrivacyEnabled = false;
        var rebuilt = Config(); rebuilt.PreserveDraftFrom(config);
        Assert.True(rebuilt.IsDirty);
        rebuilt.IsPrivacyEnabled = true; Assert.Equal(PrivacyMode.Application, rebuilt.PrivacyMode);
        rebuilt.Revert(); Assert.False(rebuilt.IsDirty); Assert.False(rebuilt.IsPrivacyEnabled);
        rebuilt.IsPrivacyEnabled = true; Assert.Equal(PrivacyMode.Desktop, rebuilt.PrivacyMode);
        var target = Config(); target.MarkAsSaved(); config.CopyTo(target);
        Assert.False(target.IsManaged); Assert.False(target.IsPrivacyEnabled);
        target.IsPrivacyEnabled = true; Assert.Equal(PrivacyMode.Application, target.PrivacyMode);
        target.Revert(); target.IsPrivacyEnabled = true; Assert.Equal(PrivacyMode.Curtain, target.PrivacyMode);
    }
    [Fact]
    public void HiddenAppearanceCannotBlockOtherModes_AndOnlyMultipleImagesNeedTiming()
    {
        var config = Config(); config.IsManaged = true; config.PrivacyMode = PrivacyMode.Curtain; config.UseImages = true;
        Assert.False(config.IsValid);
        config.PrivacyMode = PrivacyMode.Desktop; Assert.True(config.IsValid);
        config.PrivacyMode = PrivacyMode.Application; Assert.False(config.IsValid);
        config.SetApplication(@"C:\Apps\calendar.exe"); Assert.True(config.IsValid);
        config.SetImages([Guid.NewGuid().ToString("N") + ".png"]); config.SlideValueText = "0";
        config.PrivacyMode = PrivacyMode.Curtain; Assert.True(config.IsValid);
        config.SetImages([.. config.Images, Guid.NewGuid().ToString("N") + ".png"]); Assert.False(config.IsValid);
        config.SlideValueText = "30"; Assert.True(config.IsValid);
    }
    [Fact]
    public void PauseDefaultsUseSavedSnapshot_AndSnapshotsCannotMutateDraftOrBaseline()
    {
        var config = Config(); config.PrivacyMode = PrivacyMode.Curtain; config.MarkAsSaved();
        config.PauseValueText = "2"; config.PauseUnit = TimeUnit.Hours;
        var saved = config.SavedPrivacyOptions;
        Assert.Equal(15, saved.PauseValue); Assert.Equal(TimeUnit.Minutes, saved.PauseUnit);
        saved.PauseValue = 999;
        Assert.Equal(15, config.SavedPrivacyOptions.PauseValue); Assert.Equal("2", config.PauseValueText);
        config.MarkAsSaved(); Assert.Equal(2, config.SavedPrivacyOptions.PauseValue);
        Assert.Equal(TimeUnit.Hours, config.SavedPrivacyOptions.PauseUnit);
    }
}
