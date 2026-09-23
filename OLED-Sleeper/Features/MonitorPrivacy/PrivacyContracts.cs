using OLED_Sleeper.Features.MonitorInformation.Models;
using OLED_Sleeper.Features.UserSettings.Models;
using System.Windows;

namespace OLED_Sleeper.Features.MonitorPrivacy;

public interface IPrivacyActivitySource : IDisposable
{
    event Action<Point>? Activity;
    void Start();
    void Stop();
    PrivacyUsageSnapshot ReadUsage() => new(null, null, false, true);
}

public sealed record PrivacyUsageSnapshot(Point? Cursor, Rect? Foreground, bool RecentInput, bool SessionActive);
public enum PrivacyRuntimeState { Off, Waiting, InUse, Quiet, Paused, Protected, Unavailable }
public sealed record PrivacyRuntimeSnapshot(PrivacyRuntimeState State, int RemainingSeconds, string Message);

public interface IPrivacyCurtainHost : IDisposable
{
    void Show(string id, Rect bounds, Action release, bool desktopPending);
    void ConfigureAppearance(string id, PrivacyOptions options) { }
    void Move(string id, Rect bounds);
    void Close(string id);
    void DesktopFailed(string id);
    void DesktopReady(string id);
    bool SetSleeping(string id, bool sleeping);
    bool IsCurtainWindow(nint handle);
}

public interface IScreenDesktopService
{
    Task<bool> ShowDesktopAsync(Rect bounds, CancellationToken cancellationToken = default);
}

public interface IMonitorPrivacyService
{
    event Action? Changed;
    bool HasConfiguredScreens { get; }
    bool IsConfigured(string id);
    bool IsPaused(string id);
    bool IsProtected(string id) => false;
    string GetStatus(string? id);
    PrivacyRuntimeSnapshot GetSnapshot(string? id);
    Task ConfigureAsync(List<MonitorSettings> settings, IReadOnlyList<MonitorInfo> monitors);
    void Start();
    void Stop();
    Task ProtectAllAsync();
    Task ProtectScreenAsync(string id);
    Task PrepareForSleepAsync(string id);
    async Task<bool> TryPrepareForSleepAsync(string id, Func<bool> stillIdle)
    {
        if (!stillIdle()) return false;
        await PrepareForSleepAsync(id);
        return stillIdle();
    }
    void TogglePause(string id);
    void Pause(string id, int seconds) => TogglePause(id);
}
