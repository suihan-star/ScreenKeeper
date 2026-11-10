namespace OLED_Sleeper.Features.ColorTemperature;

public sealed partial class ColorTemperatureService
{
    private const long ResumeStableMilliseconds = 2000;
    private const string ResumeWaitingMessage = "正在等待屏幕连接和原色读数稳定。";
    private readonly object _suspensionSync = new();
    private long _suspensionRevision;
    private bool _resumeCapturePending;
    private readonly Dictionary<string, ResumeCheck> _resumeChecks = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _resumeTransitions = new(StringComparer.OrdinalIgnoreCase);

    private sealed class ResumeCheck(ColorRecoveryRecord record)
    {
        public ColorRecoveryRecord Record { get; } = record.Copy();
        public ColorValue? Sample { get; set; }
        public long SampleAt { get; set; }
        public void Reset() => Sample = null;
    }

    private void ArmResumeRecovery()
    {
        _resumeChecks.Clear();
        _resumeTransitions.Clear();
        if (!_journalSafe) return;
        foreach (var record in _journal.Records.Where(CanObserveResume))
            _resumeChecks[record.Scope.HardwareId] = new(record);
    }

    private bool CanObserveResume(ColorRecoveryRecord record) => _journalSafe && record.SessionId == _session &&
        !record.Conflicted && record.PendingValue == null && !_failed.Contains(record.Scope.HardwareId) && !_conflicts.Contains(record.Scope.HardwareId);

    private void ResetResumeSamples()
    {
        // Confirmation may already have completed while the independent read is still awaiting
        // its result. A new settings/cancel request must invalidate that confirmation as well.
        foreach (var record in _journal.Records.Where(r => _resumeTransitions.Contains(r.Scope.HardwareId) && CanObserveResume(r)))
            _resumeChecks[record.Scope.HardwareId] = new(record);
        _resumeTransitions.Clear();
        foreach (var check in _resumeChecks.Values) check.Reset();
    }

    private void RetryResumeRead(ColorDisplayScope scope, ColorRecoveryRecord record)
    {
        _resumeTransitions.Remove(scope.HardwareId);
        _resumeChecks[scope.HardwareId] = new(record);
        Set(scope, ColorControlState.Detecting, ResumeWaitingMessage);
    }

    private static bool MatchesResumeRecord(ColorRecoveryRecord? record, ResumeCheck check) =>
        record != null && !record.Conflicted && record.PendingValue == null && record.SessionId == check.Record.SessionId &&
        WindowsGammaBackend.SameScope(record.Scope, check.Record.Scope) && record.Baseline.SameAs(check.Record.Baseline) &&
        record.LastApplied.SameAs(check.Record.LastApplied);

    private bool ResumeRequestIsCurrent(long revision, long previewRevision, long suspensionRevision) =>
        !_stopped && !_suspended && Volatile.Read(ref _suspendRequested) == 0 && revision == Volatile.Read(ref _revision) &&
        previewRevision == Volatile.Read(ref _previewRevision) && suspensionRevision == Volatile.Read(ref _suspensionRevision);

    private async Task<bool> ObserveResumeAsync(ColorCapability capability, ColorRecoveryRecord? record, long revision,
        long previewRevision, CancellationToken cancellationToken)
    {
        var scope = capability.Scope;
        if (!_resumeChecks.TryGetValue(scope.HardwareId, out var check)) return true;
        if (!MatchesResumeRecord(record, check) || _failed.Contains(scope.HardwareId))
        {
            // An explicit recovery or a failed pending write invalidates the old observation.
            _resumeChecks.Remove(scope.HardwareId);
            return true;
        }
        if (capability.State != ColorCapabilityState.Supported || !WindowsGammaBackend.SameScope(scope, check.Record.Scope))
        {
            check.Reset();
            Set(scope, ColorControlState.RecoveryPending, "显示模式或能力已改变，保留恢复记录。");
            return false;
        }

        long suspensionRevision = Volatile.Read(ref _suspensionRevision);
        Set(scope, ColorControlState.Detecting, ResumeWaitingMessage);
        try
        {
            var current = await _backend.ReadAsync(scope, cancellationToken).ConfigureAwait(false);
            if (!ResumeRequestIsCurrent(revision, previewRevision, suspensionRevision)) { check.Reset(); return false; }
            if (current is not { IsValid: true }) { check.Reset(); return false; }
            if (!current.SameAs(check.Record.Baseline) && !current.SameAs(check.Record.LastApplied))
            {
                Conflict(record!);
                return false;
            }
            long now = _monotonic();
            if (!current.SameAs(check.Sample) || now < check.SampleAt)
            {
                check.Sample = current.Copy();
                check.SampleAt = now;
                return false;
            }
            if (now - check.SampleAt < ResumeStableMilliseconds) return false;

            // Recheck qualification after the observation read; discovery may race a reconnect.
            if (!await DiscoverAsync(cancellationToken).ConfigureAwait(false) ||
                !ResumeRequestIsCurrent(revision, previewRevision, suspensionRevision) || check.Sample == null ||
                !Capabilities.Any(c => c.State == ColorCapabilityState.Supported && WindowsGammaBackend.SameScope(c.Scope, scope)))
            {
                check.Reset();
                return false;
            }
            if (current.SameAs(check.Record.Baseline) && !current.SameAs(check.Record.LastApplied))
            {
                // Retain the known baseline. Removing the record would let a later external color
                // become a new baseline in the ordinary first-takeover path.
                var restored = record!.Copy();
                restored.LastApplied = restored.Baseline.Copy();
                if (!Put(restored))
                {
                    check.Reset();
                    Set(scope, ColorControlState.Detecting, "无法保存色温恢复记录，未继续调色。");
                    return false;
                }
                check.Record.LastApplied = restored.LastApplied.Copy();
                if (LastError == "无法保存色温恢复记录，未继续调色。") LastError = null;
            }
            if (!ResumeRequestIsCurrent(revision, previewRevision, suspensionRevision)) { check.Reset(); return false; }
            _resumeChecks.Remove(scope.HardwareId);
            _resumeTransitions.Add(scope.HardwareId);
            _transitions.Remove(scope.HardwareId);
            _previewChangedScreens.Remove(scope.HardwareId);
            // The normal path performs an independent read and still rejects unknown colors.
            return true;
        }
        catch (OperationCanceledException) { check.Reset(); throw; }
        catch (Exception)
        {
            check.Reset();
            Set(scope, ColorControlState.Detecting, ResumeWaitingMessage);
            return false;
        }
    }
}
