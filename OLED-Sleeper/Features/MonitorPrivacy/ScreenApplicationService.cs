using Serilog;
using System.Windows;

namespace OLED_Sleeper.Features.MonitorPrivacy;

public sealed record ApplicationWindow(string Token, nint Handle, string Path, string ClassName, string Title, Rect Bounds, bool Minimized, nint Owner,
    uint ProcessId = 0, long ExtendedStyle = 0, uint Dpi = 96);
public interface IApplicationWindowAccess
{
    IReadOnlyList<ApplicationWindow> Enumerate(bool titles = false);
    bool Launch(string path);
    bool IsRunning(string path) => Enumerate().Any(w => string.Equals(w.Path, path, StringComparison.OrdinalIgnoreCase));
    bool Place(ApplicationWindow window, Rect bounds);
    bool Covers(ApplicationWindow window, Rect bounds);
}
public interface IScreenApplicationService
{
    Task<bool> ShowAsync(PrivacyOptions options, Rect bounds, CancellationToken token = default);
    string? FailureFor(Rect bounds) => null;
}

/// <summary>Always called behind an opaque curtain. Unknown identity or geometry fails closed.</summary>
public sealed class ScreenApplicationService(IApplicationWindowAccess apps, IDesktopWindowAccess desktop) : IScreenApplicationService
{
    private readonly Dictionary<Rect, string> _failures = new();
    public string? FailureFor(Rect bounds) => _failures.GetValueOrDefault(bounds);
    private bool Fail(Rect bounds, string message)
    {
        _failures[bounds] = message;
        Log.Information("Application privacy retained curtain: {Reason}", message);
        return false;
    }
    private const string PopupFailure = "指定应用存在尚未支持的附属窗口或弹窗，请关闭弹窗后重试。已保留隐私幕。";
    private const string ChangedFailure = "指定窗口在切换过程中发生变化，请重新选择窗口。已保留隐私幕。";
    private readonly SemaphoreSlim _serial = new(1, 1);
    public async Task<bool> ShowAsync(PrivacyOptions options, Rect bounds, CancellationToken token = default)
    {
        await _serial.WaitAsync(token);
        try
        {
            options = options.Copy();
            _failures[bounds] = "无法安全切换到指定应用，已保留隐私幕。";
            if (!PrivacyOptions.IsLocalExecutable(options.ApplicationPath)) return Fail(bounds, "指定应用路径无效，请重新选择应用。已保留隐私幕。");
            var launched = false;
            var matches = Find(options);
            if (matches.Length == 0 && options.LaunchIfMissing && !apps.IsRunning(options.ApplicationPath))
            {
                token.ThrowIfCancellationRequested();
                if (!apps.Launch(options.ApplicationPath)) return Fail(bounds, "无法启动指定应用，请确认程序仍在原位置且能正常打开。已保留隐私幕。");
                launched = true;
                options.WindowToken = null;
                for (int i = 0; i < 50 && matches.Length == 0; i++)
                {
                    await Task.Delay(100, token);
                    matches = Find(options);
                }
            }
            if (matches.Length == 0) return Fail(bounds, "指定应用未运行或原窗口已关闭，已保留隐私幕。");
            if (matches.Length != 1) return Fail(bounds, "存在多个匹配窗口，请重新选择具体窗口。已保留隐私幕。");
            var target = matches[0];
            if (target.Owner != 0) return Fail(bounds, "指定窗口是附属窗口，已保留隐私幕。");
            if (!bounds.Contains(target.Bounds) && !options.MoveFromOtherScreen && !launched) return Fail(bounds, "指定应用位于其他屏幕，尚未允许移动。已保留隐私幕。");
            // Recheck the complete family on every snapshot, including immediately before minimization.
            var excluded = new ExcludingAccess(desktop, apps, target);
            if (!await new ScreenDesktopService(excluded).ShowDesktopAsync(bounds, token)) return Fail(bounds, "本屏部分窗口无法安全隐藏，已保留隐私幕。");
            token.ThrowIfCancellationRequested();
            // Revalidate identity and permission after asynchronous minimization.
            var current = Find(options);
            if (current.Length != 1 || current[0].Token != target.Token || current[0].Owner != 0 ||
                (!bounds.Contains(current[0].Bounds) && !options.MoveFromOtherScreen && !launched))
                return Fail(bounds, ChangedFailure);
            if (!apps.Place(target, bounds)) return Fail(bounds, "无法调整指定应用的窗口位置，请检查应用权限或窗口状态。已保留隐私幕。");
            for (int i = 0; i < 20; i++)
            {
                await Task.Delay(100, token);
                if (!apps.Covers(target, bounds))
                {
                    // A minimized window must process restoration before its frame can be positioned.
                    if (!apps.Place(target, bounds)) return Fail(bounds, "无法调整指定应用的窗口位置，请检查应用权限或窗口状态。已保留隐私幕。");
                    continue;
                }
                if (excluded.EnumerateVisibleWindows().Any(w => DesktopWindowPolicy.OverlapsScreen(w, bounds)))
                    return Fail(bounds, "切换后本屏仍有其他可见窗口，已保留隐私幕。");
                _failures.Remove(bounds);
                return true;
            }
            return Fail(bounds, "指定应用未能完整覆盖本屏，已保留隐私幕。");
        }
        catch (WindowFamilyChangedException ex) { return Fail(bounds, ex.Message); }
        finally { _serial.Release(); }
    }
    private ApplicationWindow[] Find(PrivacyOptions options)
    {
        var candidates = apps.Enumerate().Where(w => string.Equals(w.Path, options.ApplicationPath, StringComparison.OrdinalIgnoreCase) &&
            (string.IsNullOrEmpty(options.WindowClass) || w.ClassName == options.WindowClass)).ToArray();
        var exact = candidates.Where(w => w.Token == options.WindowToken).ToArray();
        return options.WindowToken != null ? exact : candidates;
    }
    private sealed class WindowFamilyChangedException(string reason) : Exception(reason) { }
    private sealed class ExcludingAccess(IDesktopWindowAccess inner, IApplicationWindowAccess applications, ApplicationWindow target) : IDesktopWindowAccess
    {
        public IReadOnlyList<DesktopWindow> EnumerateVisibleWindows()
        {
            var desktop = inner.EnumerateVisibleWindows();
            var snapshot = applications.Enumerate();
            var current = snapshot.SingleOrDefault(w => w.Token == target.Token && w.Handle == target.Handle && w.Owner == 0);
            if (current == null) throw new WindowFamilyChangedException(ChangedFailure);
            var family = snapshot.Where(w => ApplicationWindowFamilyPolicy.IsCompanion(current, w)).Select(w => w.Handle).ToHashSet();
            family.Add(target.Handle);
            // A real popup, an uninspectable owned window or a new child of the chrome still blocks release.
            if (snapshot.Any(w => family.Contains(w.Owner) && !family.Contains(w.Handle)) ||
                desktop.Any(w => family.Contains(w.Owner) && !family.Contains(w.Handle)))
                throw new WindowFamilyChangedException(PopupFailure);
            return desktop.Where(w => !family.Contains(w.Handle)).ToArray();
        }
        public bool TryMinimize(DesktopWindow window, Rect screen)
        {
            var current = EnumerateVisibleWindows().FirstOrDefault(w => w.Handle == window.Handle);
            return current == null || inner.TryMinimize(current, screen);
        }
    }
}
