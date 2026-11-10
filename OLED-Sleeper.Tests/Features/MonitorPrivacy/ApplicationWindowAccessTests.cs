using OLED_Sleeper.Features.MonitorPrivacy;
using OLED_Sleeper.Native;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;

namespace OLED_Sleeper.Tests.Features.MonitorPrivacy;

public class ApplicationWindowAccessTests
{
    [NativeDesktopFact]
    public async Task NativePlacementAndMinimization_WorkOnAnOwnedFixtureProcess()
    {
        // The fixture is the only window touched. No desktop-wide action or user application is used.
        var script = "[Console]::WriteLine([IntPtr]::Size); Add-Type -AssemblyName PresentationFramework; $w = New-Object Windows.Window; $w.Title='ScreenKeeper native test'; $w.Width=360; $w.Height=240; $w.Left=100; $w.Top=100; $w.ShowActivated=$false; $w.ShowInTaskbar=$true; $w.Content='ScreenKeeper test fixture'; $w.ShowDialog() | Out-Null";
        // WOW64 must inspect a real x64 target, not accidentally start another x86 fixture.
        var system = Environment.Is64BitOperatingSystem && !Environment.Is64BitProcess ? "Sysnative" : "System32";
        var shell = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), system, "WindowsPowerShell", "v1.0", "powershell.exe");
        using var process = Process.Start(new ProcessStartInfo(shell)
        {
            Arguments = "-NoProfile -STA -EncodedCommand " + Convert.ToBase64String(Encoding.Unicode.GetBytes(script)),
            UseShellExecute = false,
            RedirectStandardOutput = true,
            CreateNoWindow = true,
            WindowStyle = ProcessWindowStyle.Hidden
        })!;
        try
        {
            Assert.Equal(Environment.Is64BitOperatingSystem ? "8" : "4",
                await process.StandardOutput.ReadLineAsync().WaitAsync(TimeSpan.FromSeconds(15)));
            Assert.True(ProcessImagePath.TryGet((uint)process.Id, out var imagePath));
            var access = new ApplicationWindowAccess();
            Assert.True(access.IsRunning(imagePath));
            ApplicationWindow? target = null;
            for (int i = 0; i < 100 && target == null; i++)
            {
                await Task.Delay(100);
                target = access.Enumerate().FirstOrDefault(w => w.Token.StartsWith(process.Id + ":", StringComparison.Ordinal));
            }
            Assert.NotNull(target);
            Assert.Equal(imagePath, target.Path, ignoreCase: true);
            Assert.Empty(target.Title); // Automatic inspection must not read titles.
            var area = new Rect(120, 120, 440, 320);
            Assert.True(access.Place(target, area));
            for (int i = 0; i < 30 && !access.Covers(target, area); i++) await Task.Delay(100);
            Assert.True(access.Covers(target, area));
            ShowWindowAsync(target.Handle, 7);
            for (int i = 0; i < 30 && !IsIconic(target.Handle); i++) await Task.Delay(100);
            Assert.True(IsIconic(target.Handle));
            var minimized = access.Enumerate().Single(w => w.Token == target.Token);
            Assert.True(minimized.Minimized);
            Assert.True(minimized.Bounds.X > -10000);
            Assert.True(access.Place(minimized, area));
            await Task.Delay(200);
            Assert.True(access.Place(minimized, area));
            for (int i = 0; i < 30 && !access.Covers(target, area); i++) await Task.Delay(100);
            Assert.True(access.Covers(target, area));
        }
        finally
        {
            if (!process.HasExited) { process.Kill(); await process.WaitForExitAsync(); }
        }
    }
    [DllImport("user32.dll")] private static extern bool ShowWindowAsync(nint handle, int command);
    [DllImport("user32.dll")] private static extern bool IsIconic(nint handle);
}

public sealed class NativeDesktopFactAttribute : FactAttribute
{
    public NativeDesktopFactAttribute()
    {
        if (Environment.GetEnvironmentVariable("SCREENKEEPER_NATIVE_QA") != "1")
            Skip = "Set SCREENKEEPER_NATIVE_QA=1 in an interactive Windows desktop session.";
    }
}
