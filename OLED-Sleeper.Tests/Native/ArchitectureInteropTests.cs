using OLED_Sleeper.Native;
using System.Runtime.InteropServices;

namespace OLED_Sleeper.Tests.Native;

public sealed class ArchitectureInteropTests
{
    [Fact]
    public void TestHostRunsTheRequestedArchitecture()
    {
        var expected = Environment.GetEnvironmentVariable("SCREENKEEPER_EXPECT_ARCH");
        if (!string.IsNullOrEmpty(expected))
            Assert.Equal(expected, RuntimeInformation.ProcessArchitecture.ToString().ToLowerInvariant());
    }

    [Fact]
    public void WindowStylesAndPointerSizedDataRoundTripOnAnOwnedHiddenWindow()
    {
        var hwnd = CreateWindowEx(0, "STATIC", "ScreenKeeper interop fixture", 0x80000000, 0, 0, 1, 1, 0, 0, 0, 0);
        Assert.NotEqual(0, hwnd);
        try
        {
            // The high style bit and high pointer bits must survive both native entry-point variants.
            Assert.NotEqual(0, NativeMethods.GetWindowLongPtr(hwnd, -16).ToInt64() & 0x80000000L);
            var original = NativeMethods.GetWindowLongPtr(hwnd, NativeMethods.GWL_EXSTYLE);
            var changed = new nint(original.ToInt64() | NativeMethods.WS_EX_NOACTIVATE);
            Assert.Equal(original, NativeMethods.SetWindowLongPtr(hwnd, NativeMethods.GWL_EXSTYLE, changed));
            Assert.Equal(changed, NativeMethods.GetWindowLongPtr(hwnd, NativeMethods.GWL_EXSTYLE));
            Assert.Equal(changed, NativeMethods.SetWindowLongPtr(hwnd, NativeMethods.GWL_EXSTYLE, original));
            Assert.Equal(original, NativeMethods.GetWindowLongPtr(hwnd, NativeMethods.GWL_EXSTYLE));

            var value = IntPtr.Size == 8 ? new nint(0x12345678ABCDEF01L) : new nint(unchecked((int)0x89ABCDEF));
            Assert.Equal(0, NativeMethods.SetWindowLongPtr(hwnd, -21, value)); // GWLP_USERDATA
            Assert.Equal(value, NativeMethods.GetWindowLongPtr(hwnd, -21));
            Assert.Equal(value, NativeMethods.SetWindowLongPtr(hwnd, -21, 0));
        }
        finally { Assert.True(DestroyWindow(hwnd)); }
    }

    [Fact]
    public void InvalidWindowPreservesTheNativeError()
    {
        Assert.Equal(0, NativeMethods.GetWindowLongPtr(0, -20));
        Assert.Equal(1400, Marshal.GetLastWin32Error()); // ERROR_INVALID_WINDOW_HANDLE
        Assert.Equal(0, NativeMethods.SetWindowLongPtr(0, -20, 0));
        Assert.Equal(1400, Marshal.GetLastWin32Error());
    }

    [Fact]
    public void PhysicalMonitorAndInputLayoutsMatchTheProcessAbi()
    {
        Assert.Equal(IntPtr.Size + 256, Marshal.SizeOf<NativeMethods.PHYSICAL_MONITOR>());
        Assert.Equal(IntPtr.Size, Marshal.OffsetOf<NativeMethods.PHYSICAL_MONITOR>(nameof(NativeMethods.PHYSICAL_MONITOR.szPhysicalMonitorDescription)).ToInt32());
        Assert.Equal(IntPtr.Size == 8 ? 32 : 24, Marshal.SizeOf<PrivacyNative.MouseData>());
        Assert.Equal(IntPtr.Size == 8 ? 24 : 20, Marshal.OffsetOf<PrivacyNative.MouseData>(nameof(PrivacyNative.MouseData.ExtraInfo)).ToInt32());
        Assert.Equal(72, Marshal.SizeOf<NativeMethods.DISPLAYCONFIG_PATH_INFO>());
        Assert.Equal(64, Marshal.SizeOf<NativeMethods.DISPLAYCONFIG_MODE_INFO>());
        Assert.Equal(84, Marshal.SizeOf<NativeMethods.DISPLAYCONFIG_SOURCE_DEVICE_NAME>());
        Assert.Equal(420, Marshal.SizeOf<NativeMethods.DISPLAYCONFIG_TARGET_DEVICE_NAME>());

        var monitor = new NativeMethods.PHYSICAL_MONITOR { hPhysicalMonitor = new nint(-123), szPhysicalMonitorDescription = "屏幕 fixture" };
        var buffer = Marshal.AllocHGlobal(Marshal.SizeOf<NativeMethods.PHYSICAL_MONITOR>());
        try
        {
            Marshal.StructureToPtr(monitor, buffer, false);
            Assert.Equal("屏幕 fixture", Marshal.PtrToStringUni(buffer + IntPtr.Size));
            var read = Marshal.PtrToStructure<NativeMethods.PHYSICAL_MONITOR>(buffer);
            Assert.Equal(monitor.hPhysicalMonitor, read.hPhysicalMonitor);
            Assert.Equal(monitor.szPhysicalMonitorDescription, read.szPhysicalMonitorDescription);
        }
        finally { Marshal.FreeHGlobal(buffer); }
    }

    [Fact]
    public void ProcessImageQueryMatchesCurrentExecutableWithoutModuleEnumeration()
    {
        Assert.True(ProcessImagePath.TryGet((uint)Environment.ProcessId, out var path));
        Assert.Equal(Environment.ProcessPath, path, ignoreCase: true);
    }

    [Theory]
    [InlineData(0u)]
    [InlineData(uint.MaxValue)]
    public void UnqueryableProcessDoesNotReturnAPath(uint id)
    {
        Assert.False(ProcessImagePath.TryGet(id, out var path));
        Assert.Empty(path);
    }

    [DllImport("user32.dll", EntryPoint = "CreateWindowExW", ExactSpelling = true, CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern nint CreateWindowEx(uint extendedStyle, string className, string title, uint style,
        int x, int y, int width, int height, nint parent, nint menu, nint instance, nint parameter);

    [DllImport("user32.dll", ExactSpelling = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DestroyWindow(nint window);
}
