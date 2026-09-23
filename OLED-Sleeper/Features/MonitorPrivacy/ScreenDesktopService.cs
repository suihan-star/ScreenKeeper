using Serilog;
using System.Windows;

namespace OLED_Sleeper.Features.MonitorPrivacy;

public sealed record DesktopWindow(nint Handle, Rect Bounds, bool HasMinimizeButton, nint Owner = default);

public static class DesktopWindowPolicy
{
    public static Rect MaximizedVisibleBounds(Rect frame, Rect monitor, int borderX, int borderY)
    {
        if (borderX <= 0 || borderY <= 0) return frame;
        var maximumFrame = monitor;
        maximumFrame.Inflate(borderX, borderY);
        // Normalize only a maximized frame within the OS-reported resize border allowance.
        // A custom maximized window genuinely spanning monitors must remain a blocker.
        return maximumFrame.Contains(frame) ? Rect.Intersect(frame, monitor) : frame;
    }

    public static Rect VisibleBounds(Rect window, IReadOnlyList<Rect> screens)
    {
        var visible = Rect.Empty;
        foreach (var screen in screens)
        {
            var intersection = Rect.Intersect(window, screen);
            if (intersection.IsEmpty || intersection.Width <= 0 || intersection.Height <= 0) continue;
            visible.Union(intersection);
        }
        return visible;
    }

    public static bool FitsScreen(DesktopWindow window, Rect screen) => screen.Contains(window.Bounds);

    public static bool OverlapsScreen(DesktopWindow window, Rect screen)
    {
        var overlap = Rect.Intersect(window.Bounds, screen);
        return !overlap.IsEmpty && overlap.Width > 0 && overlap.Height > 0;
    }
}

/// <summary>Minimize only windows contained on this screen, behind an already visible curtain.</summary>
public sealed class ScreenDesktopService(IDesktopWindowAccess windows) : IScreenDesktopService
{
    public async Task<bool> ShowDesktopAsync(Rect bounds, CancellationToken cancellationToken = default)
    {
        var requested = new HashSet<nint>();
        var failedRequests = 0;
        for (var attempt = 0; attempt <= 15; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var snapshot = windows.EnumerateVisibleWindows();
            var targets = snapshot.Where(w => DesktopWindowPolicy.OverlapsScreen(w, bounds)).ToArray();
            if (targets.Length == 0) return true;
            if (targets.Any(w => !DesktopWindowPolicy.FitsScreen(w, bounds)))
            {
                Log.Information("Desktop privacy retained curtain: a visible window spans the target screen boundary.");
                return false;
            }

            var byHandle = snapshot.ToDictionary(w => w.Handle);
            var targetHandles = targets.Select(w => w.Handle).ToHashSet();
            // Minimizing an owner also hides its owned windows. Do not change another screen this way.
            if (snapshot.Any(w => !targetHandles.Contains(w.Handle) && HasOwnerIn(w, targetHandles, byHandle)))
            {
                Log.Information("Desktop privacy retained curtain: an owned window is on another screen.");
                return false;
            }
            if (attempt == 15)
            {
                Log.Information("Desktop privacy retained curtain after verification: {RemainingCount} visible windows, {FailedRequestCount} rejected minimize requests.", targets.Length, failedRequests);
                return false;
            }

            // WS_MINIMIZEBOX describes a title-bar button, not whether ShowWindowAsync can minimize.
            // Give owners one message-loop interval first so their popups can hide automatically.
            var pending = targets.Where(w => !requested.Contains(w.Handle)).Select(w => w.Handle).ToHashSet();
            foreach (var target in targets.Where(w => pending.Contains(w.Handle) && !HasOwnerIn(w, pending, byHandle)))
            {
                cancellationToken.ThrowIfCancellationRequested();
                requested.Add(target.Handle);
                if (!windows.TryMinimize(target, bounds)) failedRequests++;
            }
            // A successful API call only queues the operation. Disappearance, not the return code,
            // is the success condition; a concurrently closed window also satisfies it.
            await Task.Delay(100, cancellationToken);
        }
        return false;
    }

    private static bool HasOwnerIn(DesktopWindow window, HashSet<nint> handles, Dictionary<nint, DesktopWindow> snapshot)
    {
        var visited = new HashSet<nint> { window.Handle };
        var owner = window.Owner;
        while (owner != 0 && visited.Add(owner))
        {
            if (handles.Contains(owner)) return true;
            if (!snapshot.TryGetValue(owner, out var parent)) break;
            owner = parent.Owner;
        }
        return false;
    }
}
