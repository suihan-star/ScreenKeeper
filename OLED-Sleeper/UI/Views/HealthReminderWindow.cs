using OLED_Sleeper.Features.HealthReminder;
using OLED_Sleeper.UI.Localization;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace OLED_Sleeper.UI.Views;

public sealed class HealthReminderPresenter(Func<string, ImageSource?>? loadImage = null) : IHealthReminderPresenter
{
    private HealthReminderWindow? _window;
    private HealthSceneWindow? _scene;
    private HealthAppearance _appearance = new();
    public bool IsVisible => _window?.IsVisible == true;
    public string? ScreenDeviceName => _window?.ScreenDeviceName;
    public event Action<HealthCardAction>? ActionRequested;
    public void Configure(HealthAppearance appearance) => _appearance = appearance.Copy();

    public void Show(HealthCardKind kind, HealthScreen screen, TimeSpan remaining)
    {
        Hide();
        bool resting = kind is HealthCardKind.Countdown or HealthCardKind.PreviewCountdown;
        if (resting || _appearance.ReminderStyle != HealthReminderStyle.Compact)
        {
            ImageSource? picture = null;
            if (_appearance.Image is { } asset && loadImage != null)
                try { picture = loadImage(asset); } catch { /* Missing images use the built-in scene. */ }
            _scene = new HealthSceneWindow(screen, _appearance, resting, picture);
            _scene.PointerMoved += () => _window?.RevealControls();
            _scene.Show();
        }
        _window = new HealthReminderWindow(kind, screen, remaining, _appearance, _scene != null);
        _window.ActionRequested += action => ActionRequested?.Invoke(action);
        _window.Show();
        // Only a user-started break takes focus; typing must not go into an application hidden by the opaque scene.
        if (resting) { _window.Activate(); _window.Focus(); }
    }
    public void Update(TimeSpan remaining)
    {
        _window?.Update(remaining);
        if (remaining > TimeSpan.Zero && remaining <= TimeSpan.FromSeconds(1)) _scene?.FadeAway();
    }
    public void Hide()
    {
        var window = _window;
        _window = null;
        window?.CloseFromService();
        _scene?.Close();
        _scene = null;
    }
}

/// <summary>The action surface stays separate from the click-through scene. Escape remains local.</summary>
public sealed class HealthReminderWindow : Window
{
    private readonly HealthCardKind _kind;
    private readonly HealthScreen _screen;
    private readonly TextBlock _countdown;
    private bool _serviceClosing;
    private readonly bool _resting, _immersive;
    private readonly HealthAppearance _appearance;
    private readonly System.Windows.Threading.DispatcherTimer _controlsTimer;
    private DateTime _lastInteraction = DateTime.UtcNow;
    public string ScreenDeviceName => _screen.DeviceName;
    public event Action<HealthCardAction>? ActionRequested;

    public HealthReminderWindow(HealthCardKind kind, HealthScreen screen, TimeSpan remaining, HealthAppearance? appearance = null, bool immersive = false)
    {
        Resources.MergedDictionaries.Add(Themes.UiTheme.Palette());
        Resources.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri("/ScreenKeeper;component/UI/Themes/HealthReminder.xaml", UriKind.Relative) });
        _kind = kind;
        _screen = screen;
        _appearance = appearance?.Copy() ?? new();
        _resting = kind is HealthCardKind.Countdown or HealthCardKind.PreviewCountdown;
        _immersive = immersive;
        Width = 430;
        SizeToContent = SizeToContent.Height;
        WindowStyle = WindowStyle.None;
        ResizeMode = ResizeMode.NoResize;
        ShowInTaskbar = false;
        ShowActivated = false;
        Topmost = true;
        AllowsTransparency = true;
        Background = Brushes.Transparent;
        FontFamily = new FontFamily("Segoe UI, Microsoft YaHei UI");
        Foreground = Brush("PrimaryForegroundColor", "#E8EFE5");
        SetBinding(TitleProperty, TextBinding("健康提醒"));
        var body = new StackPanel { Margin = new Thickness(28, 24, 28, 26) };
        var heading = new Grid();
        // Equal side gutters center the title on the card, independently of the close button.
        heading.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(40) });
        heading.ColumnDefinitions.Add(new ColumnDefinition());
        heading.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(40) });
        var label = Label(kind is HealthCardKind.Preview or HealthCardKind.PreviewCountdown ? "健康提醒预览" : _resting ? "休息倒计时" : "该休息一下了");
        label.FontSize = 21;
        label.VerticalAlignment = VerticalAlignment.Center;
        label.TextAlignment = TextAlignment.Center;
        label.TextWrapping = TextWrapping.Wrap;
        Grid.SetColumn(label, 1);
        heading.Children.Add(label);
        var close = new Button { Content = "×", MinWidth = 28, MinHeight = 28, Padding = new Thickness(4), Margin = new Thickness(12, 0, 0, 0), Background = Brushes.Transparent, BorderThickness = new Thickness(0) };
        close.SetBinding(ToolTipProperty, TextBinding(kind == HealthCardKind.Countdown ? "收起到托盘" : "关闭"));
        System.Windows.Automation.AutomationProperties.SetName(close, UiText.Get(kind == HealthCardKind.Countdown ? "收起到托盘" : "关闭"));
        close.Click += (_, _) => Request(kind == HealthCardKind.Countdown ? HealthCardAction.Collapse : HealthCardAction.Close);
        Grid.SetColumn(close, 2);
        heading.Children.Add(close);
        body.Children.Add(heading);
        var note = Label(kind is HealthCardKind.Preview or HealthCardKind.PreviewCountdown ? "这只是预览，不会改变工作周期。" :
            _resting ? "按自己的节奏休息，计时结束不会唤亮屏幕。" : "专注了这么久，辛苦啦。眨眨眼，伸个懒腰，让紧绷的自己放松片刻...");
        note.TextWrapping = TextWrapping.Wrap;
        note.Foreground = Brush("SecondaryForegroundColor", "#ACC2B3");
        note.Margin = new Thickness(0, 16, 0, 22);
        note.FontSize = 14;
        note.LineHeight = 24;
        body.Children.Add(note);
        _countdown = new TextBlock { FontSize = 48, FontWeight = FontWeights.Light, Margin = new Thickness(0, 0, 0, 20), Visibility = _resting ? Visibility.Visible : Visibility.Collapsed };
        body.Children.Add(_countdown);
        Panel actions = kind == HealthCardKind.Reminder ? new StackPanel() : new WrapPanel();
        if (kind == HealthCardKind.Countdown)
        {
            AddButton(actions, "收起到托盘", HealthCardAction.Collapse, true);
            AddButton(actions, "提前结束", HealthCardAction.EndEarly);
        }
        else if (kind is HealthCardKind.Preview or HealthCardKind.PreviewCountdown)
        {
            if (kind == HealthCardKind.Preview) AddButton(actions, "预览休息画面", HealthCardAction.StartBreak, true);
            AddButton(actions, "关闭预览", HealthCardAction.Close);
        }
        else
        {
            var start = AddButton(actions, "开始休息", HealthCardAction.StartBreak, true);
            start.Resources["ButtonCornerRadius"] = new CornerRadius(12);
            start.MinHeight = 46; start.Margin = new Thickness(0);
            start.SetBinding(ToolTipProperty, TextBinding("开始本次休息倒计时。"));
            var secondary = new DockPanel { LastChildFill = false, Margin = new Thickness(0, 12, 0, 0) };
            var snooze = AddButton(secondary, "稍后", HealthCardAction.Snooze);
            snooze.Style = (Style)FindResource("HealthPauseButton");
            snooze.MinWidth = 132; snooze.MinHeight = 56;
            snooze.Padding = new Thickness(0); snooze.Margin = new Thickness(0);
            snooze.SetBinding(ToolTipProperty, TextBinding("按设置的稍后时长，再提醒一次。"));
            var skip = AddButton(secondary, "跳过本次", HealthCardAction.Skip);
            skip.Style = (Style)FindResource("HealthSkipButton");
            skip.MinWidth = 132; skip.MinHeight = 56;
            skip.Padding = new Thickness(0); skip.Margin = new Thickness(0);
            skip.SetBinding(ToolTipProperty, TextBinding("跳过这次提醒，开始新的工作周期。"));
            DockPanel.SetDock(skip, Dock.Right);
            actions.Children.Add(secondary);
        }
        body.Children.Add(actions);
        Content = new Border
        {
            Background = Brush("SecondaryBackgroundColor", "#17372E"),
            BorderBrush = Brush("SeparatorColorBrush", "#3E6250"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(12),
            Child = body
        };
        Update(remaining);
        Loaded += (_, _) =>
        {
            PositionWithoutActivation();
            if (SystemParameters.ClientAreaAnimation && _appearance.MotionEnabled)
                BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromSeconds(1)));
        };
        _controlsTimer = new System.Windows.Threading.DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _controlsTimer.Tick += (_, _) =>
        {
            if (!_resting || !_immersive || DateTime.UtcNow - _lastInteraction < TimeSpan.FromSeconds(6)) return;
            if (Opacity > .2)
            {
                if (_appearance.MotionEnabled && SystemParameters.ClientAreaAnimation) BeginAnimation(OpacityProperty, new DoubleAnimation(.18, TimeSpan.FromSeconds(.7)));
                else { BeginAnimation(OpacityProperty, null); Opacity = .18; }
            }
        };
        if (_resting && _immersive) _controlsTimer.Start();
        MouseMove += (_, _) => RevealControls();
        GotKeyboardFocus += (_, _) => RevealControls();
        Closed += (_, _) => _controlsTimer.Stop();
        PreviewKeyDown += (_, e) =>
        {
            RevealControls();
            if (e.Key != Key.Escape || !IsKeyboardFocusWithin) return;
            e.Handled = true;
            Request(kind == HealthCardKind.Countdown ? HealthCardAction.EndEarly : HealthCardAction.Close);
        };
    }

    public void Update(TimeSpan remaining) => _countdown.Text = $"{Math.Max(0, (int)Math.Ceiling(remaining.TotalSeconds)) / 60:00}:{Math.Max(0, (int)Math.Ceiling(remaining.TotalSeconds)) % 60:00}";

    public void RevealControls()
    {
        _lastInteraction = DateTime.UtcNow;
        if (Opacity < 1) { BeginAnimation(OpacityProperty, null); Opacity = 1; }
    }

    public void CloseFromService() { _serviceClosing = true; Close(); }

    protected override void OnClosing(CancelEventArgs e)
    {
        if (!_serviceClosing)
        {
            e.Cancel = true;
            Dispatcher.BeginInvoke(new Action(() => Request(_kind == HealthCardKind.Countdown ? HealthCardAction.EndEarly : HealthCardAction.Close)));
        }
        base.OnClosing(e);
    }

    private void Request(HealthCardAction action) => ActionRequested?.Invoke(action);
    private Button AddButton(Panel panel, string key, HealthCardAction action, bool primary = false)
    {
        var button = new Button { FontSize = 13, MinHeight = 40, Padding = new Thickness(14, 10, 14, 10), Margin = new Thickness(0, 0, 8, 0) };
        button.SetBinding(ContentControl.ContentProperty, TextBinding(key));
        if (primary)
        {
            if (TryFindResource("PrimaryButton") is Style style) button.Style = style;
            button.FontWeight = FontWeights.SemiBold;
        }
        else
        {
            button.Background = Brushes.Transparent;
            button.BorderBrush = Brush("SecondaryForegroundColor", "#ACC2B3");
        }
        button.Click += (_, _) => Request(action);
        panel.Children.Add(button);
        return button;
    }
    private static TextBlock Label(string key) { var text = new TextBlock(); text.SetBinding(TextBlock.TextProperty, TextBinding(key)); return text; }
    private static Binding TextBinding(string key) => new($"[{key}]") { Source = UiText.Current, Mode = BindingMode.OneWay };
    private Brush Brush(string key, string fallback) => TryFindResource(key) as Brush ?? (SolidColorBrush)new BrushConverter().ConvertFromString(fallback)!;

    private void PositionWithoutActivation()
    {
        var hwnd = new WindowInteropHelper(this).Handle;
        // First move selects the monitor's DPI; use full bounds to align with the scene's center.
        SetWindowPos(hwnd, 0, _screen.Left, _screen.Top, 0, 0, 0x0015);
        Dispatcher.BeginInvoke(new Action(() =>
        {
            if (!IsVisible) return;
            var dpi = VisualTreeHelper.GetDpi(this);
            var x = _screen.FullLeft + Math.Max(0, (_screen.FullRight - _screen.FullLeft - (int)(ActualWidth * dpi.DpiScaleX)) / 2);
            var y = _screen.FullTop + Math.Max(0, (_screen.FullBottom - _screen.FullTop - (int)(ActualHeight * dpi.DpiScaleY)) / 2);
            SetWindowPos(hwnd, 0, x, y, 0, 0, 0x0015);
        }));
    }

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetWindowPos(nint hwnd, nint after, int x, int y, int width, int height, uint flags);
}
