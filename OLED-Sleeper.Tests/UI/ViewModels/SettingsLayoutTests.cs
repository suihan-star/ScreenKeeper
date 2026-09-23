using Moq;
using OLED_Sleeper.Features.MonitorInformation.Models;
using OLED_Sleeper.Features.MonitorPrivacy;
using OLED_Sleeper.Infrastructure.Runtime.Interfaces;
using OLED_Sleeper.Storage.Interfaces;
using OLED_Sleeper.UI.Localization;
using OLED_Sleeper.UI.Services.Interfaces;
using OLED_Sleeper.UI.ViewModels;
using OLED_Sleeper.UI.Views;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace OLED_Sleeper.Tests.UI.ViewModels;

[Collection("UI language")]
public class SettingsLayoutTests
{
    [WpfFact]
    public async Task AppearanceDialogKeepsCancelSeparateAndAppliesOnlyAppearanceDraft()
    {
        if (Application.Current == null) new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
        Application.Current!.Resources.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri("/ScreenKeeper;component/UI/Themes/Calm.xaml", UriKind.Relative) });
        var owner = new Window(); new WindowInteropHelper(owner).EnsureHandle();
        var main = new MainViewModel(Mock.Of<IWorkspaceService>(), Mock.Of<IMonitorSettingsSaveService>(), Mock.Of<IDispatcher>());
        var config = new MonitorConfigurationViewModel(new MonitorInfo { HardwareId = "fixture" });
        config.IsPrivacyEnabled = true; config.QuietWhileInUse = true; config.MarkAsSaved();
        string language = UiText.Current.Language;
        try
        {
            foreach (string theme in new[] { "forest", "fog" })
                foreach (string lang in new[] { "zh-CN", "en" })
                {
                    UiText.Current.SetLanguage(lang);
                    var dialog = SettingsDialogs.CreatePrivacyAppearanceDialog(owner, main, config);
                    var palette = OLED_Sleeper.UI.Themes.UiTheme.Palette(theme);
                    foreach (var key in palette.Keys) dialog.Resources[key] = palette[key];
                    var choices = dialog.Body.Children.OfType<WrapPanel>().Single().Children.OfType<RadioButton>().ToArray();
                    Assert.True(choices[0].IsChecked); choices[1].IsChecked = true;
                    Assert.True(config.UseBuiltInBackground); Assert.False(config.IsDirty);
                    var content = (FrameworkElement)dialog.Content; var size = new Size(440, 440);
                    await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
                    content.Measure(size); content.Arrange(new Rect(size)); content.UpdateLayout();
                    Render(content, size, $"privacy-appearance-dialog-{theme}-{lang}");
                    dialog.Close(); Assert.True(config.UseBuiltInBackground); Assert.False(config.IsDirty);
                }
            var applyDialog = SettingsDialogs.CreatePrivacyAppearanceDialog(owner, main, config);
            var modes = applyDialog.Body.Children.OfType<WrapPanel>().Single().Children.OfType<RadioButton>().ToArray();
            var apply = applyDialog.Actions.Children.OfType<Button>().Last();
            modes[2].IsChecked = true; apply.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Assert.False(config.IsDirty); // Missing custom images are rejected without changing the draft.
            modes[1].IsChecked = true; apply.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Assert.True(config.UseSolid); Assert.True(config.IsPrivacyDirty); Assert.True(config.QuietWhileInUse);
            Assert.True(config.SavedPrivacyOptions.UseBuiltInBackground);
            config.Revert(OLED_Sleeper.Features.UserSettings.Models.MonitorSettingsScope.Privacy);
            Assert.True(config.UseBuiltInBackground); Assert.False(config.IsDirty);
        }
        finally { owner.Close(); UiText.Current.SetLanguage(language); }
    }

    [WpfFact]
    public async Task SnoozeDialogValidatesUnitsAndKeepsJointOptional()
    {
        if (Application.Current == null) new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
        Application.Current!.Resources.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri("/ScreenKeeper;component/UI/Themes/Calm.xaml", UriKind.Relative) });
        var owner = new Window(); new WindowInteropHelper(owner).EnsureHandle(); int seconds = 0; bool joint = false;
        var dialog = SettingsDialogs.CreateSnoozeDialog(owner, "Fixture display", OLED_Sleeper.Features.UserSettings.Models.MonitorSettingsScope.Rest, 900, true, (v, both) => { seconds = v; joint = both; });
        try
        {
            var value = dialog.Body.Children.OfType<TextBox>().Single(); var unit = dialog.Body.Children.OfType<ComboBox>().Single();
            var both = dialog.Body.Children.OfType<CheckBox>().Single(); var start = dialog.Actions.Children.OfType<Button>().Last();
            Assert.NotEqual(true, both.IsChecked); Assert.True(both.IsEnabled);
            value.Text = "0"; Assert.False(start.IsEnabled); value.Text = "1"; unit.SelectedValue = OLED_Sleeper.UI.Models.TimeUnit.Hours; Assert.True(start.IsEnabled);
            value.Text = "25"; Assert.False(start.IsEnabled); value.Text = "2"; both.IsChecked = true;
            var content = (FrameworkElement)dialog.Content; var size = new Size(400, 360);
            string language = UiText.Current.Language;
            try
            {
                foreach (string theme in new[] { "forest", "fog" })
                    foreach (string lang in new[] { "zh-CN", "en" })
                    {
                        UiText.Current.SetLanguage(lang);
                        var palette = OLED_Sleeper.UI.Themes.UiTheme.Palette(theme);
                        foreach (var key in palette.Keys) dialog.Resources[key] = palette[key];
                        await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
                        content.Measure(size); content.Arrange(new Rect(size)); content.UpdateLayout();
                        Render(content, size, $"v5-snooze-rest-{theme}-{lang}");
                        var bounds = start.TransformToAncestor(content).TransformBounds(new Rect(start.RenderSize));
                        Assert.True(bounds.Bottom <= size.Height && bounds.Right <= size.Width);
                    }
            }
            finally { UiText.Current.SetLanguage(language); }
            start.RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); Assert.Equal(7200, seconds); Assert.True(joint);
        }
        finally { dialog.Close(); owner.Close(); }
        owner = new Window(); new WindowInteropHelper(owner).EnsureHandle(); dialog = SettingsDialogs.CreateSnoozeDialog(owner, "Fixture display", OLED_Sleeper.Features.UserSettings.Models.MonitorSettingsScope.Privacy, 900, false, (_, _) => { });
        Assert.False(dialog.Body.Children.OfType<CheckBox>().Single().IsEnabled); dialog.Close(); owner.Close();
    }

    [WpfFact]
    public async Task LayoutRendersInBothLanguages_AndCurtainContainsOnlyReleaseButton()
    {
        if (Application.Current == null) new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
        Application.Current!.Resources.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri("/ScreenKeeper;component/UI/Themes/Calm.xaml", UriKind.Relative) });
        var dispatcher = new Mock<IDispatcher>(); dispatcher.SetupGet(d => d.IsOnUiThread).Returns(true);
        var root = new Mock<IStorageRoot>(); root.SetupGet(r => r.DirectoryPath).Returns(Path.GetTempPath());
        var desktop = new Mock<IScreenDesktopService>();
        var apps = new Mock<IApplicationWindowAccess>();
        apps.Setup(a => a.Enumerate(true)).Returns([new("fixture", 1, @"C:\Apps\Calendar.exe", "Fixture", "Calendar — test window", new Rect(0, 0, 100, 100), false, 0)]);
        var theme = new OLED_Sleeper.UI.Themes.UiTheme();
        var snooze = new OLED_Sleeper.Features.MonitorIdleDetection.Services.ScreenSnoozeService();
        var privacyRuntime = new Mock<IMonitorPrivacyService>();
        privacyRuntime.Setup(p => p.IsConfigured(It.IsAny<string>())).Returns(true);
        privacyRuntime.Setup(p => p.GetSnapshot(It.IsAny<string>())).Returns(new PrivacyRuntimeSnapshot(PrivacyRuntimeState.Quiet, 0, "使用时不打扰：前台窗口在本屏"));
        var vm = new MainViewModel(Mock.Of<IWorkspaceService>(), Mock.Of<IMonitorSettingsSaveService>(), dispatcher.Object, privacy: privacyRuntime.Object, images: new PrivacyImageStore(root.Object), applicationWindows: apps.Object, applications: Mock.Of<IScreenApplicationService>(), desktop: desktop.Object, theme: theme, snooze: snooze);
        for (int i = 1; i <= 3; i++)
        {
            var monitor = new MonitorLayoutViewModel(new MonitorInfo { HardwareId = "fixture-" + i, DisplayNumber = i, ModelName = i == 1 ? "DELL SE2723DS" : "Q27P10", HasSharedModelName = i > 1, IsPrimary = i == 1, Bounds = new Rect(0, 0, 1920, 1080), Capabilities = new DdcCiCapabilities(true, 100) }, 0.1, new Rect(0, 0, 1920, 1080), 0, 0);
            monitor.Configuration.IsManaged = true;
            monitor.Configuration.Behavior = OLED_Sleeper.Features.MonitorBehavior.Models.MonitorBehaviorType.Blackout;
            monitor.Configuration.PrivacyMode = PrivacyMode.Application;
            monitor.Configuration.SetApplication(@"C:\Apps\Calendar.exe", "Fixture" + i);
            monitor.Configuration.QuietWhileInUse = true;
            monitor.Configuration.MarkAsSaved();
            vm.Monitors.Add(monitor);
        }
        snooze.Configure(vm.Monitors.Select(m => m.Configuration.ToSettings()).ToArray(), vm.Monitors.Select(m => new MonitorInfo { HardwareId = m.HardwareId, DisplayNumber = m.DisplayNumber }).ToArray());
        vm.SelectedMonitor = vm.Monitors[0];
        var window = new MainWindow { DataContext = vm };
        var content = (FrameworkElement)window.Content;
        try
        {
            foreach (var paletteName in new[] { "forest", "fog" })
                foreach (var language in new[] { "zh-CN", "en" })
                {
                    UiText.Current.SetLanguage(language);
                    theme.SetTheme(paletteName);
                    var palette = OLED_Sleeper.UI.Themes.UiTheme.Palette(paletteName);
                    foreach (var key in palette.Keys) window.Resources[key] = palette[key];
                    foreach (var size in new[] { new Size(1000, 736), new Size(700, 500) })
                    {
                        await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
                        vm.SetCompactLayout(size.Width < 900);
                        content.Width = size.Width; content.Height = size.Height;
                        content.Measure(size); content.Arrange(new Rect(size)); content.UpdateLayout();
                        var picker = Descendants<ComboBox>(content).Single(c => c.ItemsSource == vm.Monitors);
                        Assert.Equal(size.Width < 900 ? Visibility.Visible : Visibility.Collapsed, picker.Visibility);
                        if (picker.Visibility == Visibility.Visible) Assert.Contains(Descendants<TextBlock>(picker), t => t.Text == vm.SelectedMonitor.SelectionLabel);
                        Assert.Empty(Descendants<TabControl>(Descendants<MonitorConfigurationView>(content).Single()));
                        foreach (var index in new[] { 0, 1 })
                        {
                            vm.CurrentModule = index == 0 ? "Displays" : "Privacy"; content.UpdateLayout();
                            Render(content, size, $"{paletteName}-{language}-{size.Width}-{index}");
                            if (index == 0)
                            {
                                var rest = Descendants<MonitorConfigurationView>(content).Single();
                                var restScroll = (ScrollViewer)rest.FindName("RestScroll");
                                restScroll.ScrollToBottom(); content.UpdateLayout();
                                Render(content, size, $"screen-apply-rest-{paletteName}-{language}-{size.Width}");
                                vm.SelectedMonitor!.Configuration.RestEnabled = false; content.UpdateLayout();
                                Render(content, size, $"screen-apply-rest-off-{paletteName}-{language}-{size.Width}");
                                vm.SelectedMonitor.Configuration.RestEnabled = true;
                                restScroll.ScrollToTop(); content.UpdateLayout();
                            }
                            if (index == 1)
                            {
                                var privacy = Descendants<PrivacySettingsView>(content).Single();
                                privacy.RaiseEvent(new RoutedEventArgs(FrameworkElement.LoadedEvent));
                                var config = vm.SelectedMonitor!.Configuration;
                                foreach (var mode in new[] { PrivacyMode.Curtain, PrivacyMode.Desktop, PrivacyMode.Application, PrivacyMode.None })
                                {
                                    config.PrivacyMode = mode; content.UpdateLayout();
                                    Assert.Equal(mode == PrivacyMode.Curtain ? Visibility.Visible : Visibility.Collapsed, ((FrameworkElement)privacy.FindName("CurtainSection")).Visibility);
                                    Assert.Equal(mode == PrivacyMode.Desktop ? Visibility.Visible : Visibility.Collapsed, ((FrameworkElement)privacy.FindName("DesktopSection")).Visibility);
                                    Assert.Equal(mode == PrivacyMode.Application ? Visibility.Visible : Visibility.Collapsed, ((FrameworkElement)privacy.FindName("ApplicationSection")).Visibility);
                                    Assert.Equal(mode == PrivacyMode.None ? Visibility.Collapsed : Visibility.Visible, ((FrameworkElement)privacy.FindName("PrivacyContent")).Visibility);
                                    Assert.DoesNotContain(Descendants<RadioButton>(privacy), r => Equals(r.Content, UiText.Get("关闭")) || Equals(r.Content, UiText.Get("隐私幕（推荐）")));
                                    Render(content, size, $"v4-{paletteName}-{language}-{size.Width}-{mode}");
                                }
                                config.PrivacyMode = PrivacyMode.Curtain; config.QuietWhileInUse = true;
                                vm.CurrentModule = "Displays"; vm.CurrentModule = "Privacy";
                                await Dispatcher.Yield(DispatcherPriority.ApplicationIdle); content.UpdateLayout();
                                var scroll = (ScrollViewer)privacy.FindName("PrivacyScroll");
                                scroll.ScrollToTop(); content.UpdateLayout();
                                var runtimeCard = (Border)privacy.FindName("RuntimeRow");
                                double runtimeY = runtimeCard.TransformToAncestor(content).Transform(new Point()).Y;
                                Assert.DoesNotContain(Descendants<RadioButton>(privacy), r => r.GroupName == "Appearance");
                                Assert.Same(privacy.FindResource("ManagementSwitch"), ((CheckBox)privacy.FindName("QuietSwitch")).Style);
                                Render(content, size, $"privacy-v5-final-{paletteName}-{language}-{size.Width}");
                                var quietSwitch = Descendants<CheckBox>(privacy).Single(c => Equals(c.Content, UiText.Get("使用时不打扰")));
                                quietSwitch.IsChecked = false; content.UpdateLayout(); Assert.False(config.QuietWhileInUse);
                                quietSwitch.IsChecked = true; content.UpdateLayout(); Assert.True(config.QuietOnMousePosition);
                                var inputChoice = Descendants<CheckBox>(privacy).Single(c => Equals(c.Content, UiText.Get("电脑有键盘或鼠标输入")));
                                inputChoice.BringIntoView(); content.UpdateLayout();
                                Assert.Equal(Visibility.Visible, ((FrameworkElement)privacy.FindName("PrivacyContent")).Visibility);
                                Assert.True(inputChoice.ActualHeight > 0);
                                Render(content, size, $"v5-quiet-{paletteName}-{language}-{size.Width}");
                                ((FrameworkElement)privacy.FindName("CurtainSection")).BringIntoView(); content.UpdateLayout();
                                Assert.Equal(runtimeY, runtimeCard.TransformToAncestor(content).Transform(new Point()).Y);
                                Assert.True(scroll.ExtentWidth <= scroll.ViewportWidth + 1);
                                Render(content, size, $"privacy-v5-appearance-{paletteName}-{language}-{size.Width}");
                                config.PrivacyMode = PrivacyMode.Application;
                                config.PauseValueText = "20";
                                content.UpdateLayout();
                                ((FrameworkElement)privacy.FindName("ApplicationOptionsSection")).BringIntoView(); content.UpdateLayout();
                                Render(content, size, $"{paletteName}-{language}-{size.Width}-app");
                                scroll.ScrollToBottom(); content.UpdateLayout();
                                Render(content, size, $"application-options-{paletteName}-{language}-{size.Width}");
                                scroll.ScrollToTop(); content.Measure(size); content.Arrange(new Rect(size)); content.UpdateLayout();
                            }
                            var save = Descendants<Button>(content).Single(b => b.Command == vm.SaveSettingsCommand);
                            var rect = save.TransformToAncestor(content).TransformBounds(new Rect(save.RenderSize));
                            Assert.True(rect.Bottom <= size.Height && rect.Right <= size.Width, $"{language} {size} tab {index}: footer {rect}");
                            Assert.True(save.ActualHeight >= 30);
                        }
                    }
                }
            if (Environment.GetEnvironmentVariable("SCREENKEEPER_NATIVE_QA") == "1")
            {
                content.ClearValue(FrameworkElement.WidthProperty); content.ClearValue(FrameworkElement.HeightProperty);
                window.ShowActivated = false; window.ShowInTaskbar = false;
                window.Show(); window.UpdateLayout();
                var handle = new WindowInteropHelper(window).Handle;
                var point = window.PointToScreen(new Point(350, 20));
                var coordinates = (nint)(((int)point.Y << 16) | ((int)point.X & 0xffff));
                Assert.Equal((nint)2, SendMessage(handle, 0x84, 0, coordinates)); // HTCAPTION: native dragging.
                SystemCommands.MaximizeWindow(window);
                await Dispatcher.Yield(DispatcherPriority.Background);
                Assert.Equal(WindowState.Maximized, window.WindowState);
                Assert.Equal("\uE923", ((Button)window.FindName("MaximizeButton")).Content);
                SystemCommands.RestoreWindow(window);
                await Dispatcher.Yield(DispatcherPriority.Background);
                Assert.Equal(WindowState.Normal, window.WindowState);
                Assert.Equal("\uE922", ((Button)window.FindName("MaximizeButton")).Content);
                foreach (var show in new Action[] { () => SettingsDialogs.Copy(window, vm, vm.SelectedMonitor.Configuration), () => SettingsDialogs.Pause(window, new(), _ => { }), () => SettingsDialogs.General(window, vm), () => SettingsDialogs.Application(window, vm, vm.SelectedMonitor.Configuration), () => SettingsDialogs.Images(window, vm, vm.SelectedMonitor.Configuration), () => SettingsDialogs.TestApplication(window, vm, vm.SelectedMonitor.Configuration), () => SettingsDialogs.TestDesktop(window, vm, vm.SelectedMonitor.Configuration) })
                {
                    _ = Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle, new Action(() =>
                    {
                        var dialog = Application.Current.Windows.OfType<SettingsDialog>().Single();
                        dialog.UpdateLayout();
                        Assert.DoesNotContain(Descendants<TextBlock>(dialog), t => t.Text.Contains("OLED Sleeper", StringComparison.Ordinal));
                        Render(dialog, new Size(dialog.ActualWidth, dialog.ActualHeight), "dialog-" + dialog.Title);
                        dialog.Close();
                    }));
                    show();
                }
                // A cancelled window operation can finish asynchronously. Do not let a second
                // test start while the first one is still unwinding after curtain release.
                var completion = new TaskCompletionSource<bool>();
                CancellationToken observed = default;
                desktop.Setup(d => d.ShowDesktopAsync(It.IsAny<Rect>(), It.IsAny<CancellationToken>()))
                    .Callback<Rect, CancellationToken>((_, token) => observed = token).Returns(completion.Task);
                Exception? failure = null;
                _ = Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle, new Action(async () =>
                {
                    var dialog = Application.Current.Windows.OfType<SettingsDialog>().Single();
                    try
                    {
                        var start = Descendants<Button>(dialog).Single(b => Equals(b.Content, UiText.Get("开始测试")));
                        start.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                        await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
                        Assert.False(start.IsEnabled);
                        Application.Current.Windows.OfType<PrivacyCurtainWindow>().Single().Dismiss();
                        Assert.True(observed.IsCancellationRequested);
                        Assert.False(start.IsEnabled);
                        completion.SetResult(false);
                        await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
                        Assert.True(start.IsEnabled);
                    }
                    catch (Exception error) { failure = error; }
                    finally
                    {
                        completion.TrySetResult(false);
                        foreach (var cover in Application.Current.Windows.OfType<PrivacyCurtainWindow>().ToArray()) cover.Dismiss();
                        dialog.Close();
                    }
                }));
                SettingsDialogs.TestDesktop(window, vm, new MonitorConfigurationViewModel(new MonitorInfo
                {
                    HardwareId = "test-cancellation",
                    Bounds = new Rect(0, 0, 160, 90)
                }));
                Assert.Null(failure);
            }
            var curtain = new PrivacyCurtainWindow();
            var surface = (FrameworkElement)curtain.Content;
            surface.Measure(new Size(800, 450)); surface.Arrange(new Rect(0, 0, 800, 450)); surface.UpdateLayout();
            Assert.Single(Descendants<Button>(surface));
            Assert.DoesNotContain(Descendants<TextBlock>(surface), t => t.Text != UiText.Get("进入此屏幕"));
            Assert.Equal((byte)255, ((SolidColorBrush)curtain.Background).Color.A);
            curtain.Dismiss();
        }
        finally { window.Close(); UiText.Current.SetLanguage("zh-CN"); }
    }
    [DllImport("user32.dll")] private static extern nint SendMessage(nint window, uint message, nint wParam, nint lParam);
    private static void Render(FrameworkElement content, Size size, string name)
    {
        var output = Environment.GetEnvironmentVariable("SCREENKEEPER_RENDER_QA");
        if (string.IsNullOrWhiteSpace(output)) return;
        Directory.CreateDirectory(output);
        var bitmap = new RenderTargetBitmap((int)size.Width, (int)size.Height, 96, 96, PixelFormats.Pbgra32);
        if (name.StartsWith("v5-snooze", StringComparison.Ordinal) || name.StartsWith("privacy-appearance-dialog", StringComparison.Ordinal))
        {
            var background = new DrawingVisual();
            using (var drawing = background.RenderOpen()) drawing.DrawRectangle((Brush)content.FindResource("SecondaryBackgroundColor"), null, new Rect(size));
            bitmap.Render(background);
        }
        bitmap.Render(content);
        var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using var stream = File.Create(Path.Combine(output, name + ".png")); encoder.Save(stream);
    }
    private static IEnumerable<T> Descendants<T>(DependencyObject root) where T : DependencyObject
    {
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
        {
            var child = VisualTreeHelper.GetChild(root, i);
            if (child is T typed) yield return typed;
            foreach (var descendant in Descendants<T>(child)) yield return descendant;
        }
    }
    [WpfFact]
    public void ImportedImagesSurviveSourceRemoval_AndRejectTraversalOrCorruption()
    {
        var directory = Path.Combine(Path.GetTempPath(), "screenkeeper-images-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        var root = new Mock<IStorageRoot>(); root.SetupGet(r => r.DirectoryPath).Returns(directory);
        var store = new PrivacyImageStore(root.Object);
        var source = Path.Combine(directory, "source.png");
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(BitmapSource.Create(1, 1, 96, 96, PixelFormats.Bgra32, null, new byte[] { 0, 100, 0, 255 }, 4)));
        using (var stream = File.Create(source)) encoder.Save(stream);
        var asset = store.Import(source); File.Delete(source);
        Assert.Equal(1, store.Load(asset).PixelWidth);
        Assert.Throws<InvalidDataException>(() => store.Load("../source.png"));
        File.WriteAllText(source, "not an image");
        Assert.ThrowsAny<Exception>(() => store.Import(source));
        Assert.Equal(1, store.Load(asset).PixelHeight);
        // This test owns the unique temporary directory and no application/user data.
        Directory.Delete(directory, true);
    }
}
