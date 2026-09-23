using Moq;
using OLED_Sleeper.Features.HealthReminder;
using OLED_Sleeper.Features.MonitorInformation.Models;
using OLED_Sleeper.Features.MonitorPrivacy;
using OLED_Sleeper.Features.UserSettings.Models;
using OLED_Sleeper.Storage.Interfaces;
using OLED_Sleeper.UI.Helpers;
using OLED_Sleeper.UI.Localization;
using OLED_Sleeper.UI.Themes;
using OLED_Sleeper.UI.ViewModels;
using OLED_Sleeper.UI.Views;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace OLED_Sleeper.Tests.Features.MonitorPrivacy;

[Collection("UI language")]
public sealed class SharedBackgroundTests
{
    [Fact]
    public void OldSettingsAndExplicitChoicesSurviveCopyDraftAndDiscard()
    {
        var old = JsonSerializer.Deserialize<PrivacyOptions>("{}")!;
        Assert.True(old.UseBuiltInBackground); Assert.False(old.UseImages);
        var config = Config(); config.ApplySettings(new MonitorSettings { PrivacyOptions = old }); config.MarkAsSaved();
        Assert.True(config.UseBuiltInBackground); Assert.False(config.UseSolid);
        config.UseSolid = true;
        Assert.True(config.IsDirty); Assert.False(config.UseBuiltInBackground);
        var rebuilt = Config(); rebuilt.PreserveDraftFrom(config);
        Assert.True(rebuilt.UseSolid); rebuilt.Revert(); Assert.True(rebuilt.UseBuiltInBackground);
        var target = Config(); target.MarkAsSaved(); config.CopyTo(target);
        Assert.True(target.UseSolid); Assert.True(target.IsDirty);
        var restored = JsonSerializer.Deserialize<PrivacyOptions>(JsonSerializer.Serialize(config.Options))!;
        Assert.False(restored.UseBuiltInBackground);
        target.Revert(); Assert.True(target.UseBuiltInBackground);
        config.SetImages([Guid.NewGuid().ToString("N") + ".png"]); config.UseImages = true;
        Assert.False(config.UseSolid); Assert.False(config.UseBuiltInBackground);
        config.UseBuiltInBackground = true; Assert.False(config.UseImages); Assert.Single(config.Images);
        config.UseImages = true; Assert.Single(config.Images);
        Assert.Equal(HealthRestStyle.Picture, new HealthAppearance().RestStyle);
        var savedHealth = JsonSerializer.Deserialize<HealthAppearance>("""{"ReminderStyle":1,"RestStyle":0}""")!;
        Assert.Equal(HealthRestStyle.Night, savedHealth.RestStyle);
        Assert.Equal(HealthReminderStyle.EdgeGlow, savedHealth.ReminderStyle);
    }

    [WpfFact]
    public void EmbeddedForestIsFrozenAndSharedButCustomPicturesAndExistingStylesTakePrecedence()
    {
        Resources();
        var image = Assert.IsType<BitmapImage>(BuiltInBackground.Forest);
        Assert.True(image.IsFrozen); Assert.Equal(1672, image.PixelWidth); Assert.Equal(941, image.PixelHeight);
        Assert.Same(image, BuiltInBackground.Forest);
        foreach (bool rest in new[] { false, true })
        {
            var scene = HealthSceneWindow.CreateScene(new(), rest);
            Assert.Same(image, Descendants<Image>(scene).Single().Source);
            var custom = BitmapSource.Create(1, 1, 96, 96, PixelFormats.Bgra32, null, new byte[] { 30, 60, 90, 255 }, 4);
            Assert.Same(custom, Descendants<Image>(HealthSceneWindow.CreateScene(new(), rest, custom)).Single().Source);
        }
        Assert.Empty(Descendants<Image>(HealthSceneWindow.CreateScene(new() { ReminderStyle = HealthReminderStyle.EdgeGlow }, false)));
        Assert.Empty(Descendants<Image>(HealthSceneWindow.CreateScene(new() { RestStyle = HealthRestStyle.Night }, true)));
    }

    [WpfFact]
    public void CurtainIsOpaqueDuringImageFadeAndBlackDuringSleepWithSafeCustomFallback()
    {
        Resources();
        var window = new PrivacyCurtainWindow();
        var directory = Path.Combine(Path.GetTempPath(), "screenkeeper-background-" + Guid.NewGuid().ToString("N"));
        try
        {
            var surface = (Grid)window.Content;
            var image = surface.Children.OfType<Image>().Single();
            Assert.Same(BuiltInBackground.Forest, image.Source);
            Assert.False(window.AllowsTransparency); Assert.Equal(1, window.Opacity);
            Assert.Equal(255, ((SolidColorBrush)window.Background).Color.A);
            surface.Background = window.Background;
            image.Opacity = 0;
            // Check the covering layers separately from WPF text antialiasing (which can round alpha to 254).
            var panel = surface.Children.OfType<StackPanel>().Single(); panel.Visibility = Visibility.Hidden;
            var first = Render(surface, new Size(700, 500), "curtain-first-frame");
            Assert.All(first.Where((_, i) => i % 4 == 3), a => Assert.Equal(255, a));
            image.Opacity = 1; panel.Visibility = Visibility.Visible;
            foreach (var size in new[] { new Size(1280, 800), new Size(700, 500), new Size(700, 1000) })
                Render(surface, size, $"curtain-forest-{size.Width}-{size.Height}");
            Assert.Single(Descendants<Button>(surface));
            window.SetSleeping(true); surface.Background = window.Background;
            var sleeping = Render(surface, new Size(700, 500), "curtain-sleep");
            Assert.All(sleeping.Where((_, i) => i % 4 != 3), c => Assert.Equal(0, c));
            window.SetSleeping(false); surface.Background = window.Background;
            Assert.Equal(Visibility.Visible, image.Visibility);
            window.SetAppearance(null, new() { UseBuiltInBackground = false }); Assert.Null(image.Source);
            window.SetAppearance(null, new() { UseImages = true }); Assert.Null(image.Source);
            Directory.CreateDirectory(directory);
            var store = new PrivacyImageStore(Mock.Of<IStorageRoot>(r => r.DirectoryPath == directory));
            var name = Guid.NewGuid().ToString("N") + ".png";
            Directory.CreateDirectory(Path.Combine(directory, "PrivacyImages"));
            var png = new PngBitmapEncoder();
            png.Frames.Add(BitmapFrame.Create(BitmapSource.Create(1, 1, 96, 96, PixelFormats.Bgra32, null, new byte[] { 30, 60, 90, 255 }, 4)));
            using (var stream = File.Create(Path.Combine(directory, "PrivacyImages", name))) png.Save(stream);
            window.SetAppearance(store, new() { UseImages = true, Images = [name], ContainImages = true });
            Assert.NotNull(image.Source); Assert.NotSame(BuiltInBackground.Forest, image.Source); Assert.Equal(Stretch.Uniform, image.Stretch);
            Assert.Equal(Visibility.Hidden, surface.Children.OfType<Border>().Single().Visibility);
            window.SetAppearance(store, new() { UseImages = true, Images = [Guid.NewGuid().ToString("N") + ".png"] });
            Assert.Null(image.Source); Assert.Equal(1, window.Opacity);
            window.SetAppearance(store, new()); Assert.Same(BuiltInBackground.Forest, image.Source);
        }
        finally { window.Dismiss(); if (Directory.Exists(directory)) Directory.Delete(directory, true); }
    }

    [WpfFact]
    public void ForestHealthScenesCoverLandscapeAndPortraitWithoutGaps()
    {
        Resources();
        foreach (var size in new[] { new Size(1280, 800), new Size(700, 1000) })
            foreach (bool rest in new[] { false, true })
            {
                var pixels = Render(HealthSceneWindow.CreateScene(new() { MotionEnabled = false }, rest), size, $"health-forest-{rest}-{size.Width}-{size.Height}");
                Assert.All(pixels.Where((_, i) => i % 4 == 3), a => Assert.Equal(255, a));
            }
    }

    [WpfFact]
    public void PrivacyAppearanceChoicesUpdateThumbnailAndDraftInBothThemesAndLanguages()
    {
        Resources();
        Application.Current!.Resources.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri("/ScreenKeeper;component/UI/Themes/Calm.xaml", UriKind.Relative) });
        var language = UiText.Current.Language;
        try
        {
            foreach (var theme in new[] { "forest", "fog" })
                foreach (var lang in new[] { "zh-CN", "en" })
                {
                    UiText.Current.SetLanguage(lang);
                    var config = Config(); config.IsManaged = true; config.PrivacyMode = PrivacyMode.Curtain; config.MarkAsSaved();
                    var view = new PrivacySettingsView { DataContext = config, Background = UiTheme.Palette(theme)["PrimaryBackgroundColor"] as Brush };
                    view.Resources.MergedDictionaries.Add(UiTheme.Palette(theme));
                    view.RaiseEvent(new RoutedEventArgs(FrameworkElement.LoadedEvent));
                    try
                    {
                        Render(view, new Size(460, 800), $"privacy-background-settings-{theme}-{lang}");
                        Assert.Empty(Descendants<RadioButton>(view).Where(b => b.GroupName == "Appearance"));
                        Assert.NotNull(view.FindName("AppearanceButton"));
                        var image = (Image)view.FindName("CurtainThumbnail");
                        Assert.Same(BuiltInBackground.Forest, image.Source);
                        var appearance = config.Options.Copy(); appearance.UseBuiltInBackground = false;
                        config.ApplyPrivacyAppearance(appearance);
                        Assert.True(config.UseSolid); Assert.Null(image.Source); Assert.True(config.IsDirty);
                        appearance.UseImages = true; config.ApplyPrivacyAppearance(appearance);
                        Assert.True(config.UseImages); Assert.False(config.UseBuiltInBackground); Assert.Null(image.Source);
                        config.Revert(); Assert.True(config.UseBuiltInBackground); Assert.Same(BuiltInBackground.Forest, image.Source);
                    }
                    finally { view.RaiseEvent(new RoutedEventArgs(FrameworkElement.UnloadedEvent)); }
                }
        }
        finally { UiText.Current.SetLanguage(language); }
    }

    private static MonitorConfigurationViewModel Config() => new(new MonitorInfo
    {
        HardwareId = "background-fixture",
        Bounds = new Rect(0, 0, 1920, 1080),
        Capabilities = new DdcCiCapabilities(true, 100)
    });
    private static void Resources()
    {
        if (Application.Current == null) new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
    }
    private static IEnumerable<T> Descendants<T>(DependencyObject parent) where T : DependencyObject
    {
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            if (child is T typed) yield return typed;
            foreach (var item in Descendants<T>(child)) yield return item;
        }
    }
    private static byte[] Render(FrameworkElement content, Size size, string name)
    {
        content.Measure(size); content.Arrange(new Rect(size)); content.UpdateLayout();
        var bitmap = new RenderTargetBitmap((int)size.Width, (int)size.Height, 96, 96, PixelFormats.Pbgra32); bitmap.Render(content);
        var pixels = new byte[(int)size.Width * (int)size.Height * 4]; bitmap.CopyPixels(pixels, (int)size.Width * 4, 0);
        var directory = Environment.GetEnvironmentVariable("SCREENKEEPER_RENDER_QA");
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
            var png = new PngBitmapEncoder(); png.Frames.Add(BitmapFrame.Create(bitmap));
            using var stream = File.Create(Path.Combine(directory, name + ".png")); png.Save(stream);
        }
        return pixels;
    }
}
