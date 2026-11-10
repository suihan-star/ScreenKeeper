namespace OLED_Sleeper.Features.ColorTemperature
{
    public sealed record ColorPreviewStatus(string HardwareId, int? Kelvin, TimeSpan Remaining);

    /// <summary>Serializes ownership and write-ahead recovery. Device support is a separate, explicit backend gate.</summary>
    public sealed partial class ColorTemperatureService
    {
        private readonly IColorTemperatureBackend _backend;
        private readonly IColorRecoveryStore _store;
        private readonly Func<DateTimeOffset> _now;
        private readonly Func<long> _monotonic;
        private readonly SemaphoreSlim _serial = new(1, 1);
        private readonly string _session = Guid.NewGuid().ToString("N");
        private readonly Dictionary<string, ColorScreenState> _states = new(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> _failed = new(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> _conflicts = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, ColorTransition> _transitions = new(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> _previewChangedScreens = new(StringComparer.OrdinalIgnoreCase);
        private readonly CancellationTokenSource _transitionPumpCancellation = new();
        private bool _transitionPumpRunning, _smoothAll;
        private DateTime? _lastLocalTime;
        private long _lastMonotonic;
        private ColorRecoveryDocument _journal = new();
        private bool _loaded, _journalSafe = true, _suspended, _stopped;
        private Preview? _preview;
        private CancellationTokenSource? _previewExpiry;
        private ColorScreenState[] _stateSnapshot = [];
        private long _revision;
        private long _previewRevision;
        private int _suspendRequested;

        public ColorTemperatureService(IColorTemperatureBackend backend, IColorRecoveryStore store, Func<DateTimeOffset>? now = null, Func<long>? monotonicMilliseconds = null)
        {
            _backend = backend;
            _store = store;
            _now = now ?? (() => DateTimeOffset.Now);
            _monotonic = monotonicMilliseconds ?? (() => Environment.TickCount64);
        }

        public ColorTemperatureSettings Settings { get; private set; } = new();
        public IReadOnlyList<ColorCapability> Capabilities { get; private set; } = [];
        public IReadOnlyList<ColorScreenState> States => Volatile.Read(ref _stateSnapshot);
        public string? LastError { get; private set; }
        public bool HasPreview => _preview != null;
        public string? PreviewHardwareId => Volatile.Read(ref _preview)?.HardwareId;
        public ColorPreviewStatus? PreviewStatus
        {
            get
            {
                var preview = Volatile.Read(ref _preview);
                return preview == null ? null : new(preview.HardwareId, preview.Kelvin,
                    TimeSpan.FromMilliseconds(Math.Max(0, preview.ExpiresMilliseconds - _monotonic())));
            }
        }
        public event Action? Changed;

        public async Task ApplySettingsAsync(ColorTemperatureSettings settings, CancellationToken cancellationToken = default)
        {
            CancelValidation();
            Interlocked.Increment(ref _previewRevision);
            var copy = settings.Copy();
            if (copy.Validate() is { } error) throw new ArgumentException(error, nameof(settings));
            long revision = Interlocked.Increment(ref _revision);
            await _serial.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                if (_stopped || revision != _revision) return;
                Settings = copy;
                ResetResumeSamples();
                _smoothAll = true;
                SetPreview(null);
                await RefreshCoreAsync(cancellationToken).ConfigureAwait(false);
            }
            finally { Publish(); _serial.Release(); Changed?.Invoke(); }
        }

        public async Task RefreshAsync(CancellationToken cancellationToken = default)
        {
            if (!await _serial.WaitAsync(0, cancellationToken).ConfigureAwait(false)) return;
            try { if (!_stopped) await RefreshCoreAsync(cancellationToken).ConfigureAwait(false); }
            finally { Publish(); _serial.Release(); Changed?.Invoke(); }
        }

        public async Task<bool> PauseAsync(string? hardwareId, DateTimeOffset until, CancellationToken cancellationToken = default)
        {
            Interlocked.Increment(ref _previewRevision);
            await _serial.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                Load();
                if (_stopped || !_journalSafe || until <= _now()) return false;
                var next = _journal.Copy();
                if (hardwareId != null) next.PausedUntil[hardwareId] = until;
                else foreach (var id in Settings.ScreenIds.Where(id => Settings.ForScreen(id).Enabled)) next.PausedUntil[id] = until;
                if (!Persist(next)) return false;
                ResetResumeSamples();
                SetPreview(null);
                if (hardwareId == null) _transitions.Clear(); else _transitions.Remove(hardwareId);
                await RefreshCoreAsync(cancellationToken).ConfigureAwait(false);
                return true;
            }
            finally { Publish(); _serial.Release(); Changed?.Invoke(); }
        }

        public async Task<bool> ResumeAsync(string? hardwareId = null, CancellationToken cancellationToken = default)
        {
            await _serial.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                Load();
                if (_stopped || !_journalSafe) return false;
                var next = _journal.Copy();
                if (hardwareId == null) next.PausedUntil.Clear(); else next.PausedUntil.Remove(hardwareId);
                if (!Persist(next)) return false;
                ResetResumeSamples();
                await RefreshCoreAsync(cancellationToken).ConfigureAwait(false);
                return true;
            }
            finally { Publish(); _serial.Release(); Changed?.Invoke(); }
        }

        public Task<bool> PreviewAsync(string hardwareId, int kelvin, CancellationToken cancellationToken = default) =>
            PreviewTargetAsync(hardwareId, kelvin, cancellationToken);

        public Task<bool> PreviewOriginalAsync(string hardwareId, CancellationToken cancellationToken = default) =>
            PreviewTargetAsync(hardwareId, null, cancellationToken);

        private async Task<bool> PreviewTargetAsync(string hardwareId, int? kelvin, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(hardwareId) || kelvin is < 3500 or > 6500 || IsValidating) return false;
            long request = Interlocked.Increment(ref _previewRevision);
            long expires = _monotonic() + 15_000;
            await _serial.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                Load();
                if (_stopped || _suspended || Volatile.Read(ref _suspendRequested) != 0 || !_journalSafe || request != _previewRevision || expires <= _monotonic()) return false;
                if (!await DiscoverAsync(cancellationToken).ConfigureAwait(false)) return false;
                var capability = Capabilities.FirstOrDefault(c => c.Scope.HardwareId.Equals(hardwareId, StringComparison.OrdinalIgnoreCase) && c.State == ColorCapabilityState.Supported);
                if (capability == null || request != _previewRevision || _resumeChecks.ContainsKey(hardwareId) || _resumeTransitions.Contains(hardwareId) ||
                    _failed.Contains(hardwareId) || _conflicts.Contains(hardwareId) ||
                    (_journal.PausedUntil.TryGetValue(hardwareId, out var pause) && pause > _now())) return false;
                if (_preview is { } previous && !previous.HardwareId.Equals(hardwareId, StringComparison.OrdinalIgnoreCase))
                {
                    SetPreview(null);
                    await RefreshCoreAsync(cancellationToken, previous.HardwareId, request).ConfigureAwait(false);
                }
                if (request != _previewRevision) return false;
                SetPreview(new(capability.Scope, kelvin, expires, request));
                await RefreshCoreAsync(cancellationToken, hardwareId, request).ConfigureAwait(false);
                bool success = request == _previewRevision && _states.TryGetValue(hardwareId, out var state) &&
                    (state.State is ColorControlState.Warm or ColorControlState.Transitioning || (kelvin == null && state.State == ColorControlState.Original));
                if (!success && request == _previewRevision) SetPreview(null);
                return success;
            }
            catch (OperationCanceledException)
            {
                if (request == Volatile.Read(ref _previewRevision))
                {
                    SetPreview(null);
                    if (!_stopped) await RefreshCoreAsync(CancellationToken.None, hardwareId, request).ConfigureAwait(false);
                }
                throw;
            }
            finally { Publish(); _serial.Release(); Changed?.Invoke(); }
        }

        public async Task CancelPreviewAsync(CancellationToken cancellationToken = default)
        {
            Interlocked.Increment(ref _previewRevision);
            await _serial.WaitAsync(cancellationToken).ConfigureAwait(false);
            try { ResetResumeSamples(); SetPreview(null); if (!_stopped) await RefreshCoreAsync(cancellationToken).ConfigureAwait(false); }
            finally { Publish(); _serial.Release(); Changed?.Invoke(); }
        }

        public async Task SetSuspendedAsync(bool suspended, CancellationToken cancellationToken = default)
        {
            Interlocked.Increment(ref _previewRevision);
            if (suspended) CancelValidation();
            // A lock/sleep request also blocks writes already queued behind an in-flight read.
            long request;
            lock (_suspensionSync)
            {
                request = ++_suspensionRevision;
                if (suspended)
                {
                    _resumeCapturePending = true;
                    Volatile.Write(ref _suspendRequested, 1);
                }
            }
            await _serial.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                bool capture;
                lock (_suspensionSync)
                {
                    // A delayed unlock must never clear the fast gate of a newer lock request.
                    if (request != _suspensionRevision) return;
                    // A quick lock/unlock can overtake the queued lock handler. Its recovery
                    // observation must survive even when that obsolete handler is skipped.
                    capture = _resumeCapturePending;
                    _resumeCapturePending = false;
                    _suspended = suspended;
                    if (!suspended) Volatile.Write(ref _suspendRequested, 0);
                }
                Load();
                if (capture) ArmResumeRecovery();
                else ResetResumeSamples();
                _smoothAll = true;
                if (suspended) _transitions.Clear();
                SetPreview(null);
                if (!_stopped) await RefreshCoreAsync(cancellationToken).ConfigureAwait(false);
            }
            finally { Publish(); _serial.Release(); Changed?.Invoke(); }
        }

        public async Task StopAsync(CancellationToken cancellationToken = default)
        {
            CancelValidation();
            Interlocked.Increment(ref _previewRevision);
            Interlocked.Increment(ref _revision);
            await _serial.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                Load();
                _stopped = true;
                _resumeChecks.Clear();
                _resumeTransitions.Clear();
                _transitions.Clear();
                _transitionPumpCancellation.Cancel();
                SetPreview(null);
                // Stop invalidates queued policy updates; recovery still checks the live identity immediately before each write.
                await DiscoverAsync(cancellationToken).ConfigureAwait(false);
                foreach (var record in _journal.Records.ToArray()) await RestoreAsync(record, cancellationToken).ConfigureAwait(false);
            }
            finally { Publish(); _serial.Release(); Changed?.Invoke(); }
        }

        private void Load()
        {
            if (_loaded) return;
            _loaded = true;
            var read = _store.Read();
            _journal = read.Document.Copy();
            _journalSafe = read.IsSafe;
            LastError = read.Error;
        }

        private async Task RefreshCoreAsync(CancellationToken cancellationToken, string? onlyScreenId = null, long? previewRequest = null)
        {
            Load();
            if (_suspended || Volatile.Read(ref _suspendRequested) != 0)
            {
                foreach (var capability in Capabilities) Set(capability.Scope, ColorControlState.Paused, "会话已暂停，等待恢复后重新检测。");
                return;
            }
            long revision = Volatile.Read(ref _revision);
            long previewRevision = previewRequest ?? Volatile.Read(ref _previewRevision);
            if (previewRevision != Volatile.Read(ref _previewRevision)) return;
            if (!await DiscoverAsync(cancellationToken).ConfigureAwait(false)) return;
            if (!_journalSafe)
            {
                foreach (var capability in Capabilities) Set(capability.Scope, ColorControlState.RecoveryPending, LastError ?? "色温恢复记录不可确认，已停止接管。");
                return;
            }
            var now = _now();
            long monotonic = _monotonic();
            if (onlyScreenId == null && _lastLocalTime is { } previousLocal &&
                Math.Abs((now.DateTime - previousLocal).TotalMilliseconds - (monotonic - _lastMonotonic)) > 30_000)
                _smoothAll = true;
            if (onlyScreenId == null) { _lastLocalTime = now.DateTime; _lastMonotonic = monotonic; }
            if (_preview is { } preview && (preview.ExpiresMilliseconds <= monotonic ||
                !Capabilities.Any(c => c.State == ColorCapabilityState.Supported && WindowsGammaBackend.SameScope(c.Scope, preview.Scope)))) SetPreview(null);
            // Prior sessions must resolve their journal before a residual warm ramp can become a new baseline.
            foreach (var record in _journal.Records.Where(r => r.SessionId != _session &&
                (onlyScreenId == null || r.Scope.HardwareId.Equals(onlyScreenId, StringComparison.OrdinalIgnoreCase))).ToArray())
                await RestoreAsync(record, cancellationToken).ConfigureAwait(false);

            var activeIds = Capabilities.Select(c => c.Scope.HardwareId).ToHashSet(StringComparer.OrdinalIgnoreCase);
            foreach (var id in _transitions.Keys.Where(id => !activeIds.Contains(id)).ToArray()) _transitions.Remove(id);
            _previewChangedScreens.RemoveWhere(id => !activeIds.Contains(id));
            foreach (var id in Settings.ScreenIds.Concat(_journal.Records.Select(r => r.Scope.HardwareId)).Distinct(StringComparer.OrdinalIgnoreCase).Where(id => !activeIds.Contains(id)))
                _states[id] = _journal.Records.Any(r => r.Scope.HardwareId.Equals(id, StringComparison.OrdinalIgnoreCase))
                    ? new(id, ColorControlState.RecoveryPending, "屏幕离线，保留原色恢复记录。")
                    : new(id, ColorControlState.Offline, "屏幕离线，已保留偏好。");

            foreach (var capability in Capabilities)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var scope = capability.Scope;
                if (onlyScreenId != null && !scope.HardwareId.Equals(onlyScreenId, StringComparison.OrdinalIgnoreCase)) continue;
                var record = _journal.Records.FirstOrDefault(r => r.Scope.HardwareId.Equals(scope.HardwareId, StringComparison.OrdinalIgnoreCase));
                if (_conflicts.Contains(scope.HardwareId) || record?.Conflicted == true) continue;
                if (!await ObserveResumeAsync(capability, record, revision, previewRevision, cancellationToken).ConfigureAwait(false)) continue;
                record = _journal.Records.FirstOrDefault(r => r.Scope.HardwareId.Equals(scope.HardwareId, StringComparison.OrdinalIgnoreCase));
                var screenSettings = Settings.ForScreen(scope.HardwareId);
                var policy = ColorSchedule.Evaluate(screenSettings, now.DateTime);
                bool participating = screenSettings.Enabled;
                bool paused = _journal.PausedUntil.TryGetValue(scope.HardwareId, out var pause) && pause > now;
                bool previewing = _preview?.HardwareId.Equals(scope.HardwareId, StringComparison.OrdinalIgnoreCase) == true;
                if (previewing && _preview!.Revision != previewRevision) continue;
                double warmth = previewing ? _preview!.Kelvin.HasValue ? 1 : 0 : policy.Warmth;
                int kelvin = previewing ? _preview!.Kelvin ?? 6500 : policy.Kelvin;
                if (paused || (!participating && !previewing) || (!previewing && warmth == 0 && record == null && !_transitions.ContainsKey(scope.HardwareId)))
                {
                    _transitions.Remove(scope.HardwareId);
                    if (record != null && !await RestoreAsync(record, cancellationToken).ConfigureAwait(false)) continue;
                    _previewChangedScreens.Remove(scope.HardwareId);
                    Set(scope, paused ? ColorControlState.Paused : !participating && !previewing ? ColorControlState.Disabled : ColorControlState.Original,
                        paused ? "已临时恢复原色。" : "未接管屏幕颜色。", paused ? pause : null);
                    continue;
                }
                if (_failed.Contains(scope.HardwareId)) continue;
                if (capability.State != ColorCapabilityState.Supported)
                {
                    _transitions.Remove(scope.HardwareId);
                    if (record != null) Set(scope, ColorControlState.RecoveryPending, "显示模式或能力已改变，保留恢复记录。");
                    else Set(scope, capability.State == ColorCapabilityState.Unverified ? ColorControlState.Unverified : ColorControlState.Unsupported, capability.Reason);
                    continue;
                }
                if (record != null && (!WindowsGammaBackend.SameScope(scope, record.Scope) || record.SessionId != _session))
                {
                    _transitions.Remove(scope.HardwareId);
                    Set(scope, ColorControlState.RecoveryPending, "显示模式或能力已改变，保留恢复记录。");
                    continue;
                }
                if (revision != Volatile.Read(ref _revision) || previewRevision != Volatile.Read(ref _previewRevision) || Volatile.Read(ref _suspendRequested) != 0) return;
                bool immediate = previewing || _previewChangedScreens.Contains(scope.HardwareId);
                await ApplyAsync(scope, record, kelvin, warmth, revision, cancellationToken, previewRevision, immediate,
                    previewing && _preview!.Kelvin == null).ConfigureAwait(false);
                _previewChangedScreens.Remove(scope.HardwareId);
            }
            if (onlyScreenId == null) _smoothAll = false;
        }

        private async Task<bool> DiscoverAsync(CancellationToken cancellationToken)
        {
            try
            {
                Capabilities = await _backend.DiscoverAsync(cancellationToken).ConfigureAwait(false);
                if (LastError == "无法读取显示拓扑。") LastError = null;
                var duplicates = Capabilities.GroupBy(c => c.Scope.HardwareId, StringComparer.OrdinalIgnoreCase).Where(g => g.Count() > 1).Select(g => g.Key).ToHashSet(StringComparer.OrdinalIgnoreCase);
                Capabilities = Capabilities.Select(c => duplicates.Contains(c.Scope.HardwareId) ? c with { State = ColorCapabilityState.Unsupported, Reason = "屏幕身份冲突，已停止自动接管。" } : c).ToArray();
                Capabilities = Capabilities.Select(c => c.State == ColorCapabilityState.Supported &&
                    _journal.ValidationRequiredScreens.Contains(c.Scope.HardwareId, StringComparer.OrdinalIgnoreCase)
                    ? c with { State = ColorCapabilityState.Unverified, Reason = "原色起点已重新建立，请重新验证此屏幕。" } : c).ToArray();
                foreach (var check in _resumeChecks.Values)
                    if (!Capabilities.Any(c => c.State == ColorCapabilityState.Supported && WindowsGammaBackend.SameScope(c.Scope, check.Record.Scope))) check.Reset();
                return true;
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception)
            {
                ResetResumeSamples();
                LastError = "无法读取显示拓扑。";
                foreach (var capability in Capabilities) Set(capability.Scope, ColorControlState.Failed, LastError);
                return false;
            }
        }

        private async Task ApplyAsync(ColorDisplayScope scope, ColorRecoveryRecord? record, int kelvin, double warmth, long revision, CancellationToken cancellationToken,
            long previewRevision, bool immediate, bool retainBaseline)
        {
            bool awaitingResumeRead = _resumeTransitions.Contains(scope.HardwareId) && record != null;
            try
            {
                var current = await _backend.ReadAsync(scope, cancellationToken).ConfigureAwait(false);
                if (revision != Volatile.Read(ref _revision) || previewRevision != Volatile.Read(ref _previewRevision) || Volatile.Read(ref _suspendRequested) != 0) return;
                if (current is not { IsValid: true })
                {
                    if (awaitingResumeRead) RetryResumeRead(scope, record!);
                    else Fail(scope, "无法读取原色基线。");
                    return;
                }
                if (record != null && !current.SameAs(record.LastApplied))
                {
                    if (awaitingResumeRead && current.SameAs(record.Baseline)) RetryResumeRead(scope, record);
                    else Conflict(record);
                    return;
                }
                bool resuming = _resumeTransitions.Remove(scope.HardwareId);
                awaitingResumeRead = false;
                bool firstTakeover = record == null;
                record ??= new() { Scope = scope, SessionId = _session, Baseline = current.Copy(), LastApplied = current.Copy() };
                // Sixty small increments limit hardware/journal churn. No unchanged target is rewritten on the one-second poll.
                var desired = _backend.CreateTarget(record.Baseline, kelvin, Math.Round(warmth * 60) / 60);
                _transitions.TryGetValue(scope.HardwareId, out var transition);
                if (immediate || current.SameAs(desired)) transition = null;
                else if (resuming || (_smoothAll && (transition == null || !desired.SameAs(transition.Target))) ||
                    (firstTakeover && (transition == null || !current.SameAs(transition.From))))
                    transition = new(current.Copy(), desired.Copy(), _monotonic());
                else if (transition != null && !desired.SameAs(transition.Target)) transition = transition with { Target = desired.Copy() };

                var target = transition?.Frame(_monotonic()) ?? desired;
                bool transitioning = transition != null && transition.Progress(_monotonic()) < 1;
                if (transitioning)
                {
                    _transitions[scope.HardwareId] = transition!;
                    EnsureTransitionPump();
                }
                else _transitions.Remove(scope.HardwareId);
                if (!current.SameAs(target))
                {
                    var pending = record.Copy();
                    pending.PendingValue = target.Copy();
                    if (!Put(pending)) { Fail(scope, "无法保存色温恢复记录，未继续调色。"); return; }
                    cancellationToken.ThrowIfCancellationRequested();
                    if (revision != Volatile.Read(ref _revision) || previewRevision != Volatile.Read(ref _previewRevision) || Volatile.Read(ref _suspendRequested) != 0) return;
                    // Once dispatch starts, verification and journal completion are not canceled halfway through ownership transfer.
                    bool written = await _backend.WriteAsync(scope, target, CancellationToken.None).ConfigureAwait(false);
                    var verified = await _backend.ReadAsync(scope, CancellationToken.None).ConfigureAwait(false);
                    if (!written || !target.SameAs(verified)) { Fail(scope, "色温写入或读回失败，保留恢复记录。"); return; }
                    pending.LastApplied = target.Copy();
                    pending.PendingValue = null;
                    if (!Put(pending)) { Fail(scope, "无法保存色温恢复记录，未继续调色。"); return; }
                }
                if (!transitioning && desired.SameAs(record.Baseline))
                {
                    // Keep ownership while comparing: an external change must not become a new baseline on release.
                    if (retainBaseline)
                    {
                        if (firstTakeover && !Put(record)) { Fail(scope, "无法保存色温恢复记录，未继续调色。"); return; }
                        Set(scope, ColorControlState.Original, "正在对比接管前原色。");
                        return;
                    }
                    var owned = _journal.Records.FirstOrDefault(r => r.Scope.ScopeId == scope.ScopeId);
                    if (owned != null) await RestoreAsync(owned, cancellationToken).ConfigureAwait(false);
                    else Set(scope, ColorControlState.Original, "未接管屏幕颜色。");
                }
                else Set(scope, transitioning || warmth < 1 ? ColorControlState.Transitioning : ColorControlState.Warm,
                    transitioning ? "正在平滑过渡到当前色温策略。" : "已写入经过验证的色温策略。", warmth: warmth);
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception)
            {
                if (awaitingResumeRead) RetryResumeRead(scope, record!);
                else Fail(scope, "色温写入或读回失败，保留恢复记录。");
            }
        }

        private async Task<bool> RestoreAsync(ColorRecoveryRecord record, CancellationToken cancellationToken)
        {
            var scope = record.Scope;
            if (!_journalSafe || record.Conflicted || _conflicts.Contains(scope.HardwareId)) { Set(scope, ColorControlState.RecoveryPending, "存在外部调色或未确认的恢复记录。"); return false; }
            var capability = Capabilities.FirstOrDefault(c => WindowsGammaBackend.SameScope(c.Scope, scope));
            if (capability == null || (capability.State != ColorCapabilityState.Supported && !capability.CanValidate))
            {
                Set(scope, ColorControlState.RecoveryPending, "显示模式或能力已改变，保留恢复记录。");
                return false;
            }
            try
            {
                var current = await _backend.ReadAsync(scope, cancellationToken).ConfigureAwait(false);
                if (current == null) { Set(scope, ColorControlState.RecoveryPending, "无法读取原色基线。"); return false; }
                if (!current.SameAs(record.Baseline))
                {
                    if (!current.SameAs(record.LastApplied) && !current.SameAs(record.PendingValue)) { Conflict(record); return false; }
                    cancellationToken.ThrowIfCancellationRequested();
                    if (!await _backend.RestoreAsync(scope, record.Baseline, CancellationToken.None).ConfigureAwait(false) ||
                        !record.Baseline.SameAs(await _backend.ReadAsync(scope, CancellationToken.None).ConfigureAwait(false)))
                    {
                        Set(scope, ColorControlState.RecoveryPending, "原色恢复失败，已保留恢复记录。");
                        return false;
                    }
                }
                var next = _journal.Copy();
                next.Records.RemoveAll(r => r.Scope.ScopeId == scope.ScopeId);
                if (!Persist(next)) { Set(scope, ColorControlState.RecoveryPending, "原色已恢复，但恢复记录更新失败。"); return false; }
                _failed.Remove(scope.HardwareId);
                _resumeChecks.Remove(scope.HardwareId);
                _resumeTransitions.Remove(scope.HardwareId);
                Set(scope, ColorControlState.Original, "已恢复接管前原色。");
                return true;
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception) { Set(scope, ColorControlState.RecoveryPending, "原色恢复失败，已保留恢复记录。"); return false; }
        }

        private bool Persist(ColorRecoveryDocument next)
        {
            if (!_journalSafe || !_store.TryWrite(next)) { LastError = "无法保存色温恢复记录，未继续调色。"; return false; }
            _journal = next;
            return true;
        }

        private bool Put(ColorRecoveryRecord record)
        {
            var next = _journal.Copy();
            next.Records.RemoveAll(r => r.Scope.ScopeId == record.Scope.ScopeId);
            next.Records.Add(record.Copy());
            return Persist(next);
        }

        private void Conflict(ColorRecoveryRecord record)
        {
            var conflict = record.Copy();
            conflict.Conflicted = true;
            Put(conflict);
            _failed.Add(record.Scope.HardwareId);
            _conflicts.Add(record.Scope.HardwareId);
            _resumeChecks.Remove(record.Scope.HardwareId);
            _resumeTransitions.Remove(record.Scope.HardwareId);
            _transitions.Remove(record.Scope.HardwareId);
            Set(record.Scope, ColorControlState.Conflict, "检测到外部调色，已停止竞争。请检查当前颜色。");
        }

        private void Fail(ColorDisplayScope scope, string message)
        {
            _failed.Add(scope.HardwareId);
            _transitions.Remove(scope.HardwareId);
            Set(scope, ColorControlState.Failed, message);
        }

        private void Set(ColorDisplayScope scope, ColorControlState state, string message, DateTimeOffset? until = null, double warmth = 0) =>
            _states[scope.HardwareId] = new(scope.HardwareId, state, message, until, warmth);

        private void Publish() => Volatile.Write(ref _stateSnapshot, _states.Values.ToArray());

        private void SetPreview(Preview? preview)
        {
            if (_preview != null) _previewChangedScreens.Add(_preview.HardwareId);
            if (preview != null) _previewChangedScreens.Add(preview.HardwareId);
            _previewExpiry?.Cancel();
            _previewExpiry?.Dispose();
            _previewExpiry = null;
            _preview = preview;
            if (preview == null) return;
            _previewExpiry = new();
            _ = ExpirePreviewAsync(preview, _previewExpiry.Token);
        }

        private async Task ExpirePreviewAsync(Preview preview, CancellationToken cancellationToken)
        {
            try
            {
                await Task.Delay(TimeSpan.FromMilliseconds(Math.Max(0, preview.ExpiresMilliseconds - _monotonic())), cancellationToken).ConfigureAwait(false);
                await _serial.WaitAsync(cancellationToken).ConfigureAwait(false);
                try
                {
                    if (!_stopped && ReferenceEquals(_preview, preview))
                    {
                        SetPreview(null);
                        await RefreshCoreAsync(CancellationToken.None).ConfigureAwait(false);
                    }
                }
                finally { Publish(); _serial.Release(); Changed?.Invoke(); }
            }
            catch (OperationCanceledException) { }
            catch (Exception)
            {
                // The journal remains authoritative if an asynchronous rollback cannot complete.
                LastError = "原色恢复失败，已保留恢复记录。";
                Changed?.Invoke();
            }
        }

        private void EnsureTransitionPump()
        {
            if (_transitionPumpRunning || _stopped) return;
            _transitionPumpRunning = true;
            _ = AdvanceTransitionsAsync(_transitionPumpCancellation.Token);
        }

        private async Task AdvanceTransitionsAsync(CancellationToken cancellationToken)
        {
            try
            {
                while (true)
                {
                    await Task.Delay(ColorTransition.StepMilliseconds, cancellationToken).ConfigureAwait(false);
                    await _serial.WaitAsync(cancellationToken).ConfigureAwait(false);
                    try
                    {
                        if (_stopped || _suspended || _transitions.Count == 0)
                        {
                            _transitionPumpRunning = false;
                            return;
                        }
                        await RefreshCoreAsync(cancellationToken).ConfigureAwait(false);
                    }
                    finally { Publish(); _serial.Release(); Changed?.Invoke(); }
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception)
            {
                LastError = "色温操作失败，已保留恢复记录。";
                Changed?.Invoke();
            }
        }

        private sealed record Preview(ColorDisplayScope Scope, int? Kelvin, long ExpiresMilliseconds, long Revision)
        {
            public string HardwareId => Scope.HardwareId;
        }
    }
}
