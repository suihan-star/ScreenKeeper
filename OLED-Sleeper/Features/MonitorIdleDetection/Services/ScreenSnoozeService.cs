using OLED_Sleeper.Features.MonitorBehavior.Models;
using OLED_Sleeper.Features.MonitorInformation.Models;
using OLED_Sleeper.Features.UserSettings.Models;

namespace OLED_Sleeper.Features.MonitorIdleDetection.Services;

/// <summary>Session-only monotonic deadlines shared by UI, privacy and the background rest loop.</summary>
public sealed class ScreenSnoozeService(TimeProvider? clock = null)
{
    private readonly TimeProvider _clock = clock ?? TimeProvider.System;
    private readonly object _gate = new();
    private readonly Dictionary<string, Entry> _entries = new(StringComparer.OrdinalIgnoreCase);
    private sealed class Entry
    {
        public string Name = "";
        public bool Rest, Privacy;
        public long RestUntil, PrivacyUntil, RestFrom, PrivacyFrom;
    }
    public event Action? Changed;
    public long Now => (long)_clock.GetElapsedTime(0, _clock.GetTimestamp()).TotalMilliseconds;
    public IReadOnlyList<(string Id, string Name)> Screens { get { lock (_gate) return _entries.Select(e => (e.Key, e.Value.Name)).ToArray(); } }
    public void Configure(IReadOnlyList<MonitorSettings> settings, IReadOnlyList<MonitorInfo> monitors)
    {
        lock (_gate)
        {
            var keep = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var monitor in monitors)
            {
                var setting = settings.FirstOrDefault(s => s.HardwareId == monitor.HardwareId);
                if (setting == null) continue;
                keep.Add(monitor.HardwareId);
                if (!_entries.TryGetValue(monitor.HardwareId, out var entry)) _entries[monitor.HardwareId] = entry = new();
                entry.Name = UI.Helpers.MonitorTitleFormatter.Format(monitor);
                entry.Rest = setting.IsManaged && setting.Behavior != MonitorBehaviorType.None;
                entry.Privacy = setting.HasPrivacy;
                if (!entry.Rest) entry.RestUntil = entry.RestFrom = 0;
                if (!entry.Privacy) entry.PrivacyUntil = entry.PrivacyFrom = 0;
            }
            foreach (var id in _entries.Keys.Where(id => !keep.Contains(id)).ToArray()) _entries.Remove(id);
        }
        Changed?.Invoke();
    }
    public bool IsEnabled(string id, MonitorSettingsScope scope) { lock (_gate) return _entries.TryGetValue(id, out var e) && (scope == MonitorSettingsScope.Privacy ? e.Privacy : e.Rest); }
    public long Until(string id, MonitorSettingsScope scope) { lock (_gate) return _entries.TryGetValue(id, out var e) ? scope == MonitorSettingsScope.Privacy ? e.PrivacyUntil : e.RestUntil : 0; }
    public bool IsPaused(string id, MonitorSettingsScope scope) => Until(id, scope) > Now;
    public bool IsJoint(string id) { lock (_gate) return _entries.TryGetValue(id, out var e) && e.RestUntil > Now && e.RestUntil == e.PrivacyUntil; }
    public long WaitFrom(string id, MonitorSettingsScope scope)
    {
        lock (_gate) return _entries.TryGetValue(id, out var e) ? scope == MonitorSettingsScope.Privacy ? Math.Max(e.PrivacyUntil, e.PrivacyFrom) : Math.Max(e.RestUntil, e.RestFrom) : 0;
    }
    public void Pause(string id, MonitorSettingsScope scope, int seconds, bool both = false)
    {
        if (seconds is < 60 or > 86400) return;
        lock (_gate)
        {
            if (!_entries.TryGetValue(id, out var e)) return;
            long until = Now + seconds * 1000L;
            if ((scope == MonitorSettingsScope.Rest || both) && e.Rest) e.RestUntil = until;
            if ((scope == MonitorSettingsScope.Privacy || both) && e.Privacy) e.PrivacyUntil = until;
        }
        Changed?.Invoke();
    }
    public void Resume(string id, MonitorSettingsScope scope, bool both = false)
    {
        lock (_gate)
        {
            if (!_entries.TryGetValue(id, out var e)) return;
            var now = Now;
            if (scope == MonitorSettingsScope.Rest || both) { e.RestUntil = 0; e.RestFrom = now; }
            if (scope == MonitorSettingsScope.Privacy || both) { e.PrivacyUntil = 0; e.PrivacyFrom = now; }
        }
        Changed?.Invoke();
    }
    public void RestartWaiting()
    {
        lock (_gate) foreach (var e in _entries.Values) e.RestFrom = e.PrivacyFrom = Now;
    }
}
