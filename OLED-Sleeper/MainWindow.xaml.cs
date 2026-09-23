using OLED_Sleeper.UI.Localization;
using OLED_Sleeper.UI.ViewModels;
using OLED_Sleeper.UI.Views;
using System.Windows;
using System.Windows.Automation;

namespace OLED_Sleeper;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        InitializeTaskbarIcon();
        SizeChanged += (_, _) => { if (DataContext is MainViewModel vm) vm.SetCompactLayout(ActualWidth < 900); };
        DataContextChanged += (_, _) => { if (DataContext is MainViewModel vm) vm.SetCompactLayout(ActualWidth < 900); };
        Loaded += (_, _) => { if (DataContext is MainViewModel vm && vm.Monitors.Count == 0) vm.RecalculateLayout(600, 180); };
        StateChanged += (_, _) => { RefreshChrome(); if (WindowState == WindowState.Minimized && DataContext is MainViewModel vm) vm.LeaveSettingsWindow(); };
        System.ComponentModel.PropertyChangedEventManager.AddHandler(UiText.Current, (_, _) => RefreshChrome(), string.Empty);
        RefreshChrome();
    }
    private void RefreshChrome()
    {
        var maximized = WindowState == WindowState.Maximized;
        MaximizeButton.Content = maximized ? "\uE923" : "\uE922";
        var label = UiText.Get(maximized ? "还原" : "最大化");
        MaximizeButton.ToolTip = label;
        AutomationProperties.SetName(MaximizeButton, label);
    }
    private void MinimizeButton_Click(object sender, RoutedEventArgs e) => SystemCommands.MinimizeWindow(this);
    private void MaximizeButton_Click(object sender, RoutedEventArgs e)
    {
        if (WindowState == WindowState.Maximized) SystemCommands.RestoreWindow(this);
        else SystemCommands.MaximizeWindow(this);
    }
    private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();
    private void MonitorList_Activate(object sender, System.Windows.Input.MouseButtonEventArgs e) { if (DataContext is MainViewModel vm && vm.SelectedMonitor != null) vm.CurrentModule = "Displays"; }
    private void General_Click(object sender, RoutedEventArgs e) { if (DataContext is MainViewModel vm) SettingsDialogs.General(this, vm); }
}
