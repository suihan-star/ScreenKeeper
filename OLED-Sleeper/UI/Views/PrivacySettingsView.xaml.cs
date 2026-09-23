using OLED_Sleeper.UI.Helpers;
using OLED_Sleeper.UI.ViewModels;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace OLED_Sleeper.UI.Views;

public partial class PrivacySettingsView : UserControl
{
    private MonitorConfigurationViewModel? _configuration;
    private (bool Images, bool BuiltIn, string? First, bool Contain)? _thumbnailKey;
    public PrivacySettingsView()
    {
        InitializeComponent();
        Loaded += (_, _) => Observe(DataContext as MonitorConfigurationViewModel);
        Unloaded += (_, _) => Observe(null);
        DataContextChanged += (_, _) => { if (IsLoaded) Observe(DataContext as MonitorConfigurationViewModel); };
    }
    private void Observe(MonitorConfigurationViewModel? configuration)
    {
        if (_configuration != null) _configuration.PropertyChanged -= ConfigurationChanged;
        _configuration = configuration;
        _thumbnailKey = null;
        if (_configuration != null) _configuration.PropertyChanged += ConfigurationChanged;
        RefreshThumbnail();
    }
    private void ConfigurationChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(MonitorConfigurationViewModel.Images) or nameof(MonitorConfigurationViewModel.UseImages) or nameof(MonitorConfigurationViewModel.UseBuiltInBackground) or nameof(MonitorConfigurationViewModel.ContainImages)) RefreshThumbnail();
    }
    private void RefreshThumbnail()
    {
        var key = (_configuration?.UseImages == true, _configuration?.UseBuiltInBackground == true, _configuration?.Images.FirstOrDefault(), _configuration?.ContainImages == true);
        if (_thumbnailKey == key) return;
        _thumbnailKey = key;
        CurtainThumbnail.Source = null;
        CurtainThumbnailShade.Visibility = Visibility.Hidden;
        if (_configuration?.UseBuiltInBackground == true)
        {
            CurtainThumbnail.Stretch = Stretch.UniformToFill;
            CurtainThumbnail.Source = BuiltInBackground.Forest;
            CurtainThumbnailShade.Visibility = CurtainThumbnail.Source != null ? Visibility.Visible : Visibility.Hidden;
            return;
        }
        if (_configuration is not { UseImages: true } config || config.Images.Length == 0) return;
        CurtainThumbnail.Stretch = config.ContainImages ? Stretch.Uniform : Stretch.UniformToFill;
        try { CurtainThumbnail.Source = (Window.GetWindow(this)?.DataContext as MainViewModel)?.Images?.Load(config.Images[0]); }
        catch { /* Keep the same opaque solid fallback as the privacy curtain. */ }
    }
    private void Run(Action<Window, MainViewModel, MonitorConfigurationViewModel> action)
    {
        if (Window.GetWindow(this) is { DataContext: MainViewModel main } owner && DataContext is MonitorConfigurationViewModel config) action(owner, main, config);
    }
    private void Copy_Click(object sender, RoutedEventArgs e) => Run(SettingsDialogs.Copy);
    private void Images_Click(object sender, RoutedEventArgs e) => Run(SettingsDialogs.Images);
    private void Preview_Click(object sender, RoutedEventArgs e) => Run(SettingsDialogs.Preview);
    private void Application_Click(object sender, RoutedEventArgs e) => Run(SettingsDialogs.Application);
    private void TestApplication_Click(object sender, RoutedEventArgs e) => Run(SettingsDialogs.TestApplication);
    private void TestDesktop_Click(object sender, RoutedEventArgs e) => Run(SettingsDialogs.TestDesktop);
    private void Help_Click(object sender, RoutedEventArgs e) => Run((owner, _, _) =>
    {
        var dialog = new SettingsDialog(owner, "了解隐私保护规则");
        dialog.Body.Children.Add(SettingsDialog.Text("本屏实际操作会重新计时；使用时不打扰按你勾选的任一条件生效。"));
        dialog.Body.Children.Add(SettingsDialog.Text("唤亮不会解除隐私幕；回到桌面不会自动还原窗口，失败时保留隐私幕。"));
        dialog.Action("完成", () => dialog.Close(), true); dialog.ShowDialog();
    });
}
