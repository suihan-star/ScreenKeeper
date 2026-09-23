using Microsoft.Win32;
using Serilog;

namespace OLED_Sleeper.UI.Helpers;

/// <summary>
/// Window taskbar buttons and the notification tray follow the Windows shell, independently of the app palette.
/// </summary>
public static class SystemTaskbarTheme
{
    public static string IconResource
    {
        get
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
                return key?.GetValue("SystemUsesLightTheme") is int value && value != 0
                    ? "icon.ico" : "icon-dark.ico";
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Unable to read taskbar theme; using the dark-taskbar icon.");
                return "icon-dark.ico";
            }
        }
    }
}
