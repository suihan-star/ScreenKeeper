using Moq;
using OLED_Sleeper.Features.ColorTemperature;
using OLED_Sleeper.Features.HealthReminder;
using OLED_Sleeper.UI.Localization;
using OLED_Sleeper.UI.Views;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace OLED_Sleeper.Tests.Features.HealthReminder;

[Collection("UI language")]
public sealed class HealthAppearanceTests
{
    [Fact]
    public void OldSettingsLoadAppearanceDefaultsAndRejectUnsafeReferences()
    {
        var old = JsonSerializer.Deserialize<HealthReminderSettings>("""{"Schema":1,"Enabled":false}""")!;
        Assert.Equal(8, old.Appearance.FadeSeconds); Assert.Equal(30, old.Appearance.OverlayPercent);
        Assert.Empty(old.Validate());
        old.Appearance.Image = "../private.png"; Assert.NotEmpty(old.Validate());
        old.Appearance.Image = Guid.NewGuid().ToString("N") + ".png"; Assert.Empty(old.Validate());
        old.Appearance.OverlayPercent = 100; Assert.Empty(old.Validate());
        old.Appearance.OverlayPercent = 101; Assert.NotEmpty(old.Validate());
        old.Appearance.OverlayPercent = 10; Assert.Empty(old.Validate());
        old.Appearance.OverlayPercent = 9; Assert.NotEmpty(old.Validate());
    }

    [WpfFact]
    public void SceneNativeStylesPassReminderClicksButBlockClicksIntoHiddenAppsDuringRest()
    {
        foreach (bool rest in new[] { false, true })
        {
            var scene = new HealthSceneWindow(new("fixture", 0, 0, 700, 500), new(), rest);
            try
            {
                var hwnd = new WindowInteropHelper(scene).EnsureHandle();
                var style = GetWindowLong(hwnd, -20);
                Assert.Equal(!rest, (style & 0x20) != 0);
                Assert.NotEqual(0, style & 0x08000000);
                Assert.False(scene.ShowActivated); Assert.False(scene.ShowInTaskbar);
                Assert.Equal((nint)3, SendMessage(hwnd, 0x21, 0, 0));
            }
            finally { scene.Close(); }
        }
    }

    [WpfFact]
    public void ReminderAndRestScenesRenderAtBothSizesWithoutReadingDesktopContent()
    {
        Resources();
        string language = UiText.Current.Language;
        try
        {
            foreach (var lang in new[] { "zh-CN", "en" })
                foreach (var size in new[] { new Size(1280, 800), new Size(700, 500) })
                    foreach (var style in new[] { "Scenic", "EdgeGlow", "EdgeGlow100", "Night", "Picture" })
                    {
                        UiText.Current.SetLanguage(lang);
                        bool rest = style is "Night" or "Picture";
                        var appearance = new HealthAppearance
                        {
                            MotionEnabled = false,
                            ReminderStyle = style.StartsWith("EdgeGlow", StringComparison.Ordinal) ? HealthReminderStyle.EdgeGlow : HealthReminderStyle.Scenic,
                            OverlayPercent = style == "EdgeGlow100" ? 100 : 30,
                            RestStyle = style == "Picture" ? HealthRestStyle.Picture : HealthRestStyle.Night
                        };
                        var root = new Grid { Width = size.Width, Height = size.Height, ClipToBounds = true, Background = new SolidColorBrush(Color.FromRgb(235, 238, 232)) };
                        var work = new StackPanel { Margin = new Thickness(54) };
                        work.Children.Add(new TextBlock { Text = "Example workspace", FontSize = 28, Foreground = Brushes.DarkSlateGray });
                        work.Children.Add(new TextBlock { Text = "ScreenKeeper · Fictional content for visual verification", Margin = new Thickness(0, 15, 0, 30), Foreground = Brushes.DarkSlateGray });
                        for (int i = 0; i < 5; i++) work.Children.Add(new Border { Background = Brushes.WhiteSmoke, Height = 24, Margin = new Thickness(0, 0, 140 + i * 25, 14) });
                        root.Children.Add(work);
                        var backdrop = HealthSceneWindow.CreateScene(appearance, rest); backdrop.Opacity = rest ? 1 : appearance.OverlayPercent / 100d;
                        root.Children.Add(backdrop);
                        var card = new HealthReminderWindow(rest ? HealthCardKind.Countdown : HealthCardKind.Reminder, new("fixture", 0, 0, (int)size.Width, (int)size.Height), TimeSpan.FromSeconds(135), appearance, true);
                        try
                        {
                            var content = (FrameworkElement)card.Content; card.Content = null;
                            // Preserve the window's local palette and templates in the offscreen composition.
                            foreach (var dictionary in card.Resources.MergedDictionaries)
                                content.Resources.MergedDictionaries.Add(dictionary);
                            content.Width = card.Width; content.HorizontalAlignment = HorizontalAlignment.Center;
                            content.VerticalAlignment = VerticalAlignment.Center;
                            content.SetValue(TextBlock.ForegroundProperty, card.Foreground);
                            root.Children.Add(content);
                            root.Measure(size); root.Arrange(new Rect(size)); root.UpdateLayout();
                            Assert.True(content.ActualHeight < size.Height);
                            Render(root, size, $"scene-{lang}-{size.Width}-{style}");
                        }
                        finally { card.CloseFromService(); }
                    }
        }
        finally { UiText.Current.SetLanguage(language); }
    }

    [WpfFact]
    public async Task FogFlowsWithoutTransparencyGapsAndStopsWhenUnloaded()
    {
        foreach (bool enabled in new[] { false, true })
        {
            var size = new Size(700, 500);
            var scene = HealthSceneWindow.CreateScene(new() { ReminderStyle = HealthReminderStyle.EdgeGlow, OverlayPercent = 100, MotionEnabled = enabled }, false);
            scene.Measure(size); scene.Arrange(new Rect(size)); scene.UpdateLayout();
            scene.RaiseEvent(new RoutedEventArgs(FrameworkElement.LoadedEvent));
            try
            {
                var motion = scene.Resources.Values.OfType<Storyboard>().SingleOrDefault();
                Assert.Equal(enabled && SystemParameters.ClientAreaAnimation, motion != null);
                if (motion != null)
                    Assert.True(Descendants<Border>(scene).Any(b => b.Background.HasAnimatedProperties), "Loaded must attach fog animations.");
                motion?.SeekAlignedToLastTick(scene, TimeSpan.Zero, TimeSeekOrigin.BeginTime);
                await Dispatcher.Yield(DispatcherPriority.Render);
                var initialCentres = Descendants<Border>(scene).Select(b => ((RadialGradientBrush)b.Background).Center).ToArray();
                var first = Pixels(scene, size);
                Assert.All(first.Where((_, i) => i % 4 == 3), alpha => Assert.Equal(byte.MaxValue, alpha));
                Render(scene, size, $"fog-{enabled}-start");
                motion?.SeekAlignedToLastTick(scene, TimeSpan.FromSeconds(6), TimeSeekOrigin.BeginTime);
                await Dispatcher.Yield(DispatcherPriority.Render);
                var movedCentres = Descendants<Border>(scene).Select(b => ((RadialGradientBrush)b.Background).Center).ToArray();
                Assert.True((motion == null) == initialCentres.SequenceEqual(movedCentres),
                    $"Fog motion: {motion?.GetCurrentState(scene)} at {motion?.GetCurrentTime(scene)}; {string.Join("; ", initialCentres)} -> {string.Join("; ", movedCentres)}");
                var later = Pixels(scene, size);
                Assert.Equal(motion == null, first.SequenceEqual(later));
                Render(scene, size, $"fog-{enabled}-six-seconds");
            }
            finally { scene.RaiseEvent(new RoutedEventArgs(FrameworkElement.UnloadedEvent)); }
            await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
            // Brush invalidation is deferred to WPF's next timing tick; the controllable scene clock is removed now.
            var removed = scene.Resources.Values.OfType<Storyboard>().SingleOrDefault();
            if (removed != null) Assert.Throws<InvalidOperationException>(() => removed.GetCurrentState(scene));
        }
    }

    private static byte[] Pixels(FrameworkElement scene, Size size)
    {
        var bitmap = new RenderTargetBitmap((int)size.Width, (int)size.Height, 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(scene);
        var pixels = new byte[(int)size.Width * (int)size.Height * 4];
        bitmap.CopyPixels(pixels, (int)size.Width * 4, 0);
        return pixels;
    }

    [WpfFact]
    public void VerificationDialogStartsWithAnExplicitObservationStepInBothLanguages()
    {
        Resources(); string language = UiText.Current.Language;
        try
        {
            foreach (var lang in new[] { "zh-CN", "en" })
            {
                UiText.Current.SetLanguage(lang);
                var service = new ColorTemperatureService(Mock.Of<IColorTemperatureBackend>(), Mock.Of<IColorRecoveryStore>());
                var dialog = new ColorValidationWindow(service, Mock.Of<IColorValidationRegistry>(r => r.IsAvailable), "fixture", "Test display · SDR");
                try
                {
                    var content = (FrameworkElement)dialog.Content;
                    ((ScrollViewer)content).Background = dialog.Background;
                    var size = new Size(540, 640); content.Measure(size); content.Arrange(new Rect(size)); content.UpdateLayout();
                    var buttons = Descendants<Button>(content).ToArray();
                    Assert.False(buttons.Single(b => Equals(b.Content, UiText.Get("开始 15 秒验证"))).IsEnabled);
                    Assert.False(service.IsValidating);
                    Render(content, size, $"color-validation-{lang}");
                }
                finally { dialog.Close(); }
            }
        }
        finally { UiText.Current.SetLanguage(language); }
    }

    private static void Resources()
    {
        if (Application.Current == null) new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
        Application.Current!.Resources.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri("/ScreenKeeper;component/UI/Themes/Calm.xaml", UriKind.Relative) });
    }
    private static IEnumerable<T> Descendants<T>(DependencyObject parent) where T : DependencyObject
    {
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i); if (child is T typed) yield return typed;
            foreach (var item in Descendants<T>(child)) yield return item;
        }
    }
    private static void Render(FrameworkElement content, Size size, string name)
    {
        var path = Environment.GetEnvironmentVariable("SCREENKEEPER_RENDER_QA"); if (string.IsNullOrWhiteSpace(path)) return;
        Directory.CreateDirectory(path);
        var bitmap = new RenderTargetBitmap((int)size.Width, (int)size.Height, 96, 96, PixelFormats.Pbgra32); bitmap.Render(content);
        var png = new PngBitmapEncoder(); png.Frames.Add(BitmapFrame.Create(bitmap));
        using var stream = File.Create(Path.Combine(path, name + ".png")); png.Save(stream);
    }
    [DllImport("user32.dll", EntryPoint = "GetWindowLongW")] private static extern int GetWindowLong(nint hwnd, int index);
    [DllImport("user32.dll")] private static extern nint SendMessage(nint hwnd, uint message, nint w, nint l);
}
