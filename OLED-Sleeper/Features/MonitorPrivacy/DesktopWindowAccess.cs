using OLED_Sleeper.Native;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;

namespace OLED_Sleeper.Features.MonitorPrivacy;

public interface IDesktopWindowAccess
{
    IReadOnlyList<DesktopWindow> EnumerateVisibleWindows();
    bool TryMinimize(DesktopWindow window, Rect screen);
}

/// <summary>Reads window geometry and ownership, never titles or content.</summary>
public sealed class DesktopWindowAccess : IDesktopWindowAccess
{
    public IReadOnlyList<DesktopWindow> EnumerateVisibleWindows()
    {
        var screens = ReadScreens();
        var result = new List<DesktopWindow>();
        bool inspectionFailed = false;
        var succeeded = PrivacyNative.EnumWindows((hwnd, _) =>
        {
            try
            {
                if (!PrivacyNative.IsWindowVisible(hwnd) || PrivacyNative.IsIconic(hwnd)) return true;
                PrivacyNative.GetWindowThreadProcessId(hwnd, out var process);
                if (process == Environment.ProcessId) return true;
                var name = new StringBuilder(256);
                NativeMethods.GetClassName(hwnd, name, name.Capacity);
                if (name.ToString() is "Progman" or "WorkerW" or "Shell_TrayWnd" or "Shell_SecondaryTrayWnd") return true;
                if (PrivacyNative.GetCloaked(hwnd, 14, out var cloaked, sizeof(int)) == 0 && cloaked != 0) return true;
                if (!TryReadBounds(hwnd, out var bounds))
                {
                    // Closing between enumeration and inspection is normal, not a privacy failure.
                    if (!PrivacyNative.IsWindow(hwnd) || !PrivacyNative.IsWindowVisible(hwnd)) return true;
                    inspectionFailed = true;
                    return false;
                }
                // A few pixels beyond the outer desktop edge are not content on another screen.
                // Clip against the actual monitor rectangles, not their overall bounding rectangle.
                bounds = DesktopWindowPolicy.VisibleBounds(bounds, screens);
                if (bounds.IsEmpty || bounds.Width <= 0 || bounds.Height <= 0) return true;
                var hasMinimizeButton = (NativeMethods.GetWindowLongPtr(hwnd, -16).ToInt64() & 0x20000) != 0;
                result.Add(new DesktopWindow(hwnd, bounds, hasMinimizeButton, PrivacyNative.GetWindow(hwnd, 4))); // GW_OWNER
                return true;
            }
            catch { inspectionFailed = true; return false; }
        }, 0);
        if (!succeeded || inspectionFailed) throw new InvalidOperationException("Cannot enumerate desktop windows.");
        return result;
    }

    public bool TryMinimize(DesktopWindow window, Rect screen)
    {
        if (!PrivacyNative.IsWindow(window.Handle) || !PrivacyNative.IsWindowVisible(window.Handle) || PrivacyNative.IsIconic(window.Handle)) return true;
        // A window may move after enumeration. Recheck immediately before issuing the request.
        var screens = ReadScreens();
        if (!screens.Contains(screen) || !TryReadBounds(window.Handle, out var current)) return false;
        current = DesktopWindowPolicy.VisibleBounds(current, screens);
        if (current.IsEmpty) return true;
        if (!screen.Contains(current)) return false;
        return PrivacyNative.ShowWindowAsync(window.Handle, 7); // SW_SHOWMINNOACTIVE: do not steal focus.
    }

    internal static bool TryReadBounds(nint hwnd, out Rect bounds)
    {
        if (NativeMethods.DwmGetWindowAttribute(hwnd, NativeMethods.DWMWA_EXTENDED_FRAME_BOUNDS, out var rect, Marshal.SizeOf<NativeMethods.Rect>()) != 0 &&
            !NativeMethods.GetWindowRect(hwnd, out rect))
        {
            bounds = Rect.Empty;
            return false;
        }
        bounds = rect.ToWindowsRect();
        if (PrivacyNative.IsZoomed(hwnd))
        {
            var monitor = NativeMethods.MonitorFromWindow(hwnd, NativeMethods.MONITOR_DEFAULTTONEAREST);
            var info = new NativeMethods.MonitorInfoEx { cbSize = Marshal.SizeOf<NativeMethods.MonitorInfoEx>() };
            var dpi = PrivacyNative.GetDpiForWindow(hwnd);
            if (dpi != 0 && NativeMethods.GetMonitorInfo(monitor, ref info))
            {
                var padding = PrivacyNative.GetSystemMetricsForDpi(92, dpi); // SM_CXPADDEDBORDER
                var borderX = PrivacyNative.GetSystemMetricsForDpi(32, dpi) + padding; // SM_CXSIZEFRAME
                var borderY = PrivacyNative.GetSystemMetricsForDpi(33, dpi) + padding; // SM_CYSIZEFRAME
                bounds = DesktopWindowPolicy.MaximizedVisibleBounds(bounds, info.rcMonitor.ToWindowsRect(), borderX, borderY);
            }
        }
        return true;
    }

    private static IReadOnlyList<Rect> ReadScreens()
    {
        var screens = new List<Rect>();
        if (!NativeMethods.EnumDisplayMonitors(0, 0, (nint monitor, nint dc, ref NativeMethods.Rect rect, nint data) =>
        {
            screens.Add(rect.ToWindowsRect());
            return true;
        }, 0) || screens.Count == 0)
            throw new InvalidOperationException("Cannot inspect desktop monitor bounds.");
        return screens;
    }
}
