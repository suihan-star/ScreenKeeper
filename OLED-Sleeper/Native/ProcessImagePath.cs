using Microsoft.Win32.SafeHandles;
using System.Runtime.InteropServices;
using System.Text;

namespace OLED_Sleeper.Native;

internal static class ProcessImagePath
{
    internal static bool TryGet(uint processId, out string path)
    {
        path = string.Empty;
        // Module enumeration fails across x86 -> x64. This query needs no module access or elevation.
        using var process = OpenProcess(0x1000, false, processId); // PROCESS_QUERY_LIMITED_INFORMATION
        if (process.IsInvalid) return false;
        for (int capacity = 512; capacity <= 32768; capacity *= 2)
        {
            var buffer = new StringBuilder(capacity);
            uint length = (uint)capacity;
            if (QueryFullProcessImageName(process, 0, buffer, ref length))
            {
                path = buffer.ToString();
                return path.Length > 0;
            }
            if (Marshal.GetLastWin32Error() != 122) return false; // ERROR_INSUFFICIENT_BUFFER
        }
        return false;
    }

    [DllImport("kernel32.dll", ExactSpelling = true, SetLastError = true)]
    private static extern SafeProcessHandle OpenProcess(uint access, [MarshalAs(UnmanagedType.Bool)] bool inherit, uint processId);

    [DllImport("kernel32.dll", EntryPoint = "QueryFullProcessImageNameW", ExactSpelling = true, CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool QueryFullProcessImageName(SafeProcessHandle process, uint flags, StringBuilder path, ref uint length);
}
