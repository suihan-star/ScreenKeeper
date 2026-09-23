using System.Collections;
using System.IO;
using System.Resources;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace OLED_Sleeper.Tests.UI.Services
{
    public class ApplicationIconTests
    {
        [WpfFact]
        public void PackagedIconsDecodeAtEverySupportedSizeWithTransparency()
        {
            var assembly = typeof(MainWindow).Assembly;
            using var resources = new ResourceReader(assembly.GetManifestResourceStream("ScreenKeeper.g.resources")!);
            var sizes = new[] { 16, 20, 24, 32, 40, 48, 64, 128, 256 };
            var checkedResources = 0;
            foreach (DictionaryEntry entry in resources)
            {
                if (entry.Key is not string key || (key != "assets/icon.ico" && key != "assets/icon-dark.ico")) continue;
                using var data = new MemoryStream();
                ((Stream)entry.Value!).CopyTo(data);
                data.Position = 0;
                var decoder = new IconBitmapDecoder(data, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
                Assert.Equal(sizes, decoder.Frames.Select(frame => frame.PixelWidth));
                foreach (var frame in decoder.Frames)
                {
                    Assert.Equal(frame.PixelWidth, frame.PixelHeight);
                    var pixels = new FormatConvertedBitmap(frame, PixelFormats.Bgra32, null, 0);
                    var buffer = new byte[pixels.PixelWidth * pixels.PixelHeight * 4];
                    pixels.CopyPixels(buffer, pixels.PixelWidth * 4, 0);
                    Assert.Equal(0, buffer[3]); // Transparent canvas must survive ICO encoding.
                    Assert.Contains((byte)255, buffer.Where((_, index) => index % 4 == 3));
                    // System.Drawing's frame picker does not select the ICO's zero-byte 256 px entry.
                    // WPF checks that shell frame above; the tray uses the smaller native frames.
                    if (frame.PixelWidth < 256)
                    {
                        data.Position = 0;
                        using var native = new System.Drawing.Icon(data, frame.PixelWidth, frame.PixelHeight);
                        Assert.Equal(frame.PixelWidth, native.Width);
                        Assert.Equal(frame.PixelHeight, native.Height);
                    }
                }
                checkedResources++;
            }
            Assert.Equal(2, checkedResources);
        }
    }
}
