using OLED_Sleeper.Storage.Interfaces;
using System.IO;
using System.Text.Json;

namespace OLED_Sleeper.Features.ColorTemperature
{
    public sealed class ColorRecoveryFileStore(IAppDataFileStore store, IFileSystem files, IStorageRoot root) : IColorRecoveryStore
    {
        public const string FileName = "color-temperature-recovery.json";
        private bool _readOnly;

        public ColorRecoveryReadResult Read()
        {
            string path = Path.Combine(root.DirectoryPath, FileName);
            try
            {
                if (!files.FileExists(path) && !files.FileExists(path + ".bak")) return new(new(), true);
                if (files.FileExists(path))
                {
                    using var json = JsonDocument.Parse(files.ReadAllText(path));
                    if (json.RootElement.ValueKind == JsonValueKind.Object &&
                        json.RootElement.TryGetProperty("Schema", out var schema) && schema.TryGetInt32(out int version) && version == 1 &&
                        json.RootElement.TryGetProperty("Records", out var records) && records.ValueKind == JsonValueKind.Array)
                    {
                        var primary = json.RootElement.Deserialize<ColorRecoveryDocument>();
                        if (Valid(primary)) return new(primary!, true);
                    }
                }
            }
            catch (Exception) { /* An unreadable journal cannot be interpreted as an empty journal. */ }

            _readOnly = true;
            ColorRecoveryDocument recovered;
            try { recovered = store.Read<ColorRecoveryDocument>(FileName) is { } value && Valid(value) ? value : new(); }
            catch (Exception) { recovered = new(); }
            // A backup may precede the first device write. Keep it for inspection, never use it as evidence that no takeover occurred.
            return new(recovered, false, "色温恢复记录不可确认，已停止接管。请保留文件并检查备份。");
        }

        public bool TryWrite(ColorRecoveryDocument document) => !_readOnly && Valid(document) && store.TryWriteDurable(FileName, document.Copy());

        public bool TryArchive(ColorRecoveryArchive archive)
        {
            if (_readOnly || archive.Schema != 1 || archive.Reason != "UserConfirmedCurrentColors" || !Valid(archive.Previous) ||
                archive.Previous.Records.Count == 0 || archive.ConfirmedColors == null || archive.ConfirmedColors.Count != archive.Previous.Records.Count ||
                archive.ConfirmedColors.Any(c => !ValidObservation(c, archive.Previous)) ||
                archive.ConfirmedColors.Select(c => c.Scope.HardwareId).Distinct(StringComparer.OrdinalIgnoreCase).Count() != archive.ConfirmedColors.Count) return false;
            string fileName = $"color-temperature-archive-{Guid.NewGuid():N}.json";
            if (files.FileExists(Path.Combine(root.DirectoryPath, fileName))) return false;
            return store.TryWriteDurable(fileName, archive.Copy());
        }

        // An archive keeps both old and newly observed scopes as evidence. It never restores a ramp,
        // so reconnecting the same physical display must not require the old mode or API address.
        private static bool ValidObservation(ColorBaselineObservation? observation, ColorRecoveryDocument previous) =>
            observation?.Scope is { } scope && observation.Value is { IsValid: true } &&
            !string.IsNullOrWhiteSpace(scope.HardwareId) && !string.IsNullOrWhiteSpace(scope.ScopeId) &&
            !string.IsNullOrWhiteSpace(scope.DeviceName) && !string.IsNullOrWhiteSpace(scope.TopologyFingerprint) &&
            !string.IsNullOrWhiteSpace(scope.ModeFingerprint) && previous.Records.Count(r =>
                r.Scope.HardwareId.Equals(scope.HardwareId, StringComparison.OrdinalIgnoreCase) &&
                r.Scope.ScopeId.Equals(scope.ScopeId, StringComparison.OrdinalIgnoreCase)) == 1;

        private static bool Valid(ColorRecoveryDocument? value) => value is { Schema: 1, Records: not null, PausedUntil: not null, ValidationRequiredScreens: not null } &&
            value.Records.All(r => r != null && r.Scope != null && !string.IsNullOrWhiteSpace(r.Scope.HardwareId) &&
                !string.IsNullOrWhiteSpace(r.Scope.ScopeId) && !string.IsNullOrWhiteSpace(r.Scope.TopologyFingerprint) &&
                !string.IsNullOrWhiteSpace(r.Scope.ModeFingerprint) && r.Baseline is { IsValid: true } &&
                r.LastApplied is { IsValid: true } && (r.PendingValue == null || r.PendingValue.IsValid)) &&
            value.Records.Select(r => r.Scope.ScopeId).Distinct(StringComparer.OrdinalIgnoreCase).Count() == value.Records.Count &&
            value.PausedUntil.Keys.All(k => !string.IsNullOrWhiteSpace(k)) && value.ValidationRequiredScreens.All(id => !string.IsNullOrWhiteSpace(id)) &&
            value.ValidationRequiredScreens.Distinct(StringComparer.OrdinalIgnoreCase).Count() == value.ValidationRequiredScreens.Count;
    }
}
