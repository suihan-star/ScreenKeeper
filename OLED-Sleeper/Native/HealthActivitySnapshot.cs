using Microsoft.Win32;
using OLED_Sleeper.Features.HealthReminder;
using System.Runtime.InteropServices;
using System.Text;

namespace OLED_Sleeper.Native;

/// <summary>Polls session-local input timing; no hooks or input contents are collected.</summary>
public sealed class WindowsHealthActivitySnapshot : IUserActivitySnapshot
{
    private bool _started;
    private volatile bool _locked, _suspended;
    public event Action? Changed;

    public void Start()
    {
        if (_started) return;
        SystemEvents.SessionSwitch += SessionChanged;
        SystemEvents.PowerModeChanged += PowerChanged;
        _started = true;
    }

    public void Stop()
    {
        if (!_started) return;
        SystemEvents.SessionSwitch -= SessionChanged;
        SystemEvents.PowerModeChanged -= PowerChanged;
        _started = false;
    }

    public HealthActivitySnapshot Read()
    {
        var input = new NativeMethods.LASTINPUTINFO { cbSize = (uint)Marshal.SizeOf<NativeMethods.LASTINPUTINFO>() };
        bool inputAvailable = NativeMethods.GetLastInputInfo(ref input);
        // Both values are DWORD uptime. Unsigned subtraction handles the 49.7-day wrap.
        var idle = TimeSpan.FromMilliseconds(unchecked((uint)Environment.TickCount64 - input.dwTime));
        bool session = !_locked && !_suspended && IsInputDesktopAvailable();
        if (!session) return new(inputAvailable, idle, false, false, false, null);
        var foreground = NativeMethods.GetForegroundWindow();
        if (foreground == 0) return new(inputAvailable, idle, true, false, false, null);
        var monitor = NativeMethods.MonitorFromWindow(foreground, NativeMethods.MONITOR_DEFAULTTONEAREST);
        var info = new NativeMethods.MonitorInfoEx { cbSize = Marshal.SizeOf<NativeMethods.MonitorInfoEx>() };
        if (!NativeMethods.GetMonitorInfo(monitor, ref info)) return new(inputAvailable, idle, true, false, false, null);
        var screen = new HealthScreen(info.szDevice, info.rcWork.left, info.rcWork.top, info.rcWork.right, info.rcWork.bottom)
        {
            FullLeft = info.rcMonitor.left,
            FullTop = info.rcMonitor.top,
            FullRight = info.rcMonitor.right,
            FullBottom = info.rcMonitor.bottom
        };
        PrivacyNative.GetWindowThreadProcessId(foreground, out uint processId);
        if (processId == Environment.ProcessId)
            return new(inputAvailable, idle, true, true, false, screen);
        if (foreground == GetShellWindow() || foreground == GetDesktopWindow())
            return new(inputAvailable, idle, true, true, false, screen);
        bool geometry = NativeMethods.GetWindowRect(foreground, out var rect);
        bool fullscreen = geometry && rect.left <= info.rcMonitor.left && rect.top <= info.rcMonitor.top &&
            rect.right >= info.rcMonitor.right && rect.bottom >= info.rcMonitor.bottom;
        return new(inputAvailable, idle, true, geometry, fullscreen, screen);
    }

    private static bool IsInputDesktopAvailable()
    {
        var desktop = OpenInputDesktop(0, false, 1);
        if (desktop == 0) return false;
        try
        {
            var name = new StringBuilder(256);
            return GetUserObjectInformation(desktop, 2, name, name.Capacity * sizeof(char), out _) &&
                string.Equals(name.ToString(), "Default", StringComparison.OrdinalIgnoreCase);
        }
        finally { CloseDesktop(desktop); }
    }

    private void SessionChanged(object sender, SessionSwitchEventArgs e)
    {
        if (e.Reason is SessionSwitchReason.SessionLock or SessionSwitchReason.SessionLogoff or SessionSwitchReason.ConsoleDisconnect or SessionSwitchReason.RemoteDisconnect)
            _locked = true;
        else if (e.Reason is SessionSwitchReason.SessionUnlock or SessionSwitchReason.SessionLogon or SessionSwitchReason.ConsoleConnect or SessionSwitchReason.RemoteConnect)
            _locked = false;
        Changed?.Invoke();
    }

    private void PowerChanged(object sender, PowerModeChangedEventArgs e)
    {
        if (e.Mode == PowerModes.Suspend) _suspended = true;
        else if (e.Mode == PowerModes.Resume) _suspended = false;
        Changed?.Invoke();
    }

    public void Dispose() => Stop();

    [DllImport("user32.dll")] private static extern nint OpenInputDesktop(uint flags, [MarshalAs(UnmanagedType.Bool)] bool inherit, uint access);
    [DllImport("user32.dll")][return: MarshalAs(UnmanagedType.Bool)] private static extern bool CloseDesktop(nint desktop);
    [DllImport("user32.dll", EntryPoint = "GetUserObjectInformationW", CharSet = CharSet.Unicode)]
    [return: MarshalAs(UnmanagedType.Bool)] private static extern bool GetUserObjectInformation(nint handle, int index, StringBuilder info, int length, out int needed);
    [DllImport("user32.dll")] private static extern nint GetShellWindow();
    [DllImport("user32.dll")] private static extern nint GetDesktopWindow();
}
