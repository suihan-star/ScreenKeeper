using OLED_Sleeper.Features.HealthReminder;
using OLED_Sleeper.UI.Helpers;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace OLED_Sleeper.UI.Views;

/// <summary>No desktop capture. Reminder decoration passes input through; an explicit rest blocks hidden-app clicks.</summary>
public sealed class HealthSceneWindow : Window
{
    private readonly HealthScreen _screen;
    private readonly HealthAppearance _appearance;
    private readonly bool _resting;
    private bool _fadingAway;
    public event Action? PointerMoved;
    public bool PassesInputThrough => !_resting;

    public HealthSceneWindow(HealthScreen screen, HealthAppearance appearance, bool resting, ImageSource? picture = null)
    {
        _screen = screen; _appearance = appearance.Copy(); _resting = resting;
        WindowStyle = WindowStyle.None; ResizeMode = ResizeMode.NoResize;
        AllowsTransparency = true; Background = Brushes.Transparent;
        ShowInTaskbar = false; ShowActivated = false; Topmost = true;
        Width = Math.Max(1, screen.FullRight - screen.FullLeft);
        Height = Math.Max(1, screen.FullBottom - screen.FullTop);
        Content = CreateScene(appearance, resting, picture);
        Opacity = 0;
        SourceInitialized += (_, _) =>
        {
            var hwnd = new WindowInteropHelper(this).Handle;
            SetWindowLong(hwnd, -20, GetWindowLong(hwnd, -20) | 0x08000080 | (resting ? 0 : 0x20));
            HwndSource.FromHwnd(hwnd)?.AddHook((nint h, int message, nint w, nint l, ref bool handled) =>
            {
                if (message == 0x21) { handled = true; return (nint)3; } // MA_NOACTIVATE
                return 0;
            });
        };
        Loaded += (_, _) =>
        {
            Place();
            Dispatcher.BeginInvoke(new Action(Place));
            double target = resting ? 1 : appearance.OverlayPercent / 100d;
            if (AnimationsAllowed)
                BeginAnimation(OpacityProperty, new DoubleAnimation(0, target, TimeSpan.FromSeconds(resting ? 1.2 : appearance.FadeSeconds))
                { EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut } });
            else Opacity = target;
        };
        MouseMove += (_, _) => PointerMoved?.Invoke();
    }

    private bool AnimationsAllowed => _appearance.MotionEnabled && SystemParameters.ClientAreaAnimation;
    public void FadeAway()
    {
        if (_fadingAway || !AnimationsAllowed) return;
        _fadingAway = true;
        BeginAnimation(OpacityProperty, new DoubleAnimation(0, TimeSpan.FromSeconds(1)));
    }

    private void Place()
    {
        if (!IsVisible) return;
        SetWindowPos(new WindowInteropHelper(this).Handle, 0, _screen.FullLeft, _screen.FullTop,
            _screen.FullRight - _screen.FullLeft, _screen.FullBottom - _screen.FullTop, 0x0014);
    }

    public static FrameworkElement CreateScene(HealthAppearance appearance, bool resting, ImageSource? picture = null)
    {
        bool night = resting && appearance.RestStyle == HealthRestStyle.Night;
        bool animate = resting && appearance.MotionEnabled && SystemParameters.ClientAreaAnimation;
        if (!resting && appearance.ReminderStyle == HealthReminderStyle.EdgeGlow)
            return CreateFogScene(appearance);
        bool builtIn = picture == null;
        if (!night) picture ??= BuiltInBackground.Forest;
        if (!night && picture != null)
            return new Grid
            {
                Background = Solid("#102A23"),
                ClipToBounds = true,
                Children = { new Image { Source = picture, Stretch = Stretch.UniformToFill },
                new Border { Background = builtIn ? BuiltInBackground.Tint : new SolidColorBrush(Color.FromArgb(30, 12, 32, 26)) } }
            };

        var canvas = new Canvas { Width = 1200, Height = 750, ClipToBounds = true };
        var sky = new LinearGradientBrush(ColorOf(night ? "#10251F" : "#446B5C"), ColorOf(night ? "#284B3C" : "#A6B5A0"), 90);
        canvas.Children.Add(new Rectangle { Width = 1200, Height = 750, Fill = sky });
        Shape moon = night ? new Path
        {
            Data = new CombinedGeometry(GeometryCombineMode.Exclude, new EllipseGeometry(new Point(46, 46), 46, 46), new EllipseGeometry(new Point(21, 23), 43, 43)),
            Fill = Solid("#C1CEB1"),
            Opacity = .75
        } : new Ellipse { Width = 92, Height = 92, Fill = Solid("#D1D9C1"), Opacity = .75 };
        Canvas.SetLeft(moon, 865); Canvas.SetTop(moon, 118); canvas.Children.Add(moon);
        Mountain(canvas, "M0,420 Q100,380 220,330 Q285,302 350,345 L490,402 Q590,250 670,298 L890,432 Q1040,340 1200,389 L1200,750 L0,750 Z", night ? "#315445" : "#718F7B");
        Mountain(canvas, "M0,478 Q190,355 290,445 Q445,540 620,422 Q710,390 815,455 Q1020,510 1200,414 L1200,750 L0,750 Z", night ? "#254B3A" : "#547963");
        Mountain(canvas, "M0,582 Q105,495 258,553 Q390,594 535,518 Q695,447 806,536 Q965,618 1200,511 L1200,750 L0,750 Z", night ? "#1B3D2E" : "#365E48");
        Mountain(canvas, "M0,646 Q223,567 433,639 Q699,706 895,608 Q1070,565 1200,635 L1200,750 L0,750 Z", night ? "#132E24" : "#244B37");
        for (int i = 0; i < 3; i++)
        {
            var cloud = new Ellipse
            {
                Width = 720,
                Height = 85,
                Opacity = .045,
                Fill = new RadialGradientBrush(ColorOf("#E4ECD8"), Colors.Transparent),
                RenderTransform = new TranslateTransform()
            };
            Canvas.SetLeft(cloud, i * 365 - 220); Canvas.SetTop(cloud, 218 + i * 122); canvas.Children.Add(cloud);
            if (animate)
                ((TranslateTransform)cloud.RenderTransform).BeginAnimation(TranslateTransform.XProperty,
                    new DoubleAnimation(-14 * appearance.MotionStrength, 14 * appearance.MotionStrength, TimeSpan.FromSeconds(22 + i * 9))
                    { AutoReverse = true, RepeatBehavior = RepeatBehavior.Forever, EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut } });
        }
        return new Viewbox { Stretch = Stretch.UniformToFill, ClipToBounds = true, Child = canvas };
    }

    private static FrameworkElement CreateFogScene(HealthAppearance appearance)
    {
        // Window opacity is the single coverage control; an opaque base also covers the centre at 100%.
        var scene = new Grid
        {
            Background = new LinearGradientBrush(ColorOf("#102E28"), ColorOf("#193B2E"), 75),
            ClipToBounds = true,
            IsHitTestVisible = false
        };
        var motion = new Storyboard();
        double strength = appearance.MotionStrength == 2 ? 1.65 : 1;
        AddFog(new(.08, .14), new(.24, .19), .78, .56, "#527B56", .88, 9);
        AddFog(new(.94, .60), new(-.23, -.26), .66, .84, "#386C5B", .86, 13);
        AddFog(new(.38, 1.04), new(.28, -.25), .88, .52, "#648260", .64, 11);
        var edge = new RadialGradientBrush { Center = new(.5, .46), GradientOrigin = new(.5, .46), RadiusX = .73, RadiusY = .77 };
        edge.GradientStops.Add(new(Colors.Transparent, .3));
        edge.GradientStops.Add(new(Color.FromArgb(120, 8, 29, 24), 1));
        scene.Children.Add(new Border { Background = edge });
        if (appearance.MotionEnabled && SystemParameters.ClientAreaAnimation)
        {
            scene.Resources.Add("FogMotion", motion);
            scene.Loaded += (_, _) => motion.Begin(scene, HandoffBehavior.SnapshotAndReplace, true);
            scene.Unloaded += (_, _) => motion.Remove(scene);
        }
        return scene;

        void AddFog(Point centre, Vector drift, double radiusX, double radiusY, string tint, double opacity, double seconds)
        {
            var color = ColorOf(tint);
            var brush = new RadialGradientBrush
            {
                Center = centre,
                GradientOrigin = centre,
                RadiusX = radiusX,
                RadiusY = radiusY,
                Opacity = opacity
            };
            brush.GradientStops.Add(new(color, 0));
            brush.GradientStops.Add(new(Color.FromArgb(220, color.R, color.G, color.B), .34));
            brush.GradientStops.Add(new(Color.FromArgb(70, color.R, color.G, color.B), .72));
            brush.GradientStops.Add(new(Color.FromArgb(0, color.R, color.G, color.B), 1));
            var layer = new Border { Background = brush };
            scene.Children.Add(layer);
            foreach (var property in new[] { RadialGradientBrush.CenterProperty, RadialGradientBrush.GradientOriginProperty })
                Track(layer, property, new PointAnimation(centre, centre + drift * strength, TimeSpan.FromSeconds(seconds)));
            Track(layer, RadialGradientBrush.RadiusXProperty,
                new DoubleAnimation(radiusX, radiusX * (1 + .18 * strength), TimeSpan.FromSeconds(seconds + 3)));
            Track(layer, RadialGradientBrush.RadiusYProperty,
                new DoubleAnimation(radiusY, radiusY * (1 - .15 * strength), TimeSpan.FromSeconds(seconds + 2)));
        }

        void Track(Border target, DependencyProperty property, AnimationTimeline animation)
        {
            animation.AutoReverse = true;
            animation.RepeatBehavior = RepeatBehavior.Forever;
            var easing = new SineEase { EasingMode = EasingMode.EaseInOut };
            if (animation is PointAnimation point) point.EasingFunction = easing;
            if (animation is DoubleAnimation number) number.EasingFunction = easing;
            Storyboard.SetTarget(animation, target);
            Storyboard.SetTargetProperty(animation, new PropertyPath("(0).(1)", Border.BackgroundProperty, property));
            motion.Children.Add(animation);
        }
    }

    private static void Mountain(Canvas canvas, string geometry, string color) => canvas.Children.Add(new Path { Data = Geometry.Parse(geometry), Fill = Solid(color) });
    private static Color ColorOf(string color) => (Color)ColorConverter.ConvertFromString(color);
    private static SolidColorBrush Solid(string color) => new(ColorOf(color));
    [DllImport("user32.dll", EntryPoint = "GetWindowLongW")] private static extern int GetWindowLong(nint hwnd, int index);
    [DllImport("user32.dll", EntryPoint = "SetWindowLongW")] private static extern int SetWindowLong(nint hwnd, int index, int value);
    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetWindowPos(nint hwnd, nint after, int x, int y, int width, int height, uint flags);
}
