using OLED_Sleeper.Features.ColorTemperature;
using OLED_Sleeper.UI.Localization;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

namespace OLED_Sleeper.UI.Views;

/// <summary>The user observes the physical effect. API readback alone cannot click the confirmation.</summary>
public sealed class ColorValidationWindow : Window
{
    private readonly ColorTemperatureService _service;
    private readonly IColorValidationRegistry _registry;
    private readonly string _hardwareId;
    private readonly TextBlock _status;
    private readonly TextBlock _prerequisites;
    private readonly Button _start, _confirm, _close;
    private readonly CheckBox _ready;
    private readonly CheckBox[] _observations;
    private readonly StackPanel _checks;
    private readonly StackPanel _recovery;
    private readonly TextBlock _recoveryScreens;
    private readonly CancellationTokenSource _cancel = new();
    private bool _running, _closeRequested, _confirmed, _attempted;
    private Guid _ticket;

    public ColorValidationWindow(ColorTemperatureService service, IColorValidationRegistry registry, string hardwareId, string name)
    {
        _service = service; _registry = registry; _hardwareId = hardwareId;
        SetBinding(TitleProperty, Text("验证此屏幕"));
        Width = 540; SizeToContent = SizeToContent.Height; MaxHeight = Math.Max(400, SystemParameters.WorkArea.Height - 40);
        WindowStartupLocation = WindowStartupLocation.CenterOwner; ResizeMode = ResizeMode.NoResize; ShowInTaskbar = false;
        SetResourceReference(BackgroundProperty, "SecondaryBackgroundColor");
        SetResourceReference(ForegroundProperty, "PrimaryForegroundColor");
        FontFamily = new FontFamily("Segoe UI, Microsoft YaHei UI");
        var body = new StackPanel { Margin = new Thickness(26) };
        body.Children.Add(new TextBlock { Text = name, FontSize = 22, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 0, 0, 16) });
        body.Children.Add(Label("仅将此屏调暖至约 5000K，15 秒后自动恢复。请同时观察其他屏幕是否保持原样。"));
        body.Children.Add(Label("开始前请关闭 Windows 夜间模式和其他调色工具；使用自定义校色配置时暂不验证。验证通过后不会自动启用调色。"));
        _prerequisites = Label("");
        _prerequisites.FontWeight = FontWeights.SemiBold;
        body.Children.Add(_prerequisites);
        _recovery = new StackPanel { Margin = new Thickness(0, 0, 0, 12) };
        _recoveryScreens = Label("");
        _recovery.Children.Add(_recoveryScreens);
        var resolve = Button("处理恢复记录");
        resolve.HorizontalAlignment = HorizontalAlignment.Left;
        resolve.Click += (_, _) =>
        {
            new ColorBaselineResetWindow(_service, _registry) { Owner = this }.ShowDialog();
            UpdateButtons();
        };
        _recovery.Children.Add(resolve);
        body.Children.Add(_recovery);
        _ready = Check("我已关闭其他调色工具，当前未使用自定义校色，并准备观察屏幕。");
        body.Children.Add(_ready);
        _status = Label("点击开始后请留意屏幕，测试结束前可随时取消。");
        _status.Margin = new Thickness(0, 18, 0, 18);
        body.Children.Add(_status);
        _checks = new StackPanel { Visibility = Visibility.Collapsed };
        _observations = [Check("目标屏幕确实变暖"), Check("其他屏幕保持原样（单屏可直接勾选）"), Check("测试结束后颜色已恢复正常")];
        foreach (var check in _observations)
        {
            _checks.Children.Add(check);
            check.Checked += (_, _) => UpdateButtons(); check.Unchecked += (_, _) => UpdateButtons();
        }
        body.Children.Add(_checks);
        var actions = new WrapPanel { Margin = new Thickness(0, 18, 0, 0) };
        _start = Button("开始 15 秒验证"); _confirm = Button("确认效果并完成验证"); _close = Button("取消");
        _confirm.Visibility = Visibility.Collapsed;
        if (TryFindResource("PrimaryButton") is Style primary) { _start.Style = primary; _confirm.Style = primary; }
        actions.Children.Add(_start); actions.Children.Add(_confirm); actions.Children.Add(_close);
        body.Children.Add(actions);
        Content = new ScrollViewer { Content = body, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
        _ready.Checked += (_, _) => UpdateButtons(); _ready.Unchecked += (_, _) => UpdateButtons();
        _start.Click += async (_, _) => await RunAsync();
        _confirm.Click += async (_, _) => await ConfirmAsync();
        _close.Click += (_, _) => Close();
        _service.Changed += ServiceChanged;
        Closed += (_, _) => { _service.Changed -= ServiceChanged; if (!_confirmed) _service.CancelValidation(); _cancel.Dispose(); };
        UpdateButtons();
    }

    private async Task RunAsync()
    {
        _running = true; _attempted = true; _ticket = Guid.Empty; _checks.Visibility = _confirm.Visibility = Visibility.Collapsed;
        foreach (var check in _observations) check.IsChecked = false;
        UpdateButtons();
        try
        {
            var progress = new Progress<int>(seconds => _status.Text = UiText.Get("正在调暖，自动恢复倒计时：") + $" {seconds} s");
            var result = await _service.RunValidationAsync(_hardwareId, progress, _cancel.Token);
            _status.Text = UiText.Get(result.Message);
            if (result.CanConfirm)
            {
                _ticket = result.Ticket;
                _checks.Visibility = _confirm.Visibility = Visibility.Visible;
                _start.Visibility = Visibility.Collapsed;
            }
        }
        catch (OperationCanceledException) { _status.Text = UiText.Get("验证已取消。"); }
        catch (Exception) { _status.Text = UiText.Get("验证失败，请检查显示连接与其他调色工具。"); }
        finally { _running = false; UpdateButtons(); if (_closeRequested) Close(); }
    }

    private async Task ConfirmAsync()
    {
        if (_ticket == Guid.Empty || !_observations.All(c => c.IsChecked == true)) return;
        _running = true; UpdateButtons();
        try
        {
            var result = await _service.ConfirmValidationAsync(_ticket, _registry, _cancel.Token);
            _status.Text = UiText.Get(result.Message);
            _ticket = Guid.Empty;
            _confirm.Visibility = _checks.Visibility = Visibility.Collapsed;
            _confirmed = result.CanConfirm;
            if (!_confirmed) _start.Visibility = Visibility.Visible;
            else { _ready.IsEnabled = false; _close.SetBinding(ContentControl.ContentProperty, Text("完成")); }
        }
        catch (Exception) { _status.Text = UiText.Get("验证结果保存失败，未启用自动调色。"); }
        finally { _running = false; UpdateButtons(); if (_closeRequested) Close(); }
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        if (_running)
        {
            _closeRequested = true; _cancel.Cancel(); _service.CancelValidation(); e.Cancel = true;
            _status.Text = UiText.Get("正在恢复原色，请稍候…");
        }
        base.OnClosing(e);
    }
    private void UpdateButtons()
    {
        string? blocked = !_registry.IsAvailable ? "色温验证记录不可读取，请保留原文件并重试。" : _service.GetValidationBlockReason(_hardwareId);
        _prerequisites.Text = UiText.Get(blocked ?? (_ready.IsChecked != true ? "请先勾选下方准备观察的确认项，再开始 15 秒验证。" : "准备就绪，可以开始 15 秒验证。"));
        _prerequisites.Visibility = _running || _confirmed || _ticket != Guid.Empty ? Visibility.Collapsed : Visibility.Visible;
        _prerequisites.Foreground = TryFindResource(blocked == null ? "SecondaryForegroundColor" : "DestructiveColorBrush") as Brush ?? Foreground;
        _recovery.Visibility = !_running && !_confirmed && _ticket == Guid.Empty && _service.HasRecoveryRecords && !_service.Settings.AnyEnabled
            ? Visibility.Visible : Visibility.Collapsed;
        _recoveryScreens.Text = UiText.Get("待处理屏幕：") + " " + string.Join("、", _service.RecoveryScopes.Select(s => UI.Helpers.MonitorTitleFormatter.Format(s, _service.Capabilities)));
        _status.Visibility = _attempted ? Visibility.Visible : Visibility.Collapsed;
        _start.IsEnabled = !_running && !_confirmed && _ready.IsChecked == true && blocked == null;
        _start.ToolTip = blocked == null ? null : UiText.Get(blocked);
        ToolTipService.SetShowOnDisabled(_start, true);
        _confirm.IsEnabled = !_running && _ticket != Guid.Empty && _observations.All(c => c.IsChecked == true);
        _ready.IsEnabled = !_running && !_confirmed;
    }
    private void ServiceChanged()
    {
        if (!Dispatcher.HasShutdownStarted) Dispatcher.BeginInvoke(new Action(UpdateButtons));
    }
    private static Binding Text(string key) => new($"[{key}]") { Source = UiText.Current };
    private static TextBlock Label(string key)
    {
        var label = new TextBlock { TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 0, 0, 12), FontSize = 14 };
        label.SetBinding(TextBlock.TextProperty, Text(key)); return label;
    }
    private static CheckBox Check(string key) => new() { Content = Label(key), Margin = new Thickness(0, 8, 0, 0) };
    private static Button Button(string key)
    {
        var button = new Button { Margin = new Thickness(0, 0, 8, 8), Padding = new Thickness(12, 9, 12, 9) };
        button.SetBinding(ContentControl.ContentProperty, Text(key)); return button;
    }
}
