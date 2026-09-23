using OLED_Sleeper.Native;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows;

namespace OLED_Sleeper.Features.MonitorPrivacy;

/// <summary>UI-thread hooks report only input location, not key values or message contents.</summary>
public sealed class WindowsPrivacyActivitySource : IPrivacyActivitySource
{
    private readonly PrivacyNative.HookProc _mouseCallback;
    private readonly PrivacyNative.HookProc _keyboardCallback;
    private nint _mouse, _keyboard;
    private Point? _lastMouse;
    private readonly WindowsHealthActivitySnapshot _session = new();
    public event Action<Point>? Activity;
    public WindowsPrivacyActivitySource() { _mouseCallback = Mouse; _keyboardCallback = Keyboard; }

    public void Start()
    {
        if (_mouse != 0) return;
        _session.Start();
        var module = PrivacyNative.GetModuleHandle(null);
        _mouse = PrivacyNative.SetWindowsHookEx(14, _mouseCallback, module, 0);
        _keyboard = PrivacyNative.SetWindowsHookEx(13, _keyboardCallback, module, 0);
        if (_mouse == 0 || _keyboard == 0)
        {
            var error = Marshal.GetLastWin32Error();
            Stop();
            throw new Win32Exception(error);
        }
    }
    private nint Mouse(int code, nint message, nint data)
    {
        try
        {
            if (code >= 0)
            {
                var input = Marshal.PtrToStructure<PrivacyNative.MouseData>(data);
                var point = new Point(input.Point.X, input.Point.Y);
                if ((input.Flags & 1) == 0 && (message != 0x200 || _lastMouse != point))
                {
                    _lastMouse = point;
                    Activity?.Invoke(point);
                }
            }
        }
        catch { /* Never unwind through a native input callback; the idle timer remains conservative. */ }
        return PrivacyNative.CallNextHookEx(_mouse, code, message, data);
    }
    private nint Keyboard(int code, nint message, nint data)
    {
        try
        {
            // Only key-down timing matters. Never marshal or retain the virtual-key code.
            if (code >= 0 && (message == 0x100 || message == 0x104) && (Marshal.ReadInt32(data, 8) & 0x10) == 0)
            {
                var monitor = NativeMethods.MonitorFromWindow(NativeMethods.GetForegroundWindow(), NativeMethods.MONITOR_DEFAULTTONEAREST);
                var info = new NativeMethods.MonitorInfoEx { cbSize = Marshal.SizeOf<NativeMethods.MonitorInfoEx>() };
                if (NativeMethods.GetMonitorInfo(monitor, ref info))
                    Activity?.Invoke(new Point((info.rcMonitor.left + info.rcMonitor.right) / 2.0, (info.rcMonitor.top + info.rcMonitor.bottom) / 2.0));
            }
        }
        catch { /* Always allow the rest of the system to receive input. */ }
        return PrivacyNative.CallNextHookEx(_keyboard, code, message, data);
    }
    public void Stop()
    {
        if (_mouse != 0) PrivacyNative.UnhookWindowsHookEx(_mouse);
        if (_keyboard != 0) PrivacyNative.UnhookWindowsHookEx(_keyboard);
        _mouse = _keyboard = 0;
        _lastMouse = null;
        _session.Stop();
    }
    public PrivacyUsageSnapshot ReadUsage()
    {
        var session = _session.Read();
        Point? cursor = NativeMethods.GetCursorPos(out var point) ? new Point(point.X, point.Y) : null;
        Rect? foreground = null;
        var handle = NativeMethods.GetForegroundWindow();
        var name = new System.Text.StringBuilder(256);
        NativeMethods.GetClassName(handle, name, name.Capacity);
        if (name.ToString() is not ("Progman" or "WorkerW") && NativeMethods.GetWindowRect(handle, out var rect))
            foreground = new Rect(rect.left, rect.top, Math.Max(0, rect.right - rect.left), Math.Max(0, rect.bottom - rect.top));
        return new(cursor, foreground, session.InputAvailable && session.IdleFor < TimeSpan.FromSeconds(1), session.SessionAvailable);
    }
    public void Dispose() => Stop();
}
