namespace OLED_Sleeper.Features.MonitorPrivacy;

public enum PrivacyMode { None, Curtain, Desktop, Application }

/// <summary>Monotonic, per-screen privacy timing; ordinary activity never dismisses a curtain.</summary>
public sealed class PrivacyTimerState(long now)
{
    public long LastActivity { get; private set; } = now;
    public long PausedUntil { get; private set; }
    public bool IsProtected { get; private set; }
    public bool RequiresExplicitRelease { get; private set; }
    public void Activity(long now)
    {
        LastActivity = Math.Max(now, PausedUntil);
        if (IsProtected && !RequiresExplicitRelease) IsProtected = false;
    }
    public bool IsDue(long now, int seconds) =>
        !IsProtected && now >= PausedUntil && now - LastActivity >= seconds * 1000L;
    public void Defer(long now) => LastActivity = Math.Max(LastActivity, now);
    public void RestartWaiting(long now) => LastActivity = now;
    public void Protect() { IsProtected = true; RequiresExplicitRelease = true; }
    public void DesktopReady() { RequiresExplicitRelease = false; }
    public void Release(long now) { IsProtected = false; RequiresExplicitRelease = false; LastActivity = now; }
    public void Pause(long now, int seconds = 900) { PausedUntil = now + Math.Clamp(seconds, 60, 86400) * 1000L; LastActivity = PausedUntil; }
    public void Resume(long now) { PausedUntil = 0; LastActivity = now; }
}
