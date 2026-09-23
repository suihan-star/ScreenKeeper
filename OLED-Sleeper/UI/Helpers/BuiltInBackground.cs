using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace OLED_Sleeper.UI.Helpers;

/// <summary>One immutable, embedded image shared by settings previews and both screen overlays.</summary>
public static class BuiltInBackground
{
    private static readonly Lazy<ImageSource?> ForestImage = new(LoadForest);
    public static ImageSource? Forest => ForestImage.Value;
    public static SolidColorBrush Tint { get; } = CreateTint();

    private static ImageSource? LoadForest()
    {
        try
        {
            var image = new BitmapImage();
            image.BeginInit();
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.UriSource = new Uri("pack://application:,,,/ScreenKeeper;component/Assets/forest-mist.png");
            image.EndInit();
            image.Freeze();
            return image;
        }
        catch { return null; } // Callers retain an opaque solid or vector fallback.
    }

    private static SolidColorBrush CreateTint()
    {
        var brush = new SolidColorBrush(Color.FromArgb(56, 12, 32, 26));
        brush.Freeze();
        return brush;
    }
}
