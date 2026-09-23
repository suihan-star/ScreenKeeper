using OLED_Sleeper.Native;
using OLED_Sleeper.UI.Helpers;
using OLED_Sleeper.UI.Localization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace OLED_Sleeper.Features.MonitorPrivacy;

public sealed class PrivacyCurtainWindow : Window
{
    private readonly StackPanel _panel = new() { MaxWidth = 430, Margin = new Thickness(32), HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
    private readonly Button _releaseButton = new() { Padding = new Thickness(26, 12, 26, 12), HorizontalAlignment = HorizontalAlignment.Center, Background = Brush("#B4D6A1"), Foreground = Brush("#173326"), FontSize = 14, IsEnabled = false };
    private readonly DispatcherTimer _timer;
    private readonly Grid _surface = new();
    private readonly Image _image = new() { Stretch = Stretch.UniformToFill, IsHitTestVisible = false };
    private readonly Border _shade = new() { Background = BuiltInBackground.Tint, IsHitTestVisible = false, Visibility = Visibility.Hidden };
    private PrivacyImageStore? _images;
    private PrivacyOptions _appearance = new();
    private int _imageIndex;
    private long _nextImage;
    public void SetAppearance(PrivacyImageStore? images, PrivacyOptions appearance)
    {
        _images = images; _appearance = appearance.Copy(); _imageIndex = 0; _nextImage = 0;
        _image.BeginAnimation(OpacityProperty, null);
        _image.Opacity = 1;
        _image.Stretch = appearance.UseImages && appearance.ContainImages ? Stretch.Uniform : Stretch.UniformToFill;
        ShowNextImage();
    }
    private void ShowNextImage()
    {
        _nextImage = long.MaxValue;
        _shade.Visibility = Visibility.Hidden;
        if (!_appearance.UseImages)
        {
            _image.Source = _appearance.UseBuiltInBackground ? BuiltInBackground.Forest : null;
            if (_image.Source != null && !Sleeping) _shade.Visibility = Visibility.Visible;
            return;
        }
        if (_appearance.Images.Length == 0 || _images == null) { _image.Source = null; return; }
        try { _image.Source = _images.Load(_appearance.Images[_imageIndex++ % _appearance.Images.Length]); }
        catch { _image.Source = null; } // The opaque solid background is always present.
        // A hidden, unfinished slideshow setting must not turn a fallback curtain into a rapid slideshow.
        _nextImage = _appearance.Images.Length > 1 && _appearance.SlideSeconds is >= 1 and <= 3600 &&
            _appearance.SlideUnit is UI.Models.TimeUnit.Seconds or UI.Models.TimeUnit.Minutes
            ? Environment.TickCount64 + _appearance.SlideSeconds * 1000L : long.MaxValue;
    }
    private Action _release = () => { };
    private Rect _bounds;
    private long _armedAt;
    private bool _canClose, _requested;
    public bool Sleeping { get; private set; }
    public bool DesktopCompleted { get; set; }

    public PrivacyCurtainWindow()
    {
        Resources.MergedDictionaries.Add(UI.Themes.UiTheme.Palette());
        WindowStyle = WindowStyle.None;
        ResizeMode = ResizeMode.NoResize;
        ShowActivated = false;
        ShowInTaskbar = false;
        Topmost = true;
        Background = Brush("#102A23");
        Foreground = Brush("#E8EFE5");
        FontFamily = new FontFamily("Segoe UI, Microsoft YaHei UI");
        Bind(_releaseButton, ContentControl.ContentProperty, "进入此屏幕");
        _panel.Children.Add(_releaseButton);
        _surface.Children.Add(_image);
        _surface.Children.Add(_shade);
        _surface.Children.Add(_panel);
        Content = _surface;
        _releaseButton.Click += (_, e) =>
        {
            e.Handled = true;
            if (_requested || Sleeping || Environment.TickCount64 < _armedAt) return;
            _requested = true;
            _releaseButton.IsEnabled = false;
            // Finish this mouse-up before uncovering the underlying application.
            Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() => _release()));
        };
        Closing += (_, e) => e.Cancel = !_canClose;
        SourceInitialized += (_, _) =>
        {
            var hwnd = new WindowInteropHelper(this).Handle;
            var style = NativeMethods.GetWindowLongPtr(hwnd, NativeMethods.GWL_EXSTYLE);
            NativeMethods.SetWindowLongPtr(hwnd, NativeMethods.GWL_EXSTYLE, new nint(style.ToInt64() | NativeMethods.WS_EX_NOACTIVATE));
            Position();
        };
        _timer = new DispatcherTimer(TimeSpan.FromSeconds(1), DispatcherPriority.Background, (_, _) =>
        {
            Position();
            if (!Sleeping && Environment.TickCount64 >= _nextImage) ShowNextImage();
            if (!_requested) _releaseButton.IsEnabled = Environment.TickCount64 >= _armedAt;
        }, Dispatcher);
        _timer.Stop();
        SetAppearance(null, new());
        Loaded += (_, _) =>
        {
            // Animate only the artwork. The window stays fully opaque from its first frame.
            if (!Sleeping && !_appearance.UseImages && _appearance.UseBuiltInBackground && SystemParameters.ClientAreaAnimation)
                _image.BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(650)));
        };
    }
    public void Configure(Rect bounds, Action release, bool desktopPending)
    {
        _bounds = bounds;
        _release = release;
        DesktopCompleted = false;
        _requested = false;
        _armedAt = Environment.TickCount64 + 750;
        _releaseButton.IsEnabled = false;
        if (new WindowInteropHelper(this).Handle != 0) Position();
    }
    public void MoveTo(Rect bounds) { _bounds = bounds; Position(); }
    public void Present() { Show(); Position(); _timer.Start(); }
    public void Failed() { /* Failure details belong in settings; the curtain has only a release button. */ }
    public void SetSleeping(bool sleeping)
    {
        Sleeping = sleeping;
        Background = sleeping ? Brushes.Black : Brush("#102A23");
        _image.Visibility = sleeping ? Visibility.Hidden : Visibility.Visible;
        _shade.Visibility = !sleeping && !_appearance.UseImages && _appearance.UseBuiltInBackground && _image.Source != null ? Visibility.Visible : Visibility.Hidden;
        _panel.Visibility = sleeping ? Visibility.Hidden : Visibility.Visible;
        Cursor = sleeping ? System.Windows.Input.Cursors.None : System.Windows.Input.Cursors.Arrow;
    }
    public void Dismiss() { _timer.Stop(); _canClose = true; Close(); }
    private void Position()
    {
        var hwnd = new WindowInteropHelper(this).Handle;
        if (hwnd != 0 && !_bounds.IsEmpty)
            NativeMethods.SetWindowPos(hwnd, NativeMethods.HWND_TOPMOST, (int)_bounds.X, (int)_bounds.Y, (int)_bounds.Width, (int)_bounds.Height, NativeMethods.SWP_NOACTIVATE);
    }
    private static SolidColorBrush Brush(string color) => new((Color)ColorConverter.ConvertFromString(color));
    private static void Bind(DependencyObject target, DependencyProperty property, string key) =>
        BindingOperations.SetBinding(target, property, new Binding("[" + key + "]") { Source = UiText.Current, Mode = BindingMode.OneWay });
}

/// <summary>UI-thread window ownership; ordinary blackout restore never dismisses a privacy curtain.</summary>
public sealed class PrivacyCurtainHost(PrivacyImageStore? images = null) : IPrivacyCurtainHost
{
    private readonly Dictionary<string, PrivacyOptions> _appearances = new();
    public void ConfigureAppearance(string id, PrivacyOptions options)
    {
        _appearances[id] = options.Copy();
        if (_windows.TryGetValue(id, out var existing)) existing.SetAppearance(images, options);
    }
    private readonly Dictionary<string, PrivacyCurtainWindow> _windows = new();
    private readonly HashSet<string> _sleeping = new();
    private readonly HashSet<nint> _handles = new();
    public void Show(string id, Rect bounds, Action release, bool desktopPending)
    {
        if (!_windows.TryGetValue(id, out var window))
        {
            window = new PrivacyCurtainWindow();
            _windows.Add(id, window);
        }
        window.Configure(bounds, release, desktopPending);
        window.SetAppearance(images, _appearances.GetValueOrDefault(id) ?? new());
        window.SetSleeping(_sleeping.Contains(id));
        window.Present();
        lock (_handles) _handles.Add(new WindowInteropHelper(window).Handle);
    }
    public void Move(string id, Rect bounds) { if (_windows.TryGetValue(id, out var window)) window.MoveTo(bounds); }
    public void Close(string id)
    {
        if (!_windows.Remove(id, out var window)) return;
        lock (_handles) _handles.Remove(new WindowInteropHelper(window).Handle);
        window.Dismiss();
    }
    public void DesktopReady(string id)
    {
        if (!_windows.TryGetValue(id, out var window)) return;
        window.DesktopCompleted = true;
        if (!window.Sleeping) Close(id);
    }
    public void DesktopFailed(string id) { if (_windows.TryGetValue(id, out var window)) window.Failed(); }
    public bool SetSleeping(string id, bool sleeping)
    {
        if (sleeping) _sleeping.Add(id); else _sleeping.Remove(id);
        if (!_windows.TryGetValue(id, out var window)) return false;
        if (!sleeping && window.DesktopCompleted) { Close(id); return false; }
        window.SetSleeping(sleeping);
        return true;
    }
    public bool IsCurtainWindow(nint handle) { lock (_handles) return _handles.Contains(handle); }
    public void Dispose()
    {
        foreach (var id in _windows.Keys.ToArray()) Close(id);
        _sleeping.Clear();
    }
}
