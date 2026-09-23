using OLED_Sleeper.Features.ColorTemperature;
using OLED_Sleeper.Features.MonitorInformation.Models;
using OLED_Sleeper.UI.Helpers;
using OLED_Sleeper.UI.Localization;
using OLED_Sleeper.UI.ViewModels;

namespace OLED_Sleeper.Tests.UI.Helpers;

[Collection("UI language")]
public sealed class MonitorTitleFormatterTests
{
    [WpfTheory]
    [InlineData("zh-CN", "显示器 2", "（主屏）")]
    [InlineData("en", "Display 2", " (Primary)")]
    public void ModelNamesAndFallbackMatchAcrossModules(string language, string fallback, string primary)
    {
        string original = UiText.Current.Language;
        try
        {
            UiText.Current.SetLanguage(language);
            var monitor = new MonitorInfo { HardwareId = "panel-A", ModelName = " Q27P10 ", DisplayNumber = 2 };
            var scope = new ColorDisplayScope("panel-A", "path-A", @"\\.\DISPLAY2", " Q27P10 ", "topology", "mode");
            var capabilities = new ColorCapability[] { new(scope, ColorCapabilityState.Supported, "") };
            var config = new MonitorConfigurationViewModel(monitor);
            Assert.Equal("Q27P10", config.MonitorTitle);
            Assert.Equal(config.MonitorTitle, MonitorTitleFormatter.Format(scope, capabilities));
            monitor.IsPrimary = true;
            Assert.Equal("Q27P10" + primary, config.MonitorTitle);
            monitor.IsPrimary = false;
            monitor.ModelName = " ";
            scope = scope with { DisplayName = " " };
            Assert.Equal(fallback, config.MonitorTitle);
            Assert.Equal(fallback, MonitorTitleFormatter.Format(scope, [capabilities[0] with { Scope = scope }]));
            Assert.Equal("panel-A", config.ToSettings().HardwareId);
        }
        finally { UiText.Current.SetLanguage(original); }
    }

    [WpfFact]
    public void RepeatedModelsKeepSystemNumbersWhenOrderOrQualificationChanges()
    {
        var first = new ColorDisplayScope("A", "path-A", @"\\.\DISPLAY1", "DELL SE2723DS", "topology", "mode");
        var second = new ColorDisplayScope("B", "path-B", @"\\.\DISPLAY3", " dell se2723ds ", "topology", "mode");
        ColorCapability[] capabilities = [new(second, ColorCapabilityState.Unverified, ""), new(first, ColorCapabilityState.Supported, "")];
        var monitor = new MonitorInfo { ModelName = first.DisplayName, DisplayNumber = 1, HasSharedModelName = true };
        Assert.Equal("DELL SE2723DS · 1", MonitorTitleFormatter.Format(monitor));
        Assert.Equal(MonitorTitleFormatter.Format(monitor), MonitorTitleFormatter.Format(first, capabilities));
        Assert.Equal("dell se2723ds · 3", MonitorTitleFormatter.Format(second, capabilities));
        Assert.Equal("DELL SE2723DS · 1", MonitorTitleFormatter.Format(first, capabilities.Reverse().ToArray()));
        Assert.Equal("DELL SE2723DS", MonitorTitleFormatter.Format(first, [capabilities[1]]));
        monitor.TopologyLabel = "1|2";
        monitor.HasSharedModelName = false;
        Assert.Equal("DELL SE2723DS · 1|2", MonitorTitleFormatter.Format(monitor));
    }
}
