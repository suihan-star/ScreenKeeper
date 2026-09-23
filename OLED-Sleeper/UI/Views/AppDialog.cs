using OLED_Sleeper.UI.Localization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace OLED_Sleeper.UI.Views;

/// <summary>App-owned dialog buttons follow the selected language instead of the Windows language.</summary>
public sealed class AppDialog : Window
{
    public MessageBoxResult Result { get; private set; } = MessageBoxResult.Cancel;

    public AppDialog(string message, string caption, bool confirm)
    {
        Title = caption;
        Width = 460;
        SizeToContent = SizeToContent.Height;
        ResizeMode = ResizeMode.NoResize;
        ShowInTaskbar = false;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        FontFamily = new FontFamily("Segoe UI, Microsoft YaHei UI");
        SetResourceReference(BackgroundProperty, "SecondaryBackgroundColor");
        SetResourceReference(ForegroundProperty, "PrimaryForegroundColor");
        var panel = new StackPanel { Margin = new Thickness(26) };
        panel.Children.Add(new TextBlock { Text = caption, FontSize = 20, Margin = new Thickness(0, 0, 0, 14) });
        panel.Children.Add(new ScrollViewer
        {
            MaxHeight = 320,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            Content = new TextBlock { Text = message, TextWrapping = TextWrapping.Wrap, FontSize = 14, Margin = new Thickness(0, 0, 0, 24) }
        });
        var actions = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
        if (confirm)
        {
            AddButton(actions, "取消", MessageBoxResult.Cancel, false, true);
            AddButton(actions, "不保存", MessageBoxResult.No);
            AddButton(actions, "保存", MessageBoxResult.Yes, true);
        }
        else AddButton(actions, "确定", MessageBoxResult.OK, true, true);
        panel.Children.Add(actions);
        Content = panel;
    }

    private void AddButton(Panel panel, string key, MessageBoxResult result, bool primary = false, bool cancel = false)
    {
        var button = new Button
        {
            Content = UiText.Get(key),
            Margin = new Thickness(8, 0, 0, 0),
            MinWidth = 72,
            IsDefault = primary,
            IsCancel = cancel,
            Style = (Style)FindResource(primary ? "PrimaryButton" : typeof(Button))
        };
        button.Click += (_, _) => { Result = result; Close(); };
        panel.Children.Add(button);
        if (cancel) Loaded += (_, _) => button.Focus();
    }
}
