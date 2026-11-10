using System.Runtime.InteropServices;

namespace OLED_Sleeper.Native
{
    internal static class ColorNative
    {
        [StructLayout(LayoutKind.Sequential)]
        internal struct AdvancedColorInfo
        {
            public NativeMethods.DISPLAYCONFIG_DEVICE_INFO_HEADER Header;
            public uint Flags;
            public uint ColorEncoding;
            public uint BitsPerColorChannel;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct AdvancedColorInfo2
        {
            public NativeMethods.DISPLAYCONFIG_DEVICE_INFO_HEADER Header;
            public uint Flags;
            public uint ColorEncoding;
            public uint BitsPerColorChannel;
            public uint ActiveColorMode;
        }

        [DllImport("user32.dll", EntryPoint = "DisplayConfigGetDeviceInfo")]
        internal static extern int GetAdvancedColorInfo(ref AdvancedColorInfo request);

        [DllImport("user32.dll", EntryPoint = "DisplayConfigGetDeviceInfo")]
        internal static extern int GetAdvancedColorInfo2(ref AdvancedColorInfo2 request);

        [DllImport("gdi32.dll", CharSet = CharSet.Unicode, EntryPoint = "CreateDCW")]
        internal static extern nint CreateDisplayDc(string driver, string device, nint output, nint initData);

        [DllImport("gdi32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool DeleteDC(nint dc);

        [DllImport("gdi32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool GetDeviceGammaRamp(nint dc, [Out] ushort[] ramp);

        [DllImport("gdi32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool SetDeviceGammaRamp(nint dc, [In] ushort[] ramp);

        [DllImport("dxva2.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool GetMonitorCapabilities(nint monitor, out uint capabilities, out uint colorTemperatures);

        [DllImport("dxva2.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool GetMonitorColorTemperature(nint monitor, out uint temperature);

        [DllImport("dxva2.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool GetMonitorRedGreenOrBlueGain(nint monitor, uint channel, out uint min, out uint current, out uint max);
    }
}
