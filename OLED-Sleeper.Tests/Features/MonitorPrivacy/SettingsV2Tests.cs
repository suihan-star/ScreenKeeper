using OLED_Sleeper.Features.MonitorBehavior.Models;
using OLED_Sleeper.Features.MonitorInformation.Models;
using OLED_Sleeper.Features.MonitorPrivacy;
using OLED_Sleeper.Features.UserSettings.Models;
using OLED_Sleeper.UI.Helpers;
using OLED_Sleeper.UI.Models;
using OLED_Sleeper.UI.ViewModels;
using System.Text.Json;
using System.Windows;

namespace OLED_Sleeper.Tests.Features.MonitorPrivacy;

[Collection("UI language")]
public class SettingsV2Tests
{
    private static MonitorLayoutViewModel Monitor(string id, bool dim = true) => new(new MonitorInfo { HardwareId = id, Bounds = new Rect(0, 0, 100, 100), Capabilities = new DdcCiCapabilities(dim, 100) }, 1, new Rect(0, 0, 100, 100), 0, 0);
    [Fact]
    public void OldSettingsHaveCompatibleDefaults_AndDoNotPersistRuntimeWindowIdentity()
    {
        var settings = JsonSerializer.Deserialize<MonitorSettings>("{\"PrivacyMode\":2,\"PrivacyIdleSeconds\":60}")!;
        Assert.Equal(PrivacyMode.Desktop, settings.PrivacyMode);
        Assert.Equal(15, settings.PrivacyOptions!.PauseValue);
        Assert.False(settings.PrivacyOptions.UseImages);
        settings.PrivacyOptions.WindowToken = "runtime-only";
        Assert.DoesNotContain("runtime-only", JsonSerializer.Serialize(settings));
        Assert.Equal(3, (int)PrivacyMode.Application);
    }
    [Fact]
    public void NewOptionsParticipateInDraftPreservationAndRevert()
    {
        var source = Monitor("A").Configuration;
        source.IsManaged = true; source.PrivacyMode = PrivacyMode.Application;
        source.SetApplication(@"C:\Apps\calendar.exe", "Calendar", "window-1");
        source.PrivacyUnit = TimeUnit.Hours; source.PrivacyIdleText = "2";
        source.PauseUnit = TimeUnit.Hours; source.PauseValueText = "1";
        source.SetImages([Guid.NewGuid().ToString("N") + ".png"]); source.UseImages = true;
        source.SlideUnit = TimeUnit.Minutes; source.SlideValueText = "2";
        Assert.Equal(120, source.ToSettings().PrivacyOptions!.SlideSeconds);
        Assert.Equal(7200, source.ToSettings().PrivacyIdleSeconds);
        Assert.True(source.IsValid);
        var next = Monitor("A").Configuration;
        next.PreserveDraftFrom(source);
        Assert.True(next.Options.SameAs(source.Options));
        Assert.Equal("2", next.PrivacyIdleText);
        next.Revert();
        Assert.Equal(PrivacyMode.None, next.PrivacyMode);
        Assert.False(next.UseImages); Assert.Empty(next.ApplicationPath); Assert.False(next.IsDirty);
    }
    [Theory]
    [InlineData("1.5")]
    [InlineData("0")]
    [InlineData("25")]
    [InlineData("2147483647")]
    public void PrivacyHoursRejectInvalidOrOverflowingDurations(string text)
    {
        var config = Monitor("A").Configuration;
        config.IsManaged = true; config.PrivacyMode = PrivacyMode.Curtain;
        config.PrivacyUnit = TimeUnit.Hours; config.PrivacyIdleText = text;
        Assert.False(config.IsValid);
        var rebuilt = Monitor("A").Configuration; rebuilt.PreserveDraftFrom(config);
        Assert.Equal(text, rebuilt.PrivacyIdleText);
    }
    [Fact]
    public void CopyKeepsTargetManagementAndBaseline_AndAdaptsBrightness()
    {
        var source = Monitor("A").Configuration;
        source.IsManaged = true; source.Behavior = MonitorBehaviorType.Dim; source.PrivacyMode = PrivacyMode.Curtain;
        source.PauseValueText = "2"; source.PauseUnit = TimeUnit.Hours;
        var target = Monitor("B", false).Configuration;
        source.CopyTo(target);
        Assert.False(target.IsManaged); Assert.True(target.IsDirty);
        Assert.Equal(MonitorBehaviorType.Blackout, target.Behavior);
        target.PauseValueText = "3"; Assert.Equal("2", source.PauseValueText);
        target.Revert(); Assert.False(target.IsDirty); Assert.Equal("15", target.PauseValueText);
    }
    [Fact]
    public void DuplicateAppWindowBlocksSave_ButDistinctChosenWindowsAreAllowed()
    {
        var a = Monitor("A"); var b = Monitor("B");
        foreach (var m in new[] { a, b })
        {
            m.Configuration.IsManaged = true; m.Configuration.PrivacyMode = PrivacyMode.Application;
            m.Configuration.SetApplication(@"C:\Apps\calendar.exe", "Calendar", "same");
        }
        Assert.NotNull(MonitorSettingsValidator.BuildValidationError([a, b]));
        b.Configuration.SetApplication(@"C:\Apps\calendar.exe", "Calendar", "different");
        Assert.Null(MonitorSettingsValidator.BuildValidationError([a, b]));
        b.Configuration.SetApplication(@"C:\Apps\calendar.exe");
        Assert.NotNull(MonitorSettingsValidator.BuildValidationError([a, b]));
    }
    [Theory]
    [InlineData(@"\\server\app.exe")]
    [InlineData("https://example.com/app.exe")]
    [InlineData(@"C:\app.cmd")]
    [InlineData(@"C:\app.exe --args")]
    [InlineData("app.exe")]
    public void OnlyLocalExecutablePathsAreAccepted(string path) => Assert.False(PrivacyOptions.IsLocalExecutable(path));
    [Fact]
    public void CustomPauseDoesNotReleaseCurtain_AndResumesAfterDeadline()
    {
        var timer = new PrivacyTimerState(0); timer.Protect(); timer.Pause(1000, 3600);
        Assert.True(timer.RequiresExplicitRelease); Assert.True(timer.IsProtected);
        timer.Release(2000);
        Assert.False(timer.IsDue(3_600_999, 30));
        Assert.True(timer.IsDue(3_601_000, 30));
    }
}
