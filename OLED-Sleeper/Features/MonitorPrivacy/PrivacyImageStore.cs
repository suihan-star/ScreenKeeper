using OLED_Sleeper.Storage.Interfaces;
using System.IO;
using System.Windows.Media.Imaging;

namespace OLED_Sleeper.Features.MonitorPrivacy;

/// <summary>Immutable product-local images. Failed saves and discarded drafts never destroy old references.</summary>
public sealed class PrivacyImageStore(IStorageRoot root, string directoryName = "PrivacyImages")
{
    public static bool IsAssetName(string? name) => name != null && name.EndsWith(".png", StringComparison.Ordinal) &&
        Guid.TryParseExact(name[..^4], "N", out _);
    public BitmapSource Load(string name)
    {
        if (!IsAssetName(name)) throw new InvalidDataException("Invalid privacy image reference.");
        using var stream = File.OpenRead(Path.Combine(root.DirectoryPath, directoryName, name));
        return Decode(stream);
    }
    public string Import(string path)
    {
        using var source = File.OpenRead(path);
        if (source.Length > 10 * 1024 * 1024) throw new InvalidDataException("图片不能超过 10 MB。");
        var bitmap = Decode(source);
        var directory = Path.Combine(root.DirectoryPath, directoryName);
        Directory.CreateDirectory(directory);
        var name = Guid.NewGuid().ToString("N") + ".png";
        // CreateNew prevents overwriting existing assets; a partial orphan is never referenced.
        using var destination = new FileStream(Path.Combine(directory, name), FileMode.CreateNew, FileAccess.Write, FileShare.None);
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bitmap));
        encoder.Save(destination);
        destination.Flush(true);
        return name;
    }
    private static BitmapSource Decode(Stream stream)
    {
        var decoder = BitmapDecoder.Create(stream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
        var frame = decoder.Frames[0];
        if (frame.PixelWidth <= 0 || frame.PixelHeight <= 0 || (long)frame.PixelWidth * frame.PixelHeight > 40_000_000)
            throw new InvalidDataException("图片分辨率过大，请选择不超过 4000 万像素的图片。");
        frame.Freeze();
        return frame;
    }
}
