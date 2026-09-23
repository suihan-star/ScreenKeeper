using OLED_Sleeper.Native;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;

namespace OLED_Sleeper.Features.MonitorPrivacy;

public sealed class ApplicationWindowAccess : IApplicationWindowAccess
{
    public IReadOnlyList<ApplicationWindow> Enumerate(bool titles = false)
    {
        var result = new List<ApplicationWindow>();
        if (!PrivacyNative.EnumWindows((handle, _) =>
        {
            try
            {
                if (!PrivacyNative.IsWindowVisible(handle)) return true;
                PrivacyNative.GetWindowThreadProcessId(handle, out var id);
                if (id == Environment.ProcessId) return true;
                using var process = Process.GetProcessById((int)id);
                var path = process.MainModule?.FileName;
                if (!PrivacyOptions.IsLocalExecutable(path)) return true;
                if (PrivacyNative.GetCloaked(handle, 14, out var cloaked, 4) != 0 || cloaked != 0) return true;
                var cls = new StringBuilder(256);
                NativeMethods.GetClassName(handle, cls, cls.Capacity);
                if (cls.ToString() is "Progman" or "WorkerW" or "Shell_TrayWnd" or "Shell_SecondaryTrayWnd") return true;
                if (!ReadBounds(handle, out var bounds)) return true;
                var minimized = PrivacyNative.IsIconic(handle);
                if (minimized)
                {
                    var placement = new WindowPlacement { Length = Marshal.SizeOf<WindowPlacement>() };
                    if (!GetWindowPlacement(handle, ref placement)) return true;
                    // WINDOWPLACEMENT uses workspace coordinates; convert using this monitor's work-area offset.
                    var monitor = NativeMethods.MonitorFromWindow(handle, NativeMethods.MONITOR_DEFAULTTONEAREST);
                    var info = new NativeMethods.MonitorInfoEx { cbSize = Marshal.SizeOf<NativeMethods.MonitorInfoEx>() };
                    if (!NativeMethods.GetMonitorInfo(monitor, ref info)) return true;
                    bounds = placement.Normal.ToWindowsRect();
                    bounds.Offset(info.rcWork.left - info.rcMonitor.left, info.rcWork.top - info.rcMonitor.top);
                }
                var title = new StringBuilder(512);
                if (titles) GetWindowText(handle, title, title.Capacity);
                var token = $"{id}:{process.StartTime.ToUniversalTime().Ticks}:{handle}:{cls}";
                result.Add(new(token, handle, path!, cls.ToString(), title.ToString(), bounds, minimized, PrivacyNative.GetWindow(handle, 4),
                    id, NativeMethods.GetWindowLongPtr(handle, NativeMethods.GWL_EXSTYLE).ToInt64(), PrivacyNative.GetDpiForWindow(handle)));
            }
            catch { /* Inaccessible/elevated or closing windows cannot be selected safely. */ }
            return true;
        }, 0)) throw new InvalidOperationException("Window enumeration failed.");
        return result;
    }
    public bool IsRunning(string path)
    {
        foreach (var process in Process.GetProcessesByName(Path.GetFileNameWithoutExtension(path)))
        {
            using (process)
            {
                try { if (string.Equals(process.MainModule?.FileName, path, StringComparison.OrdinalIgnoreCase)) return true; }
                catch { return true; } // Do not duplicate a matching process we cannot inspect.
            }
        }
        return false;
    }
    public bool Launch(string path)
    {
        if (!PrivacyOptions.IsLocalExecutable(path) || !File.Exists(path)) return false;
        try
        {
            using var process = Process.Start(new ProcessStartInfo(path) { UseShellExecute = false, WorkingDirectory = Path.GetDirectoryName(path)!, WindowStyle = ProcessWindowStyle.Minimized });
            return process != null;
        }
        catch { return false; }
    }
    public bool Place(ApplicationWindow window, Rect bounds)
    {
        if (!Enumerate().Any(w => w.Token == window.Token && w.Owner == 0)) return false;
        var style = NativeMethods.GetWindowLongPtr(window.Handle, NativeMethods.GWL_EXSTYLE).ToInt64();
        if ((style & (0x80000 | 0x20)) != 0) return false; // Layered/transparent windows cannot establish opaque coverage.
        if (PrivacyNative.IsIconic(window.Handle)) return PrivacyNative.ShowWindowAsync(window.Handle, 4); // SW_SHOWNOACTIVATE
        if (!NativeMethods.GetWindowRect(window.Handle, out var raw) || !ReadBounds(window.Handle, out var frame)) return false;
        var outer = raw.ToWindowsRect();
        return NativeMethods.SetWindowPos(window.Handle, 0,
            (int)(bounds.X - (frame.Left - outer.Left)), (int)(bounds.Y - (frame.Top - outer.Top)),
            (int)(bounds.Width + outer.Width - frame.Width), (int)(bounds.Height + outer.Height - frame.Height),
            NativeMethods.SWP_NOACTIVATE | 0x4000 | 0x0200); // ASYNCWINDOWPOS, NOOWNERZORDER; never make it topmost.
    }
    public bool Covers(ApplicationWindow window, Rect bounds) => Enumerate().Any(w => w.Token == window.Token && !w.Minimized && w.Bounds.Contains(bounds));
    private static bool ReadBounds(nint handle, out Rect bounds)
    {
        return DesktopWindowAccess.TryReadBounds(handle, out bounds) && bounds.Width > 0 && bounds.Height > 0;
    }

    [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern int GetWindowText(nint window, StringBuilder text, int count);
    [StructLayout(LayoutKind.Sequential)]
    private struct WindowPlacement
    {
        public int Length, Flags, Show;
        public NativeMethods.POINT Minimum, Maximum;
        public NativeMethods.Rect Normal;
    }
    [DllImport("user32.dll")] private static extern bool GetWindowPlacement(nint window, ref WindowPlacement placement);
}
