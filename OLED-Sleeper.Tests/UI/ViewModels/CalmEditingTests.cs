using Moq;
using OLED_Sleeper.Features.MonitorBehavior.Models;
using OLED_Sleeper.Features.MonitorInformation.Models;
using OLED_Sleeper.Features.UserSettings.Models;
using OLED_Sleeper.Infrastructure.Runtime.Interfaces;
using OLED_Sleeper.UI.Localization;
using OLED_Sleeper.UI.Models;
using OLED_Sleeper.UI.Services.Interfaces;
using OLED_Sleeper.UI.ViewModels;
using System.Collections.ObjectModel;
using System.Windows;

namespace OLED_Sleeper.Tests.UI.ViewModels;

[CollectionDefinition("UI language", DisableParallelization = true)]
public class UiLanguageCollection { }

[Collection("UI language")]
public class CalmEditingTests
{
    private static MonitorLayoutViewModel Monitor() => new(new MonitorInfo
    {
        HardwareId = "DISPLAY-1",
        DisplayNumber = 1,
        Bounds = new Rect(0, 0, 1920, 1080),
        Capabilities = new DdcCiCapabilities(true, 100)
    }, 0.1, new Rect(0, 0, 1920, 1080), 0, 0);

    [StaFact]
    public void Resize_PreservesInvalidDraftAndDiscardBaseline()
    {
        var workspace = new Mock<IWorkspaceService>();
        workspace.Setup(s => s.BuildWorkspaceAsync(It.IsAny<double>(), It.IsAny<double>()))
            .ReturnsAsync(() => new ObservableCollection<MonitorLayoutViewModel> { Monitor() });
        var dispatcher = new Mock<IDispatcher>();
        dispatcher.SetupGet(d => d.IsOnUiThread).Returns(true);
        var vm = new MainViewModel(workspace.Object, Mock.Of<IMonitorSettingsSaveService>(), dispatcher.Object);
        vm.RecalculateLayout(800, 200);
        vm.SelectedMonitor = vm.Monitors[0];
        var config = vm.SelectedMonitor.Configuration;
        config.IsManaged = true;
        config.Behavior = MonitorBehaviorType.Blackout;
        config.IdleValueText = "not a number";
        vm.RecalculateLayout(640, 180);
        Assert.Equal("not a number", vm.SelectedMonitor!.Configuration.IdleValueText);
        Assert.Null(vm.SelectedMonitor.Configuration.IdleValue);
        Assert.True(vm.IsDirty);
        Assert.False(vm.SaveSettingsCommand.CanExecute(null));
        Assert.False(vm.ReloadMonitorsCommand.CanExecute(null));
        vm.DiscardChanges();
        Assert.False(vm.IsDirty);
        Assert.Equal(30, vm.SelectedMonitor.Configuration.IdleValue);
    }

    [StaFact]
    public void LanguageChange_PreservesDraftAndOriginalEnums()
    {
        var config = Monitor().Configuration;
        config.IsManaged = true;
        config.Behavior = MonitorBehaviorType.Blackout;
        config.IdleValueText = "9";
        config.SelectedTimeUnit = TimeUnit.Hours;
        try
        {
            UiText.Current.SetLanguage("en");
            Assert.Equal("Display 1", config.MonitorTitle);
            Assert.Equal("Hours", config.SelectedTimeUnitText);
            Assert.Equal(9, config.IdleValue);
            Assert.Equal(TimeUnit.Hours, config.ToSettings().IdleUnit);
            Assert.True(config.IsDirty);
            config.IdleValueText = "bad";
            Assert.Contains("whole number", config.IdleValueError);
            Assert.False(config.IsValid);
        }
        finally { UiText.Current.SetLanguage("zh-CN"); }
    }

    [Theory]
    [InlineData(TimeUnit.Seconds, 2147483, 2147484)]
    [InlineData(TimeUnit.Minutes, 35791, 35792)]
    [InlineData(TimeUnit.Hours, 596, 597)]
    public void IdleDelay_DoesNotOverflowStoredMillisecondDuration(TimeUnit unit, int valid, int invalid)
    {
        var config = Monitor().Configuration;
        config.IsManaged = true;
        config.Behavior = MonitorBehaviorType.Blackout;
        config.SelectedTimeUnit = unit;
        config.IdleValue = valid;
        Assert.True(config.IsValid);
        config.IdleValue = invalid;
        Assert.False(config.IsValid);
    }

    [Fact]
    public void UnsupportedBrightness_DisablesAndRejectsDim()
    {
        var config = new MonitorConfigurationViewModel(new MonitorInfo { HardwareId = "NO-DDC" });
        config.IsManaged = true;
        config.Behavior = MonitorBehaviorType.Dim;
        Assert.False(config.IsDimSliderEnabled);
        Assert.False(config.IsValid);
        config.Behavior = MonitorBehaviorType.Blackout;
        Assert.True(config.IsValid);
    }

    [Theory]
    [InlineData("2147483648")]
    [InlineData("1.5")]
    [InlineData("abc")]
    [InlineData("0")]
    public void InvalidInput_CannotSilentlySavePreviousDuration(string text)
    {
        var config = Monitor().Configuration;
        config.IsManaged = true;
        config.Behavior = MonitorBehaviorType.Blackout;
        config.IdleValueText = text;
        Assert.False(config.IsValid);
        Assert.Equal(text, config.IdleValueText);
    }
}
