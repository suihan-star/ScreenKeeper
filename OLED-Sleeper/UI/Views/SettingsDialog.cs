using OLED_Sleeper.UI.Localization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

namespace OLED_Sleeper.UI.Views;

/// <summary>Consistent bounded dialogs: scroll content while keeping actions reachable.</summary>
public sealed class SettingsDialog : Window
{
    public StackPanel Body { get; } = new();
    public StackPanel Actions { get; } = new() { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 16, 0, 0) };
    public SettingsDialog(Window owner, string title)
    {
        Owner = owner; Bind(this, TitleProperty, title); ShowInTaskbar = false;
        Width = Math.Min(560, Math.Max(400, owner.ActualWidth - 40)); Height = Math.Min(600, Math.Max(360, owner.ActualHeight - 50));
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        SetResourceReference(BackgroundProperty, "SecondaryBackgroundColor"); SetResourceReference(ForegroundProperty, "PrimaryForegroundColor");
        FontFamily = owner.FontFamily; FontSize = 14;
        var layout = new DockPanel { Margin = new Thickness(24) };
        var heading = Text(title); heading.FontSize = 22; heading.Margin = new Thickness(0, 0, 0, 18);
        DockPanel.SetDock(heading, Dock.Top); layout.Children.Add(heading);
        DockPanel.SetDock(Actions, Dock.Bottom); layout.Children.Add(Actions);
        layout.Children.Add(new ScrollViewer { Content = Body, VerticalScrollBarVisibility = ScrollBarVisibility.Auto, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled, Padding = new Thickness(0, 0, 10, 0) });
        Content = layout;
    }
    public Button Action(string key, Action action, bool primary = false)
    {
        var button = Button(key, action); button.Margin = new Thickness(8, 0, 0, 0);
        if (primary) button.Style = (Style)FindResource("PrimaryButton");
        Actions.Children.Add(button); return button;
    }
    public static TextBlock Text(string key)
    {
        var text = new TextBlock { TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 0, 0, 10) };
        Bind(text, TextBlock.TextProperty, key); return text;
    }
    public static Button Button(string key, Action action)
    {
        var button = new Button { HorizontalAlignment = HorizontalAlignment.Left };
        Bind(button, ContentControl.ContentProperty, key); button.Click += (_, _) => action(); return button;
    }
    public static void Bind(DependencyObject target, DependencyProperty property, string key) => BindingOperations.SetBinding(target, property, new Binding("[" + key + "]") { Source = UiText.Current });
}
