namespace OLED_Sleeper.Features.ColorTemperature
{
    public enum ColorCapabilityState { Unverified, Supported, Unsupported, Offline, DetectionFailed }
    public enum ColorControlState { Disabled, Detecting, Original, Transitioning, Warm, Paused, Offline, Unsupported, Unverified, Conflict, Failed, RecoveryPending }

    // ScopeId is an immutable physical target path; DeviceName is only an ephemeral API address.
    public sealed record ColorDisplayScope(string HardwareId, string ScopeId, string DeviceName,
        string DisplayName, string TopologyFingerprint, string ModeFingerprint);

    public sealed record ColorCapability(ColorDisplayScope Scope, ColorCapabilityState State,
        string Reason, bool? AdvancedColorEnabled = null, bool IsCloned = false, bool GammaReadable = false,
        int MinimumKelvin = 3500, int MaximumKelvin = 6500)
    {
        public bool CanValidate => State == ColorCapabilityState.Unverified && AdvancedColorEnabled == false && !IsCloned && GammaReadable &&
            !string.IsNullOrWhiteSpace(Scope.HardwareId) && !string.IsNullOrWhiteSpace(Scope.ScopeId) &&
            !string.IsNullOrWhiteSpace(Scope.TopologyFingerprint) && !string.IsNullOrWhiteSpace(Scope.ModeFingerprint);
    }

    public sealed record ColorScreenState(string HardwareId, ColorControlState State, string Message,
        DateTimeOffset? PausedUntil = null, double Warmth = 0);

    public sealed class ColorValue
    {
        public string Backend { get; set; } = "Gamma";
        public ushort[] Channels { get; set; } = [];
        public ColorValue Copy() => new() { Backend = Backend, Channels = [.. Channels] };
        public bool IsValid => Backend == "Gamma" && Channels is { Length: 768 };
        public bool SameAs(ColorValue? other) => other != null && Backend == other.Backend && Channels.SequenceEqual(other.Channels);
    }

    public interface IColorTemperatureBackend
    {
        Task<IReadOnlyList<ColorCapability>> DiscoverAsync(CancellationToken cancellationToken);
        Task<ColorValue?> ReadAsync(ColorDisplayScope scope, CancellationToken cancellationToken);
        Task<bool> WriteAsync(ColorDisplayScope scope, ColorValue value, CancellationToken cancellationToken);
        Task<bool> RestoreAsync(ColorDisplayScope scope, ColorValue baseline, CancellationToken cancellationToken) => WriteAsync(scope, baseline, cancellationToken);
        ColorValue CreateTarget(ColorValue baseline, int kelvin, double warmth);
    }

    public interface IColorTrialBackend : IColorTemperatureBackend
    {
        IDisposable AuthorizeTrial(ColorDisplayScope scope);
    }

    public sealed class ColorRecoveryRecord
    {
        public ColorDisplayScope Scope { get; set; } = new("", "", "", "", "", "");
        public string SessionId { get; set; } = "";
        public ColorValue Baseline { get; set; } = new();
        public ColorValue LastApplied { get; set; } = new();
        public ColorValue? PendingValue { get; set; }
        public bool Conflicted { get; set; }
        public ColorRecoveryRecord Copy() => new()
        {
            Scope = Scope with { },
            SessionId = SessionId,
            Baseline = Baseline.Copy(),
            LastApplied = LastApplied.Copy(),
            PendingValue = PendingValue?.Copy(),
            Conflicted = Conflicted
        };
    }

    public sealed class ColorRecoveryDocument
    {
        public int Schema { get; set; } = 1;
        public List<ColorRecoveryRecord> Records { get; set; } = [];
        public Dictionary<string, DateTimeOffset> PausedUntil { get; set; } = new(StringComparer.OrdinalIgnoreCase);
        public List<string> ValidationRequiredScreens { get; set; } = [];
        public ColorRecoveryDocument Copy() => new()
        {
            Schema = Schema,
            Records = Records.Select(r => r.Copy()).ToList(),
            PausedUntil = new(PausedUntil, StringComparer.OrdinalIgnoreCase),
            ValidationRequiredScreens = [.. ValidationRequiredScreens]
        };
    }

    public sealed record ColorRecoveryReadResult(ColorRecoveryDocument Document, bool IsSafe, string? Error = null);
    public sealed record ColorBaselineObservation(ColorDisplayScope Scope, ColorValue Value)
    {
        public ColorBaselineObservation Copy() => new(Scope with { }, Value.Copy());
    }

    // Audit evidence only. Archives are never a source for automatic device recovery.
    public sealed class ColorRecoveryArchive
    {
        public int Schema { get; set; } = 1;
        public DateTimeOffset ConfirmedUtc { get; set; }
        public string Reason { get; set; } = "UserConfirmedCurrentColors";
        public ColorRecoveryDocument Previous { get; set; } = new();
        public List<ColorBaselineObservation> ConfirmedColors { get; set; } = [];
        public ColorRecoveryArchive Copy() => new()
        {
            Schema = Schema,
            ConfirmedUtc = ConfirmedUtc,
            Reason = Reason,
            Previous = Previous.Copy(),
            ConfirmedColors = ConfirmedColors.Select(c => c.Copy()).ToList()
        };
    }

    public interface IColorRecoveryStore
    {
        ColorRecoveryReadResult Read();
        bool TryWrite(ColorRecoveryDocument document);
        bool TryArchive(ColorRecoveryArchive archive) => false;
    }
}
