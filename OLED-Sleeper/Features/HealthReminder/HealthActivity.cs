namespace OLED_Sleeper.Features.HealthReminder;

public interface IHealthClock
{
    TimeSpan Elapsed { get; }
    DateTimeOffset UtcNow { get; }
}

public sealed class SystemHealthClock : IHealthClock
{
    // TickCount64 is monotonic and includes suspended time, which an active break may count.
    public TimeSpan Elapsed => TimeSpan.FromMilliseconds(Environment.TickCount64);
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}

public sealed record HealthScreen(string DeviceName, int Left, int Top, int Right, int Bottom)
{
    public int FullLeft { get; init; } = Left;
    public int FullTop { get; init; } = Top;
    public int FullRight { get; init; } = Right;
    public int FullBottom { get; init; } = Bottom;
}

/// <summary>No key values, window titles, process names, or screen contents are collected.</summary>
public sealed record HealthActivitySnapshot(
    bool InputAvailable,
    TimeSpan IdleFor,
    bool SessionAvailable,
    bool FullscreenKnown,
    bool IsFullscreen,
    HealthScreen? ForegroundScreen)
{
    public static HealthActivitySnapshot Unavailable { get; } = new(false, TimeSpan.Zero, false, false, false, null);
}

public interface IUserActivitySnapshot : IDisposable
{
    event Action? Changed;
    void Start();
    void Stop();
    HealthActivitySnapshot Read();
}

public enum HealthReminderState { Disabled, Working, Deferred, Reminder, Snoozed, Breaking, Paused }

public sealed record HealthReminderSnapshot(
    HealthReminderState State,
    string ReasonKey,
    TimeSpan Remaining,
    TimeSpan WorkElapsed,
    DateTimeOffset? PauseUntil,
    bool IsManuallyPaused,
    bool CanPresent,
    bool TimerEnded,
    string? CurrentScreenDeviceName = null);
