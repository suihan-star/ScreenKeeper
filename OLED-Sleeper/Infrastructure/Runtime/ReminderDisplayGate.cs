using System.Collections.Concurrent;

namespace OLED_Sleeper.Infrastructure.Runtime;

/// <summary>Read-only reminder eligibility; never restores or wakes a display.</summary>
public sealed class ReminderDisplayGate
{
    private readonly ConcurrentDictionary<string, byte> _sleeping = new(StringComparer.OrdinalIgnoreCase);
    public event Action? Unavailable;
    public bool IsSleeping(string id) => _sleeping.ContainsKey(id);
    public void Sleep(string id) { _sleeping[id] = 0; Unavailable?.Invoke(); }
    public void Awake(string id) => _sleeping.TryRemove(id, out _);
}
