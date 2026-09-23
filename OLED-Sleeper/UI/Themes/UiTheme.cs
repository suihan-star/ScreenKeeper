using System.ComponentModel;
using System.Windows;

namespace OLED_Sleeper.UI.Themes;

/// <summary>Changes settings resources only. Protection and rest scenes keep a local palette.</summary>
public sealed class UiTheme(Action<string>? apply = null) : INotifyPropertyChanged
{
    public static UiTheme Current { get; } = new(ApplyResources);
    public string Theme { get; private set; } = "forest";
    public event PropertyChangedEventHandler? PropertyChanged;

    public void SetTheme(string? theme)
    {
        var normalized = theme == "fog" ? "fog" : "forest";
        if (Theme == normalized) return;
        apply?.Invoke(normalized);
        Theme = normalized;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Theme)));
    }

    public static ResourceDictionary Palette(string theme = "forest") => new()
    {
        Source = new Uri($"/ScreenKeeper;component/UI/Themes/{(theme == "fog" ? "Fog" : "Forest")}.xaml", UriKind.Relative)
    };

    private static void ApplyResources(string theme)
    {
        if (Application.Current is not { } application) return;
        application.Dispatcher.VerifyAccess();
        var palette = Palette(theme);
        foreach (var key in palette.Keys) application.Resources[key] = palette[key];
    }
}
