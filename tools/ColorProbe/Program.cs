using OLED_Sleeper.Features.ColorTemperature;
using OLED_Sleeper.Native;
using System.Runtime.InteropServices;
using System.Text.Json;

if (args.Contains("--test-dell15") || args.Contains("--test-dell-service") || args.Contains("--trial-watchdog"))
{
    Environment.ExitCode = await ColorTrial.RunAsync(args);
    return;
}

var backend = new WindowsGammaBackend();
try
{
    var capabilities = await backend.DiscoverAsync(CancellationToken.None);
    var ddc = new List<object>();
    if (args.Contains("--ddc-readonly"))
    {
        NativeMethods.MonitorEnumProc callback = (nint monitor, nint dc, ref NativeMethods.Rect rect, nint data) =>
        {
            var info = new NativeMethods.MonitorInfoEx { cbSize = Marshal.SizeOf<NativeMethods.MonitorInfoEx>() };
            if (!NativeMethods.GetMonitorInfo(monitor, ref info) ||
                !NativeMethods.GetNumberOfPhysicalMonitorsFromHMONITOR(monitor, out uint count) || count is 0 or > 16) return true;
            var physical = new NativeMethods.PHYSICAL_MONITOR[count];
            if (!NativeMethods.GetPhysicalMonitorsFromHMONITOR(monitor, count, physical)) return true;
            try
            {
                foreach (var target in physical)
                {
                    bool capsRead = ColorNative.GetMonitorCapabilities(target.hPhysicalMonitor, out uint flags, out uint temperatures);
                    uint temperature = 0;
                    bool temperatureRead = capsRead && (flags & 8) != 0 && ColorNative.GetMonitorColorTemperature(target.hPhysicalMonitor, out temperature);
                    var gains = new List<object>();
                    if (capsRead && (flags & 0x10) != 0)
                        for (uint channel = 0; channel < 3; channel++)
                        {
                            bool read = ColorNative.GetMonitorRedGreenOrBlueGain(target.hPhysicalMonitor, channel, out uint min, out uint current, out uint max);
                            gains.Add(new { Channel = channel, Read = read, Min = min, Current = current, Max = max });
                        }
                    ddc.Add(new { DeviceName = info.szDevice, PhysicalCount = count, CapabilitiesRead = capsRead,
                        CapabilityFlags = flags, ColorTemperatureFlags = temperatures, TemperatureRead = temperatureRead, TemperatureEnum = temperature, Gains = gains });
                }
            }
            finally { NativeMethods.DestroyPhysicalMonitors(count, physical); }
            return true;
        };
        NativeMethods.EnumDisplayMonitors(nint.Zero, nint.Zero, callback, nint.Zero);
    }
    Console.WriteLine(JsonSerializer.Serialize(new { ReadOnly = true, CapturedAt = DateTimeOffset.Now,
        VisualOutputVerified = false, Devices = capabilities, Ddc = ddc }, new JsonSerializerOptions { WriteIndented = true }));
}
catch (Exception error)
{
    Console.Error.WriteLine($"只读检测失败：{error.GetType().Name}: {error.Message}");
    Environment.ExitCode = 1;
}
