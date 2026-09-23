using OLED_Sleeper.UI.ViewModels;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace OLED_Sleeper.UI.Views;
public partial class ColorSettingsView : UserControl
{
    private Window? _owner;
    private ColorSettingsViewModel? Model => DataContext as ColorSettingsViewModel;
    public ColorSettingsView()
    {
        InitializeComponent();
        SizeChanged += (_, _) => Model?.SetCompactLayout(ActualWidth < 640);
        Loaded += (_, _) =>
        {
            if (_owner != null) _owner.Deactivated -= OwnerDeactivated;
            _owner = Window.GetWindow(this);
            if (_owner != null) _owner.Deactivated += OwnerDeactivated;
        };
        Unloaded += (_, _) =>
        {
            Model?.LeavePage();
            CompareOriginalButton.ReleaseMouseCapture();
            if (_owner != null) _owner.Deactivated -= OwnerDeactivated;
            _owner = null;
        };
    }
    private void ScreenSetup_Click(object sender, RoutedEventArgs e)
    {
        if (Model is not { } model || sender is not FrameworkElement { DataContext: ColorScreenViewModel screen } || Window.GetWindow(this) is not { } owner) return;
        model.LeavePage();
        if (model.VerifiedScreens.Contains(screen))
        {
            model.SelectedScreen = screen;
            model.SelectedPage = 0;
            return;
        }
        SettingsDialogs.ColorSetup(owner, model, screen, () =>
        {
            model.SelectedPage = 0;
            Dispatcher.BeginInvoke(new Action(() => ColorEnabledToggle.Focus()));
        });
    }
    private void OwnerDeactivated(object? sender, EventArgs e) => Model?.EndOriginalComparison();
    private void CompareDown(object sender, MouseButtonEventArgs e)
    {
        if (Model?.CanCompareOriginal != true) return;
        CompareOriginalButton.Focus(); CompareOriginalButton.CaptureMouse();
        Model.BeginOriginalComparison(); e.Handled = true;
    }
    private void CompareUp(object sender, MouseButtonEventArgs e)
    {
        Model?.EndOriginalComparison(); CompareOriginalButton.ReleaseMouseCapture(); e.Handled = true;
    }
    private void CompareCaptureLost(object sender, MouseEventArgs e) => Model?.EndOriginalComparison();
    private void CompareFocusLost(object sender, KeyboardFocusChangedEventArgs e) => Model?.EndOriginalComparison();
    private void CompareKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key is Key.Space or Key.Enter) { if (!e.IsRepeat) Model?.BeginOriginalComparison(); e.Handled = true; }
        else if (e.Key == Key.Escape) { Model?.LeavePage(); e.Handled = true; }
    }
    private void CompareKeyUp(object sender, KeyEventArgs e)
    {
        if (e.Key is Key.Space or Key.Enter) { Model?.EndOriginalComparison(); e.Handled = true; }
    }
}
