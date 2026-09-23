using Microsoft.Win32;
using OLED_Sleeper.UI.Helpers;
using Serilog;
using System.Windows.Media.Imaging;

namespace OLED_Sleeper;

public partial class MainWindow
{
    private string? _taskbarIconResource;
    private bool _taskbarIconClosed;

    private void InitializeTaskbarIcon()
    {
        RefreshTaskbarIcon();
        // Subscribe only when this window has a native lifetime, not for unshown render fixtures.
        SourceInitialized += (_, _) =>
        {
            SystemEvents.UserPreferenceChanged += OnTaskbarPreferenceChanged;
            RefreshTaskbarIcon();
        };
        Closed += (_, _) =>
        {
            _taskbarIconClosed = true;
            SystemEvents.UserPreferenceChanged -= OnTaskbarPreferenceChanged;
        };
    }

    private void OnTaskbarPreferenceChanged(object sender, UserPreferenceChangedEventArgs e)
    {
        if (_taskbarIconClosed || Dispatcher.HasShutdownStarted || Dispatcher.HasShutdownFinished) return;
        _ = Dispatcher.BeginInvoke(new Action(RefreshTaskbarIcon));
    }

    private void RefreshTaskbarIcon()
    {
        if (_taskbarIconClosed) return;
        var resource = SystemTaskbarTheme.IconResource;
        if (_taskbarIconResource == resource) return;
        try
        {
            var uri = new Uri($"pack://application:,,,/ScreenKeeper;component/Assets/{resource}", UriKind.Absolute);
            var image = BitmapFrame.Create(uri, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
            image.Freeze();
            Icon = image;
            _taskbarIconResource = resource;
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to update the window taskbar icon.");
        }
    }
}
