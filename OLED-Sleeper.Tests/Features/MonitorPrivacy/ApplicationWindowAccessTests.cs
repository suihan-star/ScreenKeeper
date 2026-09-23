using OLED_Sleeper.Features.MonitorPrivacy;
using System.Diagnostics;
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
        var script = "Add-Type -AssemblyName PresentationFramework; $w = New-Object Windows.Window; $w.Title='ScreenKeeper native test'; $w.Width=360; $w.Height=240; $w.Left=100; $w.Top=100; $w.ShowActivated=$false; $w.ShowInTaskbar=$true; $w.Content='ScreenKeeper test fixture'; $w.ShowDialog() | Out-Null";
        using var process = Process.Start(new ProcessStartInfo("powershell.exe")
        {
            Arguments = "-NoProfile -STA -EncodedCommand " + Convert.ToBase64String(Encoding.Unicode.GetBytes(script)),
            UseShellExecute = false,
            CreateNoWindow = true,
            WindowStyle = ProcessWindowStyle.Hidden
        })!;
        try
        {
            var access = new ApplicationWindowAccess();
            ApplicationWindow? target = null;
            for (int i = 0; i < 100 && target == null; i++)
            {
                await Task.Delay(100);
                target = access.Enumerate().FirstOrDefault(w => w.Token.StartsWith(process.Id + ":", StringComparison.Ordinal));
            }
            Assert.NotNull(target);
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
