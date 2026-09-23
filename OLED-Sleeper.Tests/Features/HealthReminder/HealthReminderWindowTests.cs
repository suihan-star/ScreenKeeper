using OLED_Sleeper.Features.HealthReminder;
using OLED_Sleeper.UI.Localization;
using OLED_Sleeper.UI.Views;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace OLED_Sleeper.Tests.Features.HealthReminder;

[Collection("UI language")]
public sealed class HealthReminderWindowTests
{
    [WpfFact]
    public void CardsRenderWithLocalActionsInBothLanguages()
    {
        if (Application.Current == null) new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
        Application.Current!.Resources.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri("/ScreenKeeper;component/UI/Themes/Calm.xaml", UriKind.Relative) });
        string languageBefore = UiText.Current.Language;
        try
        {
            foreach (string language in new[] { "zh-CN", "en" })
            {
                UiText.Current.SetLanguage(language);
                foreach (HealthCardKind kind in Enum.GetValues<HealthCardKind>())
                {
                    var card = new HealthReminderWindow(kind, new("fixture", 0, 0, 700, 500), TimeSpan.FromSeconds(135));
                    try
                    {
                        Assert.False(card.ShowActivated);
                        Assert.False(card.ShowInTaskbar);
                        var content = (FrameworkElement)card.Content;
                        content.Measure(new Size(card.Width, double.PositiveInfinity));
                        var size = new Size(card.Width, content.DesiredSize.Height);
                        content.Arrange(new Rect(size));
                        content.UpdateLayout();
                        Assert.True(size.Height < 500);
                        var buttons = Descendants<Button>(content).Where(b => b.IsVisible || b.Visibility == Visibility.Visible).ToArray();
                        Assert.Equal(kind == HealthCardKind.PreviewCountdown ? 2 : kind is HealthCardKind.Countdown or HealthCardKind.Preview ? 3 : 4, buttons.Length);
                        Assert.All(buttons, button => Assert.True(button.ActualWidth > 0));
                        Render(content, size, $"health-{language}-{kind}");
                    }
                    finally { card.CloseFromService(); }
                }
            }
        }
        finally { UiText.Current.SetLanguage(languageBefore); }
    }

    [HealthNativeFact]
    public async Task ShowingCardDoesNotActivateOrReplaceForegroundFixture()
    {
        var fixture = new Window { Title = "ScreenKeeper health test fixture", Width = 320, Height = 240, Left = 100, Top = 100 };
        var card = new HealthReminderWindow(HealthCardKind.Reminder, new("fixture", 0, 0, 700, 500), TimeSpan.FromSeconds(20));
        try
        {
            fixture.Show();
            fixture.Activate();
            var fixtureHandle = new WindowInteropHelper(fixture).Handle;
            var setup = Stopwatch.StartNew();
            while (GetForegroundWindow() != fixtureHandle && setup.Elapsed < TimeSpan.FromSeconds(2))
            {
                await fixture.Dispatcher.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle);
                await Task.Delay(50);
            }
            Assert.True(GetForegroundWindow() == fixtureHandle,
                "Native QA setup could not make its own fixture the foreground window. The health card has not been shown; non-activation remains unverified in this desktop session.");
            card.Show();
            await card.Dispatcher.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle);
            Assert.Equal(fixtureHandle, GetForegroundWindow());
            Assert.False(card.IsKeyboardFocusWithin);
            for (int i = 0; i < 5; i++)
            {
                await Task.Delay(100);
                Assert.Equal(fixtureHandle, GetForegroundWindow());
                Assert.False(card.IsKeyboardFocusWithin);
            }
        }
        finally { card.CloseFromService(); fixture.Close(); }
    }

    private static IEnumerable<T> Descendants<T>(DependencyObject node) where T : DependencyObject
    {
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(node); i++)
        {
            var child = VisualTreeHelper.GetChild(node, i);
            if (child is T typed) yield return typed;
            foreach (var nested in Descendants<T>(child)) yield return nested;
        }
    }

    private static void Render(FrameworkElement content, Size size, string name)
    {
        var directory = Environment.GetEnvironmentVariable("SCREENKEEPER_RENDER_QA");
        if (string.IsNullOrEmpty(directory)) return;
        Directory.CreateDirectory(directory);
        var bitmap = new RenderTargetBitmap((int)Math.Ceiling(size.Width), (int)Math.Ceiling(size.Height), 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(content);
        var png = new PngBitmapEncoder();
        png.Frames.Add(BitmapFrame.Create(bitmap));
        using var output = File.Create(Path.Combine(directory, name + ".png"));
        png.Save(output);
    }

    [DllImport("user32.dll")] private static extern nint GetForegroundWindow();
}

public sealed class HealthNativeFactAttribute : WpfFactAttribute
{
    public HealthNativeFactAttribute()
    {
        if (Environment.GetEnvironmentVariable("SCREENKEEPER_NATIVE_QA") != "1")
        {
            Skip = "Set SCREENKEEPER_NATIVE_QA=1 to test non-activation using self-owned Windows fixtures.";
            return;
        }
        if (!Environment.UserInteractive || !HasDefaultInputDesktop())
            Skip = "Native foreground QA requires an unlocked, readable Default input desktop. Health-card non-activation is unverified in this session.";
        else if (GetForegroundWindow() == 0)
            Skip = "No foreground window is observable during native test discovery. Health-card non-activation requires a foreground-capable interactive session and remains unverified.";
    }

    private static bool HasDefaultInputDesktop()
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

    [DllImport("user32.dll")] private static extern nint GetForegroundWindow();
    [DllImport("user32.dll")] private static extern nint OpenInputDesktop(uint flags, [MarshalAs(UnmanagedType.Bool)] bool inherit, uint access);
    [DllImport("user32.dll")][return: MarshalAs(UnmanagedType.Bool)] private static extern bool CloseDesktop(nint desktop);
    [DllImport("user32.dll", EntryPoint = "GetUserObjectInformationW", CharSet = CharSet.Unicode)]
    [return: MarshalAs(UnmanagedType.Bool)] private static extern bool GetUserObjectInformation(nint handle, int index, StringBuilder info, int length, out int needed);
}
