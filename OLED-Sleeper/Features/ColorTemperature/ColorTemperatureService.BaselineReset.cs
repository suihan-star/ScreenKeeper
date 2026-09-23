namespace OLED_Sleeper.Features.ColorTemperature;

public sealed record ColorBaselineResetCheck(bool Ready, string Message, Guid Ticket = default, IReadOnlyList<string>? DisplayNames = null);

public sealed partial class ColorTemperatureService
{
    private BaselineResetProof? _baselineResetProof;
    private long _baselineResetRevision;
    public bool HasRecoveryRecords => _journal.Records.Count > 0;
    public IReadOnlyList<ColorDisplayScope> RecoveryScopes => _journal.Records.Select(r => r.Scope with { }).ToArray();

    public async Task<ColorBaselineResetCheck> PrepareBaselineResetAsync(CancellationToken cancellationToken = default,
        Func<TimeSpan, CancellationToken, Task>? delay = null)
    {
        long request = Interlocked.Increment(ref _baselineResetRevision);
        await _serial.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            _baselineResetProof = null;
            Load();
            long settingsRevision = _revision;
            if (BaselineResetBlockReason() is { } blocked) return new(false, blocked);
            if (!await DiscoverAsync(cancellationToken).ConfigureAwait(false)) return new(false, LastError!);
            var previous = _journal.Copy();
            var colors = new List<ColorBaselineObservation>();
            foreach (var record in previous.Records)
            {
                // This path archives old ownership without writing it back. A changed mode can be
                // observed afresh, but only on the uniquely identified same physical display.
                var scope = ResolveResetScope(record.Scope);
                if (scope == null) return new(false, "无法唯一确认待处理的物理屏幕，或当前不是可读取颜色的 SDR 扩展模式。请检查连接与显示设置后重试。");
                var value = await _backend.ReadAsync(scope, cancellationToken).ConfigureAwait(false);
                if (value is not { IsValid: true }) return new(false, "无法读取原色基线。");
                colors.Add(new(scope with { }, value.Copy()));
            }
            await (delay ?? Task.Delay)(TimeSpan.FromMilliseconds(750), cancellationToken).ConfigureAwait(false);
            if (!await ConfirmResetColorsAsync(colors, cancellationToken).ConfigureAwait(false))
                return new(false, "屏幕颜色仍在变化，请退出其他调色功能后重新检查。");
            cancellationToken.ThrowIfCancellationRequested();
            if (request != Volatile.Read(ref _baselineResetRevision) || settingsRevision != _revision) return new(false, "本次原色确认已失效，请重新打开此窗口。");
            if (BaselineResetBlockReason() is { } changed) return new(false, changed);
            _baselineResetProof = new(Guid.NewGuid(), request, settingsRevision, _monotonic() + 300_000, previous, colors);
            bool changedScope = previous.Records.Any(r => !colors.Any(c => WindowsGammaBackend.SameScope(r.Scope, c.Scope)));
            return new(true, changedScope ? "连接或显示模式已变化，已按当前模式完成只读检查。请确认以下屏幕画面正常，再重新建立原色。" :
                "检查完成。请确认以下屏幕当前显示正常，再建立新的验证起点。", _baselineResetProof.Ticket,
                colors.Select(c => c.Scope.DisplayName).ToArray());
        }
        finally { _serial.Release(); }
    }

    public async Task<ColorBaselineResetCheck> ConfirmBaselineResetAsync(Guid ticket, IColorValidationRegistry registry,
        CancellationToken cancellationToken = default)
    {
        await _serial.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var proof = Interlocked.Exchange(ref _baselineResetProof, null);
            if (proof == null || ticket == Guid.Empty || proof.Ticket != ticket || proof.Request != Volatile.Read(ref _baselineResetRevision) ||
                proof.SettingsRevision != _revision || proof.ExpiresMilliseconds <= _monotonic() || !SameRecoveryRecords(proof.Previous, _journal))
                return new(false, "本次原色确认已失效，请重新打开此窗口。");
            if (BaselineResetBlockReason() is { } blocked) return new(false, blocked);
            if (!registry.IsAvailable) return new(false, "色温验证记录不可读取，请保留原文件并重试。");
            if (!await ConfirmResetColorsAsync(proof.Colors, cancellationToken).ConfigureAwait(false))
                return new(false, "屏幕颜色仍在变化，请退出其他调色功能后重新检查。");
            cancellationToken.ThrowIfCancellationRequested();
            if (proof.Request != Volatile.Read(ref _baselineResetRevision) || proof.SettingsRevision != _revision) return new(false, "本次原色确认已失效，请重新打开此窗口。");
            if (BaselineResetBlockReason() is { } changed) return new(false, changed);
            // Keep every old ramp before revoking qualification or retiring active recovery ownership.
            var archive = new ColorRecoveryArchive { ConfirmedUtc = _now().ToUniversalTime(), Previous = _journal.Copy(), ConfirmedColors = proof.Colors.Select(c => c.Copy()).ToList() };
            if (!_store.TryArchive(archive)) return new(false, "旧原色记录备份失败，未解除恢复保护。");
            if (proof.Request != Volatile.Read(ref _baselineResetRevision) || proof.SettingsRevision != _revision || BaselineResetBlockReason() != null)
                return new(false, "本次原色确认已失效，请重新打开此窗口。");
            var ids = proof.Colors.Select(c => c.Scope.HardwareId).ToArray();
            if (!registry.TryRevoke(ids)) return new(false, "旧验证资格无法撤销，已保留恢复记录，请重试。");
            // Refresh qualification immediately, including when the subsequent journal write fails.
            Capabilities = Capabilities.Select(c => ids.Contains(c.Scope.HardwareId, StringComparer.OrdinalIgnoreCase) && c.State == ColorCapabilityState.Supported
                ? c with { State = ColorCapabilityState.Unverified, Reason = "原色起点已重新建立，请重新验证此屏幕。" } : c).ToArray();
            var next = _journal.Copy();
            next.Records.RemoveAll(r => ids.Contains(r.Scope.HardwareId, StringComparer.OrdinalIgnoreCase));
            next.ValidationRequiredScreens = next.ValidationRequiredScreens.Concat(ids).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            if (!Persist(next)) return new(false, "旧记录已备份，但恢复记录更新失败，暂不能调色。请重试。");
            _validationProof = null;
            foreach (var color in proof.Colors)
            {
                _conflicts.Remove(color.Scope.HardwareId); _failed.Remove(color.Scope.HardwareId);
                _transitions.Remove(color.Scope.HardwareId);
                Set(color.Scope, ColorControlState.Unverified, "原色起点已重新建立，请重新验证此屏幕。");
            }
            return new(true, "旧记录已备份，屏幕颜色未改动。接下来请选择屏幕，完成 15 秒验证。", DisplayNames: proof.Colors.Select(c => c.Scope.DisplayName).ToArray());
        }
        finally { Publish(); _serial.Release(); Changed?.Invoke(); }
    }

    public void CancelBaselineReset()
    {
        Interlocked.Increment(ref _baselineResetRevision);
        Interlocked.Exchange(ref _baselineResetProof, null);
    }

    private string? BaselineResetBlockReason() => _stopped || _suspended || Volatile.Read(ref _suspendRequested) != 0
        ? "当前会话不可验证，请解锁后重试。" : !_journalSafe ? "色温恢复记录不可确认，已停止接管。" :
        Settings.AnyEnabled ? "请先关闭色温调节并保存，再重新建立原色。" : _preview != null || IsValidating ? "请先结束试调或验证，再重新建立原色。" :
        _journal.Records.Count == 0 ? "没有需要重新建立的原色记录。" : null;

    private bool CanResetScope(ColorDisplayScope scope) => Capabilities.Any(c => WindowsGammaBackend.SameScope(c.Scope, scope) &&
        c.State is ColorCapabilityState.Supported or ColorCapabilityState.Unverified && c.AdvancedColorEnabled == false && !c.IsCloned && c.GammaReadable);

    private ColorDisplayScope? ResolveResetScope(ColorDisplayScope previous)
    {
        var matches = Capabilities.Where(c => c.Scope.HardwareId.Equals(previous.HardwareId, StringComparison.OrdinalIgnoreCase)).ToArray();
        if (matches.Length != 1) return null;
        var current = matches[0].Scope;
        if (!current.ScopeId.Equals(previous.ScopeId, StringComparison.OrdinalIgnoreCase) ||
            Capabilities.Count(c => c.Scope.ScopeId.Equals(current.ScopeId, StringComparison.OrdinalIgnoreCase)) != 1) return null;
        return CanResetScope(current) ? current : null;
    }

    private async Task<bool> ConfirmResetColorsAsync(IReadOnlyList<ColorBaselineObservation> colors, CancellationToken cancellationToken)
    {
        if (!await DiscoverAsync(cancellationToken).ConfigureAwait(false)) return false;
        foreach (var color in colors)
            if (!CanResetScope(color.Scope) || !color.Value.SameAs(await _backend.ReadAsync(color.Scope, cancellationToken).ConfigureAwait(false))) return false;
        return true;
    }

    private static bool SameRecoveryRecords(ColorRecoveryDocument a, ColorRecoveryDocument b) => a.Records.Count == b.Records.Count &&
        a.Records.All(x => b.Records.Any(y => WindowsGammaBackend.SameScope(x.Scope, y.Scope) && x.SessionId == y.SessionId &&
            x.Conflicted == y.Conflicted && x.Baseline.SameAs(y.Baseline) && x.LastApplied.SameAs(y.LastApplied) &&
            (x.PendingValue == null ? y.PendingValue == null : x.PendingValue.SameAs(y.PendingValue))));

    private sealed record BaselineResetProof(Guid Ticket, long Request, long SettingsRevision, long ExpiresMilliseconds,
        ColorRecoveryDocument Previous, List<ColorBaselineObservation> Colors);
}
