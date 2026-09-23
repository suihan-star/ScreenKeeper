using OLED_Sleeper.Storage.Interfaces;
using OLED_Sleeper.UI.Localization;
using OLED_Sleeper.UI.Themes;

namespace OLED_Sleeper.UI.Services;

public sealed class UiPreferences
{
    public string Language { get; set; } = "zh-CN";
    public string Theme { get; set; } = "forest";
}

/// <summary>UI preferences use the product's storage root, independently of monitor behavior settings.</summary>
public sealed class UiPreferencesService(IAppDataFileStore store, UiText text, UiTheme? theme = null)
{
    public const string FileName = "ui-preferences.json";
    private readonly UiTheme _theme = theme ?? UiTheme.Current;

    public void Load()
    {
        var saved = store.Read<UiPreferences>(FileName);
        text.SetLanguage(saved?.Language);
        _theme.SetTheme(saved?.Theme);
    }

    public bool TrySetLanguage(string language)
    {
        if (language is not ("en" or "zh-CN")) return false;
        if (language == text.Language) return true;
        // Persist first: a failure must not imply a preference was saved for next launch.
        if (!store.TryWrite(FileName, new UiPreferences { Language = language, Theme = _theme.Theme })) return false;
        text.SetLanguage(language);
        return true;
    }

    public bool TrySetTheme(string theme)
    {
        if (theme is not ("forest" or "fog")) return false;
        if (theme == _theme.Theme) return true;
        if (!store.TryWrite(FileName, new UiPreferences { Language = text.Language, Theme = theme })) return false;
        _theme.SetTheme(theme);
        return true;
    }
}
