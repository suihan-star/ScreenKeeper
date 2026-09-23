using OLED_Sleeper.UI.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace OLED_Sleeper.UI.Views;

public partial class MonitorConfigurationView : UserControl
{
    public MonitorConfigurationView() => InitializeComponent();
    private void Run(Action<Window, MainViewModel, MonitorConfigurationViewModel> action)
    {
        var owner = Window.GetWindow(this);
        if (owner?.DataContext is MainViewModel main && DataContext is MonitorConfigurationViewModel config) action(owner, main, config);
    }
    private void Copy_Click(object sender, RoutedEventArgs e) => Run(SettingsDialogs.Copy);
    private void Application_Click(object sender, RoutedEventArgs e) => Run(SettingsDialogs.Application);
    private void Images_Click(object sender, RoutedEventArgs e) => Run(SettingsDialogs.Images);
    private void Preview_Click(object sender, RoutedEventArgs e) => Run(SettingsDialogs.Preview);
    private void TestApplication_Click(object sender, RoutedEventArgs e) => Run(SettingsDialogs.TestApplication);
}
