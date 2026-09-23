using System.ComponentModel;
using System.Text.Json;
using System.Windows.Data;
using System.Windows.Markup;

namespace OLED_Sleeper.UI.Localization;

/// <summary>Application-scoped display language. Does not change thread culture or stored enum values.</summary>
public sealed class UiText : INotifyPropertyChanged
{
    private static readonly Dictionary<string, string> English = LoadEnglish();
    public static UiText Current { get; } = new();
    public string Language { get; private set; } = "zh-CN";
    public event PropertyChangedEventHandler? PropertyChanged;
    public string this[string key] => Language == "en" && English.TryGetValue(key, out var text) ? text : key;
    public static string Get(string key) => Current[key];

    public void SetLanguage(string? language)
    {
        var normalized = language == "en" ? "en" : "zh-CN";
        if (Language == normalized) return;
        Language = normalized;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(string.Empty));
    }

    private static Dictionary<string, string> LoadEnglish()
    {
        using var stream = typeof(UiText).Assembly.GetManifestResourceStream("ScreenKeeper.English.json")!;
        return JsonSerializer.Deserialize<Dictionary<string, string>>(stream)!;
    }
}

/// <summary>A live binding, so existing controls refresh without rebuilding their view models.</summary>
[MarkupExtensionReturnType(typeof(string))]
public sealed class TextExtension : MarkupExtension
{
    public string Key { get; set; } = string.Empty;
    public override object ProvideValue(IServiceProvider serviceProvider) =>
        new Binding($"[{Key}]") { Source = UiText.Current, Mode = BindingMode.OneWay }.ProvideValue(serviceProvider);
}
