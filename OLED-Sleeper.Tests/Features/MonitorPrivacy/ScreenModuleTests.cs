using Moq;
using OLED_Sleeper.Features.MonitorBehavior.Models;
using OLED_Sleeper.Features.MonitorIdleDetection.Services;
using OLED_Sleeper.Features.MonitorInformation.Models;
using OLED_Sleeper.Features.MonitorPrivacy;
using OLED_Sleeper.Features.UserSettings.Models;
using OLED_Sleeper.Features.UserSettings.Services.Interfaces;
using OLED_Sleeper.UI.Services;
using OLED_Sleeper.UI.Services.Interfaces;
using OLED_Sleeper.UI.ViewModels;
using System.Text.Json;
using System.Windows;

namespace OLED_Sleeper.Tests.Features.MonitorPrivacy;

public class ScreenModuleTests
{
    [Theory]
    [InlineData(false, false)]
    [InlineData(true, true)]
    public void LegacyPrivacyKeepsItsEffectiveSwitch(bool managed, bool enabled)
    {
        var setting = JsonSerializer.Deserialize<MonitorSettings>($$"""{"IsManaged":{{managed.ToString().ToLowerInvariant()}},"PrivacyMode":1}""")!;
        Assert.Equal(enabled, setting.HasPrivacy);
        var vm = new MonitorConfigurationViewModel(new() { HardwareId = "fixture" });
        vm.ApplySettings(setting); Assert.Equal(enabled, vm.IsPrivacyEnabled);
        vm.IsManaged = !managed; Assert.Equal(enabled, vm.IsPrivacyEnabled);
        vm.IsPrivacyEnabled = true;
        Assert.True(JsonSerializer.Deserialize<MonitorSettings>(JsonSerializer.Serialize(vm.ToSettings()))!.HasPrivacy);
    }
    [Fact]
    public void SavingOneModuleUsesOtherBaselineAndKeepsItsInvalidDraft()
    {
        var monitor = new MonitorLayoutViewModel(new() { HardwareId = "fixture", Bounds = new(0, 0, 1920, 1080) }, 1, new(0, 0, 1920, 1080), 0, 0);
        var vm = monitor.Configuration; vm.RestEnabled = true; vm.IsPrivacyEnabled = true; vm.MarkAsSaved();
        List<MonitorSettings>? written = null;
        var files = new Mock<IMonitorSettingsFileService>();
        files.Setup(f => f.SaveSettings(It.IsAny<List<MonitorSettings>>())).Callback<List<MonitorSettings>>(v => written = v).Returns(true);
        var save = new MonitorSettingsSaveService(files.Object, Mock.Of<IDialogService>());
        vm.PrivacyIdleText = "bad"; vm.IdleValueText = "60";
        Assert.True(save.TrySave([monitor], MonitorSettingsScope.Rest));
        Assert.Equal(30, written![0].PrivacyIdleSeconds); Assert.Equal(60, written[0].IdleValue);
        Assert.True(vm.IsPrivacyDirty); Assert.False(vm.IsRestDirty); Assert.Equal("bad", vm.PrivacyIdleText);
        vm.PrivacyIdleText = "45"; vm.IdleValueText = "invalid";
        vm.QuietWhileInUse = true;
        Assert.True(save.TrySave([monitor], MonitorSettingsScope.Privacy));
        Assert.Equal(60, written[0].IdleValue); Assert.Equal(45, written[0].PrivacyIdleSeconds);
        Assert.True(written[0].PrivacyOptions!.QuietWhileInUse);
        Assert.True(vm.IsRestDirty); Assert.False(vm.IsPrivacyDirty); Assert.Equal("invalid", vm.IdleValueText);
        vm.Revert(MonitorSettingsScope.Rest); Assert.Equal(60, vm.IdleValue); Assert.True(vm.QuietWhileInUse);
        vm.PrivacyIdleText = "90"; files.Setup(f => f.SaveSettings(It.IsAny<List<MonitorSettings>>())).Returns(false);
        Assert.False(save.TrySave([monitor], MonitorSettingsScope.Privacy)); Assert.True(vm.IsPrivacyDirty);
        vm.Revert(MonitorSettingsScope.Privacy); Assert.Equal(45, vm.PrivacyIdleSeconds);
    }
    [Fact]
    public void CopyIsScopedPreservesTargetSwitchesAndValidatesQuietChoices()
    {
        var source = new MonitorConfigurationViewModel(new() { HardwareId = "one" });
        var target = new MonitorConfigurationViewModel(new() { HardwareId = "two" });
        source.RestEnabled = true; source.IsPrivacyEnabled = true; source.PrivacyIdleSeconds = 60; source.QuietWhileInUse = true;
        target.IdleValue = 90; target.MarkAsSaved();
        source.CopyTo(target, MonitorSettingsScope.Privacy);
        Assert.False(target.IsManaged); Assert.False(target.IsPrivacyEnabled); Assert.Equal(90, target.IdleValue); Assert.Equal(60, target.PrivacyIdleSeconds);
        Assert.True(target.QuietWhileInUse); target.QuietOnMousePosition = false; target.QuietOnActiveWindow = false;
        Assert.True(source.QuietOnMousePosition); target.IsPrivacyEnabled = true; Assert.False(target.IsValid);
        target.QuietOnInput = true; Assert.True(target.IsValid);
        target.QuietWhileInUse = false; target.QuietWhileInUse = true; Assert.True(target.QuietOnInput);
        source.Behavior = MonitorBehaviorType.None; target.RestEnabled = true;
        source.CopyTo(target, MonitorSettingsScope.Rest);
        Assert.True(target.RestEnabled); Assert.Equal(MonitorBehaviorType.Blackout, target.Behavior);
        Assert.True(target.IsPrivacyEnabled); Assert.True(target.QuietOnInput);
    }
    [Fact]
    public void SnoozesAreIndependentJointAndSessionOnly()
    {
        var clock = new Clock(); var service = new ScreenSnoozeService(clock);
        List<MonitorSettings> settings = [new() { HardwareId = "one", IsManaged = true, Behavior = MonitorBehaviorType.Blackout, PrivacyEnabled = true, PrivacyMode = PrivacyMode.Curtain }];
        List<MonitorInfo> monitors = [new() { HardwareId = "one" }]; service.Configure(settings, monitors);
        service.Pause("one", MonitorSettingsScope.Privacy, 1800); var privacyUntil = service.Until("one", MonitorSettingsScope.Privacy);
        service.Pause("one", MonitorSettingsScope.Rest, 60); Assert.False(service.IsJoint("one"));
        service.Resume("one", MonitorSettingsScope.Rest); Assert.Equal(privacyUntil, service.Until("one", MonitorSettingsScope.Privacy));
        service.Pause("one", MonitorSettingsScope.Rest, 60, true); Assert.True(service.IsJoint("one"));
        clock.Tick = 60001; Assert.False(service.IsPaused("one", MonitorSettingsScope.Rest)); Assert.False(service.IsPaused("one", MonitorSettingsScope.Privacy));
        Assert.Equal(60000, service.WaitFrom("one", MonitorSettingsScope.Rest));
        service.Pause("one", MonitorSettingsScope.Privacy, 60, true); service.Configure(settings, monitors); Assert.True(service.IsJoint("one"));
        service.Resume("one", MonitorSettingsScope.Privacy, true); Assert.False(service.IsPaused("one", MonitorSettingsScope.Rest));
        Assert.Equal(clock.Tick, service.WaitFrom("one", MonitorSettingsScope.Privacy));
        service.Pause("one", MonitorSettingsScope.Rest, 60, true); service.Configure(settings, []); Assert.Empty(service.Screens);
        service.Configure(settings, monitors); Assert.False(service.IsPaused("one", MonitorSettingsScope.Rest));
    }
    private sealed class Clock : TimeProvider { public long Tick; public override long TimestampFrequency => 1000; public override long GetTimestamp() => Tick; }
}
