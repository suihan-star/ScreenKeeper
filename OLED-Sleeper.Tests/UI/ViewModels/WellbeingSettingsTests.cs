using Moq;
using OLED_Sleeper.Features.ColorTemperature;
using OLED_Sleeper.Features.HealthReminder;
using OLED_Sleeper.Infrastructure.Runtime.Interfaces;
using OLED_Sleeper.Storage;
using OLED_Sleeper.Tests.TestDoubles;
using OLED_Sleeper.UI.Localization;
using OLED_Sleeper.UI.Services.Interfaces;
using OLED_Sleeper.UI.ViewModels;
using OLED_Sleeper.UI.Views;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace OLED_Sleeper.Tests.UI.ViewModels;

[Collection("UI language")]
public sealed class WellbeingSettingsTests
{
    [WpfFact]
    public async Task FailedThemePersistenceRestoresBothSelectorsWithoutChangingBusinessDraft()
    {
        if (Application.Current == null) new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
        Application.Current!.Resources.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri("/ScreenKeeper;component/UI/Themes/Calm.xaml", UriKind.Relative) });
        var theme = new OLED_Sleeper.UI.Themes.UiTheme();
        var preferences = new OLED_Sleeper.UI.Services.UiPreferencesService(Mock.Of<OLED_Sleeper.Storage.Interfaces.IAppDataFileStore>(), UiText.Current, theme);
        var vm = new MainViewModel(Mock.Of<IWorkspaceService>(), Mock.Of<IMonitorSettingsSaveService>(), Mock.Of<IDispatcher>(), preferences, theme: theme);
        vm.IsDirty = true;
        var window = new MainWindow { DataContext = vm };
        var picker = new ThemePickerView { DataContext = vm };
        try
        {
            var content = (FrameworkElement)window.Content;
            content.Measure(new Size(700, 500)); content.Arrange(new Rect(0, 0, 700, 500)); content.UpdateLayout();
            picker.Measure(new Size(500, 240)); picker.Arrange(new Rect(0, 0, 500, 240)); picker.UpdateLayout();
            var quick = (ComboBox)window.FindName("ThemeSelector");
            quick.SelectedValue = "fog";
            await Dispatcher.Yield(DispatcherPriority.Background);
            Assert.Equal("forest", vm.Theme);
            Assert.Equal("forest", quick.SelectedValue);
            var cards = Descendants<ListBox>(picker).Single();
            cards.SelectedValue = "fog";
            await Dispatcher.Yield(DispatcherPriority.Background);
            Assert.Equal("forest", cards.SelectedValue);
            Assert.True(vm.IsDirty);
        }
        finally { window.Close(); }
    }

    [WpfFact]
    public async Task SetupGuideRequiresSavedPolicyOffAndKeepsRecoverySeparateFromValidation()
    {
        if (Application.Current == null) new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
        Application.Current!.Resources.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri("/ScreenKeeper;component/UI/Themes/Calm.xaml", UriKind.Relative) });
        using var f = new Fixture(new() { Enabled = true, ParticipatingScreens = new() { ["fixture"] = true } });
        var scope = new ColorDisplayScope("fixture", "scope", "fixture", "Example display", "topology", "mode");
        f.Color.UpdateCapabilities([new(scope, ColorCapabilityState.Unverified, "尚未完成此设备的实机调色与恢复验证。", false, false, true)], [new("fixture", ColorControlState.Conflict, "需处理恢复记录")], "");
        var owner = new MainWindow { DataContext = f.Main };
        new System.Windows.Interop.WindowInteropHelper(owner).EnsureHandle();
        var dialog = SettingsDialogs.CreateColorSetupDialog(owner, f.Color, f.Color.Screens[0], () => { });
        try
        {
            Assert.Contains(dialog.Body.Children.OfType<StackPanel>().Single().Children.OfType<Button>(), b => Equals(b.Content, UiText.Get("关闭这些屏幕并继续验证")));
            f.Color.Screens[0].Selected = false;
            Assert.True(f.Color.SavedEnabled);
            Assert.DoesNotContain(dialog.Body.Children.OfType<StackPanel>().Single().Children.OfType<Button>(), b => b.Command == f.Color.Screens[0].RecheckOriginalCommand);
            f.Fs.WritesFail = true; Assert.False(await f.Color.DisableForValidationAsync()); Assert.True(f.Color.SavedEnabled);
            f.Fs.WritesFail = false; Assert.True(await f.Color.DisableForValidationAsync());
            Assert.Contains(dialog.Body.Children.OfType<StackPanel>().Single().Children.OfType<Button>(), b => b.Command == f.Color.Screens[0].RecheckOriginalCommand);
            Assert.DoesNotContain(dialog.Body.Children.OfType<StackPanel>().Single().Children.OfType<Button>(), b => Equals(b.Content, UiText.Get("进入 15 秒验证")));
            Assert.False(f.Color.Screens[0].Selected);
        }
        finally { dialog.Close(); owner.Close(); }
    }

    [WpfFact]
    public void ModuleSaveFailureAndDiscardKeepOtherDrafts()
    {
        using var fixture = new Fixture();
        var vm = fixture.Main;
        fixture.Health.WorkMinutes = "25"; fixture.Color.NightKelvin = "4800";
        vm.CurrentModule = "Health";
        Assert.True(vm.IsDirty); Assert.Contains(UiText.Get("色温调节"), vm.DirtyModules);
        fixture.Fs.WritesFail = true;
        Assert.False(fixture.Health.TrySave()); Assert.True(fixture.Health.IsDirty); Assert.True(fixture.Color.IsDirty);
        fixture.Fs.WritesFail = false;
        Assert.True(fixture.Health.TrySave()); Assert.False(fixture.Health.IsDirty); Assert.True(vm.IsDirty);
        vm.CurrentModule = "Color"; vm.DiscardChangesCommand.Execute(null);
        Assert.Equal("4500", fixture.Color.NightKelvin); Assert.Equal("25", fixture.Health.WorkMinutes);
        Assert.False(vm.IsDirty);
    }
    [WpfFact]
    public void InvalidDurationRemainsDraftAndCannotSave()
    {
        using var fixture = new Fixture(); fixture.Main.CurrentModule = "Health";
        fixture.Health.WorkMinutes = "2147483648";
        Assert.True(fixture.Main.IsDirty); Assert.NotNull(fixture.Main.ConfigurationError); Assert.False(fixture.Main.SaveSettingsCommand.CanExecute(null));
        fixture.Main.DiscardChangesCommand.Execute(null); Assert.Equal("50", fixture.Health.WorkMinutes);
    }
    [WpfFact]
    public void AppearanceDraftParticipatesInSaveFailureDiscardAndIndependentCopy()
    {
        using var fixture = new Fixture();
        fixture.Health.FadeSeconds = 12; fixture.Health.ReminderStyle = "EdgeGlow"; fixture.Health.MotionEnabled = false;
        Assert.True(fixture.Health.IsDirty);
        fixture.Fs.WritesFail = true;
        Assert.False(fixture.Health.TrySave()); Assert.True(fixture.Health.IsDirty);
        fixture.Fs.WritesFail = false; fixture.Health.Discard();
        Assert.Equal(8, fixture.Health.FadeSeconds); Assert.Equal("Scenic", fixture.Health.ReminderStyle); Assert.True(fixture.Health.MotionEnabled);
        fixture.Health.OverlayPercent = 100; Assert.True(fixture.Health.TrySave());
        fixture.Health.OverlayPercent = 20; fixture.Health.Discard(); Assert.Equal(100, fixture.Health.OverlayPercent);
        var settings = new HealthReminderSettings(); var copy = settings.Copy(); copy.Appearance.FadeSeconds = 14;
        Assert.Equal(8, settings.Appearance.FadeSeconds);
    }
    [WpfFact]
    public void ColorOfflinePreferencesSurviveDiscoveryAndDiscard()
    {
        using var fixture = new Fixture(new() { Enabled = true, ParticipatingScreens = new() { ["offline"] = true } });
        fixture.Color.UpdateCapabilities([], [], "正在检测色温能力…");
        Assert.Single(fixture.Color.Screens); Assert.True(fixture.Color.Screens[0].Selected); Assert.False(fixture.Color.Screens[0].IsSupported);
        fixture.Color.Screens[0].Selected = false; Assert.True(fixture.Color.IsDirty); fixture.Color.Discard(); Assert.True(fixture.Color.Screens[0].Selected);
    }
    [WpfFact]
    public void ThemesAndPagesRenderInBothLanguagesAndSizes()
    {
        if (Application.Current == null) new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
        Application.Current!.Resources.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri("/ScreenKeeper;component/UI/Themes/Calm.xaml", UriKind.Relative) });
        using var fixture = new Fixture();
        var window = new MainWindow { DataContext = fixture.Main };
        fixture.ThemeWindows.Add(window);
        var content = (FrameworkElement)window.Content;
        try
        {
            foreach (var theme in new[] { "forest", "fog" })
                foreach (var language in new[] { "zh-CN", "en" })
                    foreach (var size in new[] { new Size(1000, 736), new Size(700, 500) })
                        foreach (var module in new[] { "Health", "Color" })
                        {
                            fixture.Main.Theme = theme; UiText.Current.SetLanguage(language); fixture.Main.CurrentModule = module;
                            foreach (var pageIndex in new[] { 0, 1 })
                            {
                                if (module == "Health") fixture.Health.SelectedPage = pageIndex;
                                else fixture.Color.SelectedPage = pageIndex;
                                var scope = new ColorDisplayScope("fixture", "scope", "fixture", "Studio Display — fixture", "topology", "mode");
                                fixture.Color.UpdateCapabilities([new(scope, ColorCapabilityState.Supported, "fixture")], [], ""); fixture.Color.Enabled = true;
                                fixture.Health.WorkMinutes = "25"; fixture.Color.NightKelvin = "4800";
                                content.Width = size.Width; content.Height = size.Height; content.Measure(size); content.Arrange(new Rect(size)); content.UpdateLayout();
                                var save = Descendants<Button>(content).Single(b => b.Command == fixture.Main.SaveSettingsCommand);
                                var rect = save.TransformToAncestor(content).TransformBounds(new Rect(save.RenderSize));
                                Assert.True(rect.Right <= size.Width && rect.Bottom <= size.Height);
                                var page = module == "Health" ? (FrameworkElement)Descendants<HealthSettingsView>(content).Single() : Descendants<ColorSettingsView>(content).Single();
                                var scroll = Descendants<ScrollViewer>(page).First();
                                Assert.True(scroll.ExtentWidth <= scroll.ViewportWidth + 1, $"{module}/{pageIndex}/{language}: horizontal overflow");
                                scroll.ScrollToTop(); content.UpdateLayout();
                                var output = Environment.GetEnvironmentVariable("SCREENKEEPER_RENDER_QA");
                                if (!string.IsNullOrWhiteSpace(output))
                                {
                                    Directory.CreateDirectory(output);
                                    var bitmap = new RenderTargetBitmap((int)size.Width, (int)size.Height, 96, 96, PixelFormats.Pbgra32); bitmap.Render(content);
                                    var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(bitmap));
                                    using var stream = File.Create(Path.Combine(output, $"v4-{theme}-{language}-{size.Width}-{module}-{pageIndex}.png")); encoder.Save(stream);
                                }
                                if (module == "Health" && pageIndex == 1)
                                {
                                    scroll.ScrollToBottom(); content.UpdateLayout();
                                    var preview = Descendants<Button>(page).Single(b => b.Command == fixture.Health.PreviewCommand);
                                    Assert.True(preview.ActualHeight > 0);
                                    fixture.Health.PreviewRestStage = true;
                                    Assert.False(fixture.Health.StageUsesOverlay);
                                    Assert.Equal("25", fixture.Health.WorkMinutes);
                                    fixture.Health.PreviewRestStage = false;
                                }
                                Assert.True(fixture.Health.IsDirty); Assert.True(fixture.Color.IsDirty);
                            }
                        }
        }
        finally { window.Close(); UiText.Current.SetLanguage("zh-CN"); }
    }

    [WpfFact]
    public void ThemeChangesUpdateExistingDialogsAndKeepProtectionColorsAndBusinessDrafts()
    {
        if (Application.Current == null) new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
        Application.Current!.Resources.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri("/ScreenKeeper;component/UI/Themes/Calm.xaml", UriKind.Relative) });
        using var fixture = new Fixture();
        var window = new MainWindow { DataContext = fixture.Main };
        new System.Windows.Interop.WindowInteropHelper(window).EnsureHandle();
        var dialog = new SettingsDialog(window, "通用设置");
        var picker = new ThemePickerView { DataContext = fixture.Main };
        dialog.Body.Children.Add(picker);
        fixture.ThemeWindows.Add(window); fixture.ThemeWindows.Add(dialog);
        var reminder = new HealthReminderWindow(HealthCardKind.Preview, new HealthScreen("fixture", 0, 0, 1920, 1080), TimeSpan.FromSeconds(20));
        try
        {
            fixture.Health.WorkMinutes = "25";
            fixture.Color.DayKelvin = "5100";
            fixture.Main.Theme = "fog";
            var content = (FrameworkElement)window.Content;
            content.Measure(new Size(1000, 736)); content.Arrange(new Rect(0, 0, 1000, 736)); content.UpdateLayout();
            picker.Measure(new Size(500, 240)); picker.Arrange(new Rect(0, 0, 500, 240)); picker.UpdateLayout();
            Assert.Equal("fog", ((ComboBox)window.FindName("ThemeSelector")).SelectedValue);
            Assert.Equal("fog", Descendants<ListBox>(picker).Single().SelectedValue);
            Assert.Equal((Color)ColorConverter.ConvertFromString("#EAF0E9"), ((SolidColorBrush)window.Background).Color);
            Assert.Equal((Color)ColorConverter.ConvertFromString("#F5F7F1"), ((SolidColorBrush)dialog.Background).Color);
            Assert.Equal((Color)ColorConverter.ConvertFromString("#E8EFE5"), ((SolidColorBrush)reminder.Foreground).Color);
            Descendants<ListBox>(picker).Single().SelectedValue = "forest";
            Assert.Equal("forest", fixture.Main.Theme);
            Assert.Equal("forest", ((ComboBox)window.FindName("ThemeSelector")).SelectedValue);
            Assert.Equal("25", fixture.Health.WorkMinutes);
            Assert.Equal("5100", fixture.Color.DayKelvin);
            Assert.True(fixture.Main.IsDirty);
        }
        finally { reminder.CloseFromService(); dialog.Close(); window.Close(); }
    }
    private static IEnumerable<T> Descendants<T>(DependencyObject node) where T : DependencyObject
    {
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(node); i++)
        {
            var child = VisualTreeHelper.GetChild(node, i); if (child is T value) yield return value;
            foreach (var descendant in Descendants<T>(child)) yield return descendant;
        }
    }
    private sealed class Fixture : IDisposable
    {
        public FakeFileSystem Fs { get; } = new();
        public HealthSettingsViewModel Health { get; }
        public ColorSettingsViewModel Color { get; }
        public MainViewModel Main { get; }
        public List<Window> ThemeWindows { get; } = [];
        private readonly HealthReminderService _health;
        public Fixture(ColorTemperatureSettings? color = null)
        {
            var root = new StorageRoot(Fs, false); var files = new AppDataFileStore(Fs, root);
            var activity = new Mock<IUserActivitySnapshot>(); activity.Setup(a => a.Read()).Returns(HealthActivitySnapshot.Unavailable);
            _health = new(Dispatcher.CurrentDispatcher, _ => false, activity.Object, presenter: Mock.Of<IHealthReminderPresenter>());
            Health = new(new(files, Fs, root, "health-reminder.json", () => new(), s => s.Validate().FirstOrDefault()), _health, new(), () => true);
            Color = new(new(files, Fs, root, "color-temperature.json", () => new(), s => s.Validate(), maxSchema: 2), color ?? new(), _ => { }, () => Task.CompletedTask, _ => Task.CompletedTask, _ => Task.CompletedTask, (_, _) => Task.CompletedTask, validate: _ => Task.CompletedTask);
            if (color == null) Color.UpdateCapabilities([new(new("fixture", "scope", "fixture", "Studio display", "topology", "mode"), ColorCapabilityState.Supported, "fixture")], [], "");
            Main = new(Mock.Of<IWorkspaceService>(), Mock.Of<IMonitorSettingsSaveService>(), Mock.Of<IDispatcher>(), healthSettings: Health, colorSettings: Color, theme: new OLED_Sleeper.UI.Themes.UiTheme(theme =>
            {
                foreach (var window in ThemeWindows)
                {
                    var palette = OLED_Sleeper.UI.Themes.UiTheme.Palette(theme);
                    foreach (var key in palette.Keys) window.Resources[key] = palette[key];
                }
            }));
        }
        public void Dispose() => _health.Dispose();
    }
}
