using Moq;
using OLED_Sleeper.Storage.Interfaces;
using OLED_Sleeper.UI.Localization;
using OLED_Sleeper.UI.Services;

namespace OLED_Sleeper.Tests.UI.Services;

public class UiPreferencesServiceTests
{
    [Theory]
    [InlineData(null, "forest")]
    [InlineData("unknown", "forest")]
    [InlineData("fog", "fog")]
    public void ThemeCompatibilityAndLanguageUpdatesPreserveEachOther(string? theme, string expected)
    {
        var files = new Mock<IAppDataFileStore>();
        var saved = new UiPreferences { Language = "en", Theme = theme! };
        files.Setup(f => f.Read<UiPreferences>(UiPreferencesService.FileName)).Returns(() => saved);
        files.Setup(f => f.TryWrite(UiPreferencesService.FileName, It.IsAny<UiPreferences>()))
            .Callback<string, UiPreferences>((_, value) => saved = value).Returns(true);
        var palette = new OLED_Sleeper.UI.Themes.UiTheme();
        var text = new UiText();
        var service = new UiPreferencesService(files.Object, text, palette);
        service.Load();
        Assert.Equal(expected, palette.Theme);
        Assert.True(service.TrySetLanguage("zh-CN"));
        Assert.Equal(expected, saved.Theme);
        Assert.True(service.TrySetTheme("fog"));
        Assert.Equal("zh-CN", saved.Language);
        var reloadedTheme = new OLED_Sleeper.UI.Themes.UiTheme();
        new UiPreferencesService(files.Object, new UiText(), reloadedTheme).Load();
        Assert.Equal("fog", reloadedTheme.Theme);
    }

    [Fact]
    public void FailedThemeWriteDoesNotApplyOrNotify_AndInvalidThemeIsRejected()
    {
        var files = new Mock<IAppDataFileStore>();
        int applies = 0, notifications = 0;
        var palette = new OLED_Sleeper.UI.Themes.UiTheme(_ => applies++);
        palette.PropertyChanged += (_, _) => notifications++;
        var service = new UiPreferencesService(files.Object, new UiText(), palette);
        Assert.False(service.TrySetTheme("fog"));
        Assert.False(service.TrySetTheme("system"));
        Assert.Equal("forest", palette.Theme);
        Assert.Equal(0, applies);
        Assert.Equal(0, notifications);
        files.Verify(f => f.TryWrite(UiPreferencesService.FileName, It.IsAny<UiPreferences>()), Times.Once);
    }

    [Theory]
    [InlineData(null, "zh-CN")]
    [InlineData("fr", "zh-CN")]
    [InlineData("en", "en")]
    [InlineData("zh-CN", "zh-CN")]
    public void Load_NormalizesMissingAndUnknownLanguage(string? saved, string expected)
    {
        var store = new Mock<IAppDataFileStore>();
        store.Setup(s => s.Read<UiPreferences>(UiPreferencesService.FileName))
            .Returns(saved == null ? null : new UiPreferences { Language = saved });
        var text = new UiText();
        new UiPreferencesService(store.Object, text).Load();
        Assert.Equal(expected, text.Language);
    }

    [Fact]
    public void FailedWrite_KeepsLanguageAndDoesNotNotify()
    {
        var store = new Mock<IAppDataFileStore>();
        var text = new UiText();
        var events = 0;
        text.PropertyChanged += (_, _) => events++;
        Assert.False(new UiPreferencesService(store.Object, text).TrySetLanguage("en"));
        Assert.Equal("zh-CN", text.Language);
        Assert.Equal(0, events);
    }

    [Fact]
    public void Change_WritesOnlyUiPreferenceAndRestoresOnNextLoad()
    {
        UiPreferences? saved = null;
        var store = new Mock<IAppDataFileStore>();
        store.Setup(s => s.TryWrite(UiPreferencesService.FileName, It.IsAny<UiPreferences>()))
            .Callback<string, UiPreferences>((_, value) => saved = value).Returns(true);
        store.Setup(s => s.Read<UiPreferences>(UiPreferencesService.FileName)).Returns(() => saved);
        var text = new UiText();
        Assert.True(new UiPreferencesService(store.Object, text).TrySetLanguage("en"));
        Assert.Equal("Display management", text["显示器管理"]);
        var restarted = new UiText();
        new UiPreferencesService(store.Object, restarted).Load();
        Assert.Equal("en", restarted.Language);
        store.Verify(s => s.TryWrite(UiPreferencesService.FileName, It.IsAny<UiPreferences>()), Times.Once);
    }

    [Fact]
    public void UnsupportedLanguage_IsNotWritten()
    {
        var store = new Mock<IAppDataFileStore>();
        Assert.False(new UiPreferencesService(store.Object, new UiText()).TrySetLanguage("de"));
        store.VerifyNoOtherCalls();
    }
}
