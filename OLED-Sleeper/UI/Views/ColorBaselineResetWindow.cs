using OLED_Sleeper.Features.ColorTemperature;
using OLED_Sleeper.UI.Localization;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

namespace OLED_Sleeper.UI.Views;

/// <summary>Only the user's explicit observation can retire a baseline captured under another color tool.</summary>
public sealed class ColorBaselineResetWindow : Window
{
    private readonly ColorTemperatureService _service;
    private readonly IColorValidationRegistry _registry;
    private readonly TextBlock _status;
    private readonly StackPanel _screens;
    private readonly CheckBox _ready;
    private readonly Button _confirm, _retry, _close;
    private readonly CancellationTokenSource _cancel = new();
    private Guid _ticket;
    private bool _busy, _completed, _closeRequested;

    public ColorBaselineResetWindow(ColorTemperatureService service, IColorValidationRegistry registry)
    {
        _service = service; _registry = registry;
        SetBinding(TitleProperty, Text("重新建立原色"));
        Width = 570; SizeToContent = SizeToContent.Height; MaxHeight = Math.Max(400, SystemParameters.WorkArea.Height - 40);
        WindowStartupLocation = WindowStartupLocation.CenterOwner; ResizeMode = ResizeMode.NoResize; ShowInTaskbar = false;
        SetResourceReference(BackgroundProperty, "SecondaryBackgroundColor");
        SetResourceReference(ForegroundProperty, "PrimaryForegroundColor");
        FontFamily = new FontFamily("Segoe UI, Microsoft YaHei UI");
        var body = new StackPanel { Margin = new Thickness(26) };
        var heading = Label("重新建立原色"); heading.FontSize = 22; body.Children.Add(heading);
        body.Children.Add(Label("重新连接屏幕或退出其他调色工具后，旧原色记录可能已不适用。"));
        body.Children.Add(Label("此操作会备份以下屏幕的旧恢复记录，并要求重新验证。不会改变当前屏幕颜色，也不会自动启用调色。"));
        _status = Label("正在只读检查当前颜色，请稍候…"); _status.FontWeight = FontWeights.SemiBold; body.Children.Add(_status);
        _screens = new StackPanel { Margin = new Thickness(0, 0, 0, 12) }; body.Children.Add(_screens);
        _ready = new CheckBox { Content = Label("我已关闭其他调色功能，确认上面列出的所有屏幕当前画面正常，并同意以当前颜色重新开始验证。"), Margin = new Thickness(0, 8, 0, 12), IsEnabled = false };
        body.Children.Add(_ready);
        var actions = new WrapPanel();
        _confirm = Button("备份旧记录并重新建立"); _retry = Button("重新检查"); _close = Button("取消");
        if (TryFindResource("PrimaryButton") is Style primary) _confirm.Style = primary;
        actions.Children.Add(_confirm); actions.Children.Add(_retry); actions.Children.Add(_close); body.Children.Add(actions);
        Content = new ScrollViewer { Content = body, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
        _ready.Checked += (_, _) => UpdateButtons(); _ready.Unchecked += (_, _) => UpdateButtons();
        _retry.Click += async (_, _) => await CheckAsync();
        _confirm.Click += async (_, _) => await ConfirmAsync();
        _close.Click += (_, _) => Close();
        Loaded += async (_, _) => await CheckAsync();
        Closed += (_, _) => { _service.CancelBaselineReset(); _cancel.Dispose(); };
        UpdateButtons();
    }

    private async Task CheckAsync()
    {
        if (_busy || _completed) return;
        _busy = true; _ticket = Guid.Empty; _ready.IsChecked = false; _screens.Children.Clear(); UpdateButtons();
        _status.Text = UiText.Get("正在只读检查当前颜色，请稍候…");
        try
        {
            var result = !_registry.IsAvailable ? new ColorBaselineResetCheck(false, "色温验证记录不可读取，请保留原文件并重试。") :
                await _service.PrepareBaselineResetAsync(_cancel.Token);
            _status.Text = UiText.Get(result.Message);
            _status.SetResourceReference(ForegroundProperty, result.Ready ? "PrimaryForegroundColor" : "DestructiveColorBrush");
            if (result.Ready)
            {
                _ticket = result.Ticket;
                foreach (var name in result.DisplayNames ?? []) _screens.Children.Add(new TextBlock { Text = "• " + name, FontSize = 15, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 4, 0, 4) });
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception) { _status.Text = UiText.Get("当前颜色检查失败，原记录未改动。"); }
        finally { _busy = false; UpdateButtons(); if (_closeRequested) Close(); }
    }

    private async Task ConfirmAsync()
    {
        if (_busy || _ticket == Guid.Empty || _ready.IsChecked != true) return;
        _busy = true; UpdateButtons();
        try
        {
            var result = await _service.ConfirmBaselineResetAsync(_ticket, _registry, _cancel.Token);
            _ticket = Guid.Empty; _completed = result.Ready;
            _status.Text = UiText.Get(result.Message);
            if (_completed) _close.SetBinding(ContentControl.ContentProperty, Text("返回设置并验证"));
        }
        catch (OperationCanceledException) { _ticket = Guid.Empty; }
        catch (Exception) { _ticket = Guid.Empty; _status.Text = UiText.Get("重新建立原色未完成，请保留记录并重试。"); }
        finally { _busy = false; UpdateButtons(); if (_closeRequested) Close(); }
    }

    private void UpdateButtons()
    {
        _confirm.IsEnabled = !_busy && !_completed && _ticket != Guid.Empty && _ready.IsChecked == true && _registry.IsAvailable;
        _ready.IsEnabled = !_busy && !_completed && _ticket != Guid.Empty;
        _retry.IsEnabled = !_busy && !_completed;
        _retry.Visibility = _completed ? Visibility.Collapsed : Visibility.Visible;
        _confirm.Visibility = _ready.Visibility = !_completed && _ticket != Guid.Empty ? Visibility.Visible : Visibility.Collapsed;
    }
    protected override void OnClosing(CancelEventArgs e)
    {
        if (_busy) { _closeRequested = true; _cancel.Cancel(); _service.CancelBaselineReset(); e.Cancel = true; }
        base.OnClosing(e);
    }
    private static Binding Text(string key) => new($"[{key}]") { Source = UiText.Current };
    private static TextBlock Label(string key)
    {
        var label = new TextBlock { TextWrapping = TextWrapping.Wrap, FontSize = 14, Margin = new Thickness(0, 0, 0, 12) };
        label.SetBinding(TextBlock.TextProperty, Text(key)); return label;
    }
    private static Button Button(string key)
    {
        var button = new Button { Margin = new Thickness(0, 0, 8, 8), Padding = new Thickness(12, 9, 12, 9) };
        button.SetBinding(ContentControl.ContentProperty, Text(key)); return button;
    }
}
