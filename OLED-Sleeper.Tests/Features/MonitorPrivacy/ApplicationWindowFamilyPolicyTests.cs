using OLED_Sleeper.Features.MonitorPrivacy;
using System.Windows;

namespace OLED_Sleeper.Tests.Features.MonitorPrivacy;

public class ApplicationWindowFamilyPolicyTests
{
    internal static readonly ApplicationWindow Main = new("main", 10, @"C:\Apps\WXWork.exe", "WeWorkWindow", "", new Rect(0, 0, 100, 100), false, 0, 42);
    internal static readonly ApplicationWindow Title = new("title", 11, Main.Path, "TitleBarWindow", "", new Rect(0, 0, 100, 10), false, Main.Handle, 42, 0x8080080);
    internal static readonly ApplicationWindow Shadow = Title with { Token = "shadow", Handle = 12, ClassName = "PerryShadowWnd", Bounds = new Rect(-5, -5, 110, 110) };

    [Fact]
    public void OnlyRecognizedSameProcessChromeIsACompanion()
    {
        Assert.True(ApplicationWindowFamilyPolicy.IsCompanion(Main, Title));
        Assert.True(ApplicationWindowFamilyPolicy.IsCompanion(Main, Shadow));
    }

    [Theory]
    [InlineData("process")]
    [InlineData("unknown-process")]
    [InlineData("owner")]
    [InlineData("path")]
    [InlineData("class")]
    [InlineData("layered")]
    [InlineData("tool")]
    [InlineData("activation")]
    [InlineData("minimized")]
    [InlineData("outside")]
    [InlineData("body")]
    [InlineData("narrow")]
    public void SameClassNameAloneNeverAdmitsAWindow(string mismatch)
    {
        var window = mismatch switch
        {
            "process" => Title with { ProcessId = 43 },
            "unknown-process" => Title with { ProcessId = 0 },
            "owner" => Title with { Owner = 99 },
            "path" => Title with { Path = @"C:\Other\WXWork.exe" },
            "class" => Title with { ClassName = "Dialog" },
            "layered" => Title with { ExtendedStyle = Title.ExtendedStyle & ~0x80000 },
            "tool" => Title with { ExtendedStyle = Title.ExtendedStyle & ~0x80 },
            "activation" => Title with { ExtendedStyle = Title.ExtendedStyle & ~0x8000000 },
            "minimized" => Title with { Minimized = true },
            "outside" => Title with { Bounds = new Rect(100, 0, 100, 10) },
            "body" => Title with { Bounds = new Rect(0, 0, 100, 70) },
            _ => Title with { Bounds = new Rect(0, 0, 20, 10) }
        };
        Assert.False(ApplicationWindowFamilyPolicy.IsCompanion(Main, window));
    }

    [Fact]
    public void ShadowGeometryUsesDpiButRejectsUnboundedOrMissingFrames()
    {
        var shadow = Shadow with { Bounds = new Rect(-40, -40, 180, 180) };
        Assert.False(ApplicationWindowFamilyPolicy.IsCompanion(Main, shadow));
        Assert.True(ApplicationWindowFamilyPolicy.IsCompanion(Main with { Dpi = 192 }, shadow));
        Assert.False(ApplicationWindowFamilyPolicy.IsCompanion(Main with { Dpi = 0 }, Shadow));
        Assert.False(ApplicationWindowFamilyPolicy.IsCompanion(Main, Shadow with { Bounds = new Rect(-40, -40, 90, 90) }));
        Assert.False(ApplicationWindowFamilyPolicy.IsCompanion(Main with { Minimized = true }, Shadow));
        Assert.False(ApplicationWindowFamilyPolicy.IsCompanion(Main with { ClassName = "Other" }, Shadow));
        Assert.False(ApplicationWindowFamilyPolicy.IsCompanion(Main with { Path = @"C:\Apps\Other.exe" }, Shadow with { Path = @"C:\Apps\Other.exe" }));
    }
}
