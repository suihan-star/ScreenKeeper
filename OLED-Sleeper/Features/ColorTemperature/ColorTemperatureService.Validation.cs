namespace OLED_Sleeper.Features.ColorTemperature;

public sealed record ColorValidationResult(bool CanConfirm, string Message, Guid Ticket = default);

public sealed partial class ColorTemperatureService
{
    private CancellationTokenSource? _validationCancellation;
    private ValidationProof? _validationProof;
    public bool IsValidating { get; private set; }

    public string? ValidationBlockReason
    {
        get
        {
            if (_stopped || _suspended || Volatile.Read(ref _suspendRequested) != 0) return "当前会话不可验证，请解锁后重试。";
            if (!_journalSafe) return "色温恢复记录不可确认，已停止接管。";
            if (Settings.AnyEnabled) return "请先关闭色温调节并保存，再进行验证。";
            return _journal.Records.Count != 0 ? "有屏幕的原色记录尚未处理。请先处理恢复记录，再开始验证。" : null;
        }
    }

    public string? GetValidationBlockReason(string hardwareId)
    {
        if (ValidationBlockReason is { } blocked) return blocked;
        var target = Capabilities.SingleOrDefault(c => c.Scope.HardwareId.Equals(hardwareId, StringComparison.OrdinalIgnoreCase));
        if (target == null) return "屏幕未连接，已保留偏好。";
        if (target.State == ColorCapabilityState.Supported) return "此屏幕已通过验证，无需重复验证。";
        return target.CanValidate && _backend is IColorTrialBackend ? null : target.Reason;
    }

    /// <summary>A conflict is retired only after the exact recorded original is observed, without writing any device.</summary>
    public async Task<string> RecheckOriginalAsync(string hardwareId, CancellationToken cancellationToken = default)
    {
        await _serial.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            Load();
            if (_stopped || _suspended || Volatile.Read(ref _suspendRequested) != 0) return "当前会话不可验证，请解锁后重试。";
            if (!_journalSafe) return LastError ?? "色温恢复记录不可确认，已停止接管。";
            if (Settings.AnyEnabled) return "请先关闭色温调节并保存，再重新检查原色。";
            if (_preview != null) return "请先结束试调，再重新检查原色。";
            if (!await DiscoverAsync(cancellationToken).ConfigureAwait(false)) return LastError!;
            var target = Capabilities.SingleOrDefault(c => c.Scope.HardwareId.Equals(hardwareId, StringComparison.OrdinalIgnoreCase));
            if (target == null) return "屏幕未连接，已保留偏好。";
            var record = _journal.Records.FirstOrDefault(r => r.Scope.HardwareId.Equals(hardwareId, StringComparison.OrdinalIgnoreCase));
            if (record == null) return "此屏幕没有待处理的原色恢复记录。";
            if (!WindowsGammaBackend.SameScope(target.Scope, record.Scope)) return "显示模式或能力已改变，保留恢复记录。";
            var current = await _backend.ReadAsync(record.Scope, cancellationToken).ConfigureAwait(false);
            if (current is not { IsValid: true }) return "无法读取原色基线。";
            if (!current.SameAs(record.Baseline)) return "当前颜色与旧原色不同。若已退出其他调色功能且画面正常，请选择“重新建立原色”；旧记录已保留，屏幕颜色未改动。";
            var next = _journal.Copy();
            next.Records.RemoveAll(r => WindowsGammaBackend.SameScope(r.Scope, record.Scope));
            if (!Persist(next)) return "原色已恢复，但恢复记录更新失败。";
            _conflicts.Remove(hardwareId); _failed.Remove(hardwareId); _transitions.Remove(hardwareId);
            Set(target.Scope, ColorControlState.Original, "已确认回到记录的原色，恢复阻塞已解除。");
            return "已确认回到记录的原色，恢复阻塞已解除。";
        }
        finally { Publish(); _serial.Release(); Changed?.Invoke(); }
    }

    /// <summary>Invoked only by an explicit, observed device test. It shares the production serialization and durable journal.</summary>
    public async Task<ColorValidationResult> RunValidationAsync(string hardwareId, IProgress<int>? remaining = null,
        CancellationToken cancellationToken = default, Func<TimeSpan, CancellationToken, Task>? delay = null)
    {
        Interlocked.Increment(ref _previewRevision);
        await _serial.WaitAsync(cancellationToken).ConfigureAwait(false);
        ColorDisplayScope? scope = null;
        List<(ColorDisplayScope Scope, ColorValue Value)> originals = [];
        bool observed = false, restored = false;
        string message = "验证未完成，未启用自动调色。";
        IDisposable? lease = null;
        _validationProof = null;
        using var cancel = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _validationCancellation = cancel;
        IsValidating = true;
        try
        {
            Load();
            if (_stopped || _suspended || Volatile.Read(ref _suspendRequested) != 0) throw Invalid("当前会话不可验证，请解锁后重试。");
            if (!_journalSafe) throw Invalid("色温恢复记录不可确认，已停止接管。");
            if (Settings.AnyEnabled) throw Invalid("请先关闭色温调节并保存，再进行验证。");
            SetPreview(null);
            await RefreshCoreAsync(cancel.Token).ConfigureAwait(false);
            if (GetValidationBlockReason(hardwareId) is { } blocked) throw Invalid(blocked);
            var target = Capabilities.SingleOrDefault(c => c.Scope.HardwareId.Equals(hardwareId, StringComparison.OrdinalIgnoreCase));
            if (target?.CanValidate != true || _backend is not IColorTrialBackend trial) throw Invalid("当前显示模式不支持验证，请使用 SDR 扩展屏。");
            scope = target.Scope;
            foreach (var capability in Capabilities)
            {
                var original = await _backend.ReadAsync(capability.Scope, cancel.Token).ConfigureAwait(false);
                if (original is not { IsValid: true }) throw Invalid("无法读取屏幕原色，未开始验证。");
                originals.Add((capability.Scope, original.Copy()));
            }
            var baseline = originals.Single(s => WindowsGammaBackend.SameScope(s.Scope, scope)).Value;
            var warm = _backend.CreateTarget(baseline, 5000, 1);
            if (warm.SameAs(baseline)) throw Invalid("无法生成有效的调暖测试，请检查显示模式。");
            lease = trial.AuthorizeTrial(scope);
            var record = new ColorRecoveryRecord { Scope = scope, SessionId = _session, Baseline = baseline.Copy(), LastApplied = baseline.Copy(), PendingValue = warm.Copy() };
            if (!Put(record)) throw Invalid("无法保存色温恢复记录，未继续调色。");
            cancel.Token.ThrowIfCancellationRequested();
            if (!await _backend.WriteAsync(scope, warm, CancellationToken.None).ConfigureAwait(false) ||
                !warm.SameAs(await _backend.ReadAsync(scope, CancellationToken.None).ConfigureAwait(false)))
                throw Invalid("色温写入或读回失败，保留恢复记录。");
            record.LastApplied = warm.Copy(); record.PendingValue = null;
            if (!Put(record)) throw Invalid("无法保存色温恢复记录，未继续调色。");
            var others = originals.Where(s => !WindowsGammaBackend.SameScope(s.Scope, scope)).ToArray();
            if (!await UnchangedAsync(others, cancel.Token).ConfigureAwait(false)) throw Invalid("其他屏幕颜色发生变化，验证未通过。");
            Set(scope, ColorControlState.Warm, "正在进行 15 秒调暖验证，将自动恢复原色。");
            Publish(); Changed?.Invoke();
            for (int seconds = 15; seconds > 0; seconds--)
            {
                remaining?.Report(seconds);
                await (delay ?? Task.Delay)(TimeSpan.FromSeconds(1), cancel.Token).ConfigureAwait(false);
                if (!warm.SameAs(await _backend.ReadAsync(scope, cancel.Token).ConfigureAwait(false)))
                    throw Invalid("检测到外部调色或显示模式变化，验证已停止。");
            }
            observed = true;
        }
        catch (OperationCanceledException) { message = "验证已取消，正在恢复原色。"; }
        catch (ValidationException ex) { message = ex.Message; }
        catch (Exception) { message = "验证失败，请检查显示连接与其他调色工具。"; }
        finally
        {
            try
            {
                if (scope != null)
                {
                    await DiscoverAsync(CancellationToken.None).ConfigureAwait(false);
                    var record = _journal.Records.FirstOrDefault(r => WindowsGammaBackend.SameScope(r.Scope, scope));
                    restored = record == null || await RestoreAsync(record, CancellationToken.None).ConfigureAwait(false);
                    restored = restored && await UnchangedAsync(originals, CancellationToken.None).ConfigureAwait(false);
                    if (!restored) message = "原色恢复未通过复核，已保留记录，不能启用自动调色。";
                }
            }
            catch (Exception) { restored = false; message = "原色恢复失败，已保留恢复记录。"; }
            lease?.Dispose();
            _validationCancellation = null;
            IsValidating = false;
            if (observed && restored && scope != null && !cancel.IsCancellationRequested)
                _validationProof = new(Guid.NewGuid(), scope, originals, _now().AddMinutes(5));
            Publish(); _serial.Release(); Changed?.Invoke();
        }
        return _validationProof is { } proof
            ? new(true, "已恢复原色，请确认刚才观察到的效果。", proof.Ticket)
            : new(false, message);
    }

    public async Task<ColorValidationResult> ConfirmValidationAsync(Guid ticket, IColorValidationRegistry registry, CancellationToken cancellationToken = default)
    {
        await _serial.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var proof = _validationProof;
            _validationProof = null; // Confirmation evidence is single-use, including failed persistence.
            if (proof == null || ticket == Guid.Empty || proof.Ticket != ticket || proof.Expires < _now() ||
                _stopped || _suspended || Volatile.Read(ref _suspendRequested) != 0 || Settings.AnyEnabled || _journal.Records.Count != 0 ||
                !await DiscoverAsync(cancellationToken).ConfigureAwait(false) ||
                !Capabilities.Any(c => c.CanValidate && WindowsGammaBackend.SameScope(c.Scope, proof.Scope)) ||
                !await UnchangedAsync(proof.Originals, cancellationToken).ConfigureAwait(false))
                return new(false, "验证结果已失效，请保持显示模式不变并重新验证。");
            cancellationToken.ThrowIfCancellationRequested();
            if (!registry.TryConfirm(proof.Scope)) return new(false, "验证结果保存失败，未启用自动调色。");
            if (_journal.ValidationRequiredScreens.Contains(proof.Scope.HardwareId, StringComparer.OrdinalIgnoreCase))
            {
                var next = _journal.Copy();
                next.ValidationRequiredScreens.RemoveAll(id => id.Equals(proof.Scope.HardwareId, StringComparison.OrdinalIgnoreCase));
                if (!Persist(next)) return new(false, "验证结果保存失败，未启用自动调色。");
            }
            await RefreshCoreAsync(CancellationToken.None).ConfigureAwait(false);
            return new(true, "验证通过。勾选此屏、启用色温调节并保存后生效。", ticket);
        }
        finally { Publish(); _serial.Release(); Changed?.Invoke(); }
    }

    public void CancelValidation()
    {
        try { _validationCancellation?.Cancel(); } catch (ObjectDisposedException) { }
        _validationProof = null;
    }

    private async Task<bool> UnchangedAsync(IEnumerable<(ColorDisplayScope Scope, ColorValue Value)> originals, CancellationToken cancellationToken)
    {
        foreach (var original in originals)
            if (!original.Value.SameAs(await _backend.ReadAsync(original.Scope, cancellationToken).ConfigureAwait(false))) return false;
        return true;
    }
    private static ValidationException Invalid(string message) => new(message);
    private sealed class ValidationException(string message) : Exception(message);
    private sealed record ValidationProof(Guid Ticket, ColorDisplayScope Scope, List<(ColorDisplayScope Scope, ColorValue Value)> Originals, DateTimeOffset Expires);
}
