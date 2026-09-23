using OLED_Sleeper.Storage;

namespace OLED_Sleeper.Features.ColorTemperature;

public sealed class ColorValidationDocument
{
    public int Schema { get; set; } = 1;
    public List<ColorValidationRecord> Screens { get; set; } = [];
    public string? Validate() => Schema != 1 || Screens == null || Screens.Any(s => s == null || s.Scope == null ||
        string.IsNullOrWhiteSpace(s.Scope.HardwareId) || string.IsNullOrWhiteSpace(s.Scope.ScopeId) ||
        string.IsNullOrWhiteSpace(s.Scope.TopologyFingerprint) || string.IsNullOrWhiteSpace(s.Scope.ModeFingerprint) || s.Protocol != 1)
        ? "色温验证记录不可读取，请保留原文件并重试。" : null;
}

public sealed record ColorValidationRecord(ColorDisplayScope Scope, DateTimeOffset ConfirmedUtc, int Protocol = 1);

/// <summary>No Boolean override: only the service's completed observation workflow writes exact-scope evidence.</summary>
public sealed class ColorValidationRegistry : IColorValidationRegistry
{
    private readonly IndependentSettingsStore<ColorValidationDocument> _store;
    private ColorValidationDocument _document;
    private readonly object _sync = new();
    public ColorValidationRegistry(IndependentSettingsStore<ColorValidationDocument> store) { _store = store; _document = store.Load(); }
    public bool IsAvailable => _store.IsAvailable;
    public bool IsValidated(ColorDisplayScope scope)
    {
        lock (_sync) return IsAvailable && _document.Screens.Any(s => s.Protocol == 1 && WindowsGammaBackend.SameScope(s.Scope, scope));
    }
    public bool TryConfirm(ColorDisplayScope scope)
    {
        lock (_sync)
        {
            if (!IsAvailable) return false;
            var next = new ColorValidationDocument { Screens = _document.Screens.Where(s => !s.Scope.HardwareId.Equals(scope.HardwareId, StringComparison.OrdinalIgnoreCase)).ToList() };
            next.Screens.Add(new(scope with { }, DateTimeOffset.UtcNow));
            if (!_store.TrySave(next)) return false;
            _document = next;
            return true;
        }
    }

    public bool TryRevoke(IReadOnlyCollection<string> hardwareIds)
    {
        lock (_sync)
        {
            if (!IsAvailable || hardwareIds.Count == 0 || hardwareIds.Any(string.IsNullOrWhiteSpace)) return false;
            var ids = hardwareIds.ToHashSet(StringComparer.OrdinalIgnoreCase);
            var next = new ColorValidationDocument { Screens = _document.Screens.Where(s => !ids.Contains(s.Scope.HardwareId)).ToList() };
            if (!_store.TrySave(next)) return false;
            _document = next;
            return true;
        }
    }
}
