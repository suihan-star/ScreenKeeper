using System.IO;

namespace OLED_Sleeper.Features.MonitorPrivacy;

/// <summary>Recognizes verified application chrome, never arbitrary owned dialogs.</summary>
public static class ApplicationWindowFamilyPolicy
{
    public static bool IsCompanion(ApplicationWindow target, ApplicationWindow window)
    {
        const long decorationStyles = 0x80000 | 0x80 | 0x08000000; // LAYERED, TOOLWINDOW, NOACTIVATE
        if (target.ClassName != "WeWorkWindow" || !string.Equals(Path.GetFileName(target.Path), "WXWork.exe", StringComparison.OrdinalIgnoreCase) ||
            target.ProcessId == 0 || window.ProcessId != target.ProcessId || window.Owner != target.Handle ||
            !string.Equals(target.Path, window.Path, StringComparison.OrdinalIgnoreCase) ||
            target.Minimized || window.Minimized || target.Dpi == 0 ||
            (window.ExtendedStyle & decorationStyles) != decorationStyles ||
            target.Bounds.IsEmpty || target.Bounds.Width <= 0 || target.Bounds.Height <= 0 ||
            window.Bounds.IsEmpty || window.Bounds.Width <= 0 || window.Bounds.Height <= 0)
            return false;

        double scale = target.Dpi / 96d;
        if (window.ClassName == "TitleBarWindow")
            return target.Bounds.Contains(window.Bounds) &&
                window.Bounds.Left - target.Bounds.Left <= 2 * scale &&
                target.Bounds.Right - window.Bounds.Right <= 2 * scale &&
                window.Bounds.Top - target.Bounds.Top <= 2 * scale &&
                window.Bounds.Height <= Math.Min(64 * scale, target.Bounds.Height / 4);

        if (window.ClassName == "PerryShadowWnd")
        {
            var frame = target.Bounds;
            frame.Inflate(32 * scale, 32 * scale);
            return window.Bounds.Contains(target.Bounds) && frame.Contains(window.Bounds);
        }
        return false;
    }
}
