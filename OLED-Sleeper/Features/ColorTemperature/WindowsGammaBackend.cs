using OLED_Sleeper.Native;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;

namespace OLED_Sleeper.Features.ColorTemperature
{
    /// <summary>Qualification must come from a recorded physical-output and recovery test, never from API readback.</summary>
    public interface IColorValidationRegistry
    {
        bool IsValidated(ColorDisplayScope scope);
        bool TryConfirm(ColorDisplayScope scope) => false;
        bool TryRevoke(IReadOnlyCollection<string> hardwareIds) => false;
        bool IsAvailable => true;
    }

    public sealed class UnverifiedColorValidationRegistry : IColorValidationRegistry
    {
        public bool IsValidated(ColorDisplayScope scope) => false;
    }

    /// <summary>Only observed, restored scopes qualify; a bounded trial lease never grants production support.</summary>
    public sealed class WindowsGammaBackend(IColorValidationRegistry? validation = null) : IColorTrialBackend
    {
        private readonly IColorValidationRegistry _validation = validation ?? new UnverifiedColorValidationRegistry();
        private ColorDisplayScope? _trialScope;
        public IDisposable AuthorizeTrial(ColorDisplayScope scope)
        {
            if (Interlocked.CompareExchange(ref _trialScope, scope, null) != null) throw new InvalidOperationException("A color trial is already active.");
            return new TrialLease(() => Interlocked.CompareExchange(ref _trialScope, null, scope));
        }
        private sealed class TrialLease(Action release) : IDisposable
        {
            private Action? _release = release;
            public void Dispose() => Interlocked.Exchange(ref _release, null)?.Invoke();
        }

        public Task<IReadOnlyList<ColorCapability>> DiscoverAsync(CancellationToken cancellationToken) =>
            Task.Run<IReadOnlyList<ColorCapability>>(() => Discover(cancellationToken), cancellationToken);

        private List<ColorCapability> Discover(CancellationToken cancellationToken)
        {
            if (NativeMethods.GetSystemMetrics(0x1000) != 0) return [];
            uint pathCount = 0, modeCount = 0;
            NativeMethods.DISPLAYCONFIG_PATH_INFO[] paths = [];
            NativeMethods.DISPLAYCONFIG_MODE_INFO[] modes = [];
            int result = 122;
            for (int attempt = 0; attempt < 3 && result == 122; attempt++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                result = NativeMethods.GetDisplayConfigBufferSizes(NativeMethods.QDC_ONLY_ACTIVE_PATHS, out pathCount, out modeCount);
                if (result != 0) throw new InvalidOperationException("无法读取显示拓扑。");
                paths = new NativeMethods.DISPLAYCONFIG_PATH_INFO[pathCount];
                modes = new NativeMethods.DISPLAYCONFIG_MODE_INFO[modeCount];
                result = NativeMethods.QueryDisplayConfig(NativeMethods.QDC_ONLY_ACTIVE_PATHS, ref pathCount, paths, ref modeCount, modes, nint.Zero);
            }
            if (result != 0) throw new InvalidOperationException("无法读取显示拓扑。");

            var candidates = new List<ColorCapability>();
            var sourceKeys = paths.Take((int)pathCount).GroupBy(p => SourceKey(p)).ToDictionary(g => g.Key, g => g.Count());
            for (int i = 0; i < pathCount; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var path = paths[i];
                var source = new NativeMethods.DISPLAYCONFIG_SOURCE_DEVICE_NAME { header = Header(path.sourceInfo.adapterId, path.sourceInfo.id, 1, Marshal.SizeOf<NativeMethods.DISPLAYCONFIG_SOURCE_DEVICE_NAME>()) };
                var target = new NativeMethods.DISPLAYCONFIG_TARGET_DEVICE_NAME { header = Header(path.targetInfo.adapterId, path.targetInfo.id, 2, Marshal.SizeOf<NativeMethods.DISPLAYCONFIG_TARGET_DEVICE_NAME>()) };
                var advanced = new ColorNative.AdvancedColorInfo { Header = Header(path.targetInfo.adapterId, path.targetInfo.id, 9, Marshal.SizeOf<ColorNative.AdvancedColorInfo>()) };
                var advanced2 = new ColorNative.AdvancedColorInfo2 { Header = Header(path.targetInfo.adapterId, path.targetInfo.id, 15, Marshal.SizeOf<ColorNative.AdvancedColorInfo2>()) };
                bool namesRead = NativeMethods.DisplayConfigGetDeviceInfo(ref source) == 0 && NativeMethods.DisplayConfigGetDeviceInfo(ref target) == 0;
                int advancedResult = ColorNative.GetAdvancedColorInfo(ref advanced);
                int advanced2Result = ColorNative.GetAdvancedColorInfo2(ref advanced2);
                bool advancedRead = advancedResult == 0;
                bool? isAdvanced = EvaluateAdvancedColor(advancedResult, advanced.Flags, advanced2Result, advanced2.Flags, advanced2.ActiveColorMode);
                string device = namesRead ? source.viewGdiDeviceName : "";
                string hardwareId = namesRead ? HardwareId(device, target.monitorDevicePath ?? "") : "";
                string mode = Hash(string.Join("|", SourceKey(path), path.targetInfo.rotation, path.targetInfo.scaling,
                    path.targetInfo.outputTechnology, path.targetInfo.refreshRate.Numerator, path.targetInfo.refreshRate.Denominator,
                    advancedRead, advanced.Flags, advanced.ColorEncoding, advanced.BitsPerColorChannel,
                    ModeBytes(modes, path.sourceInfo.modeInfoIdx), ModeBytes(modes, path.targetInfo.modeInfoIdx)) +
                    AdvancedColorModeSuffix(advanced2Result, advanced2.Flags, advanced2.ActiveColorMode, isAdvanced));
                var scope = new ColorDisplayScope(hardwareId, namesRead ? target.monitorDevicePath ?? "" : $"unknown-{i}", device,
                    namesRead ? target.monitorFriendlyDeviceName ?? "" : "", "", mode);
                bool cloned = sourceKeys[SourceKey(path)] > 1;
                bool gammaReadable = namesRead && ReadRamp(device) != null;
                string reason = !namesRead || string.IsNullOrWhiteSpace(hardwareId) ? "无法确认屏幕身份。" :
                    cloned ? "复制屏尚不支持独立调色。" : isAdvanced != false ? "HDR 或高级颜色状态未满足 SDR 条件。" :
                    !gammaReadable ? "无法读取原色基线。" : "尚未完成此设备的实机调色与恢复验证。";
                bool eligible = namesRead && !string.IsNullOrWhiteSpace(hardwareId) && !cloned && isAdvanced == false && gammaReadable;
                candidates.Add(new(scope, eligible ? ColorCapabilityState.Unverified : ColorCapabilityState.Unsupported,
                    reason, isAdvanced, cloned, gammaReadable));
            }
            string topology = Hash(string.Join(";", candidates.Select(c => $"{c.Scope.ScopeId}|{c.Scope.DeviceName}|{c.Scope.ModeFingerprint}").Order(StringComparer.Ordinal)));
            var duplicateIds = candidates.GroupBy(c => c.Scope.HardwareId, StringComparer.OrdinalIgnoreCase).Where(g => g.Count() > 1).Select(g => g.Key).ToHashSet(StringComparer.OrdinalIgnoreCase);
            return candidates.Select(c =>
            {
                var scope = c.Scope with { TopologyFingerprint = topology };
                if (duplicateIds.Contains(scope.HardwareId)) return c with { Scope = scope, State = ColorCapabilityState.Unsupported, Reason = "屏幕身份冲突，已停止自动接管。" };
                bool qualified = c.State == ColorCapabilityState.Unverified && _validation.IsValidated(scope);
                return c with { Scope = scope, State = qualified ? ColorCapabilityState.Supported : c.State, Reason = qualified ? "已通过此设备组合的验证。" : c.Reason };
            }).ToList();
        }

        public async Task<ColorValue?> ReadAsync(ColorDisplayScope scope, CancellationToken cancellationToken)
        {
            var current = await DiscoverAsync(cancellationToken).ConfigureAwait(false);
            if (!current.Any(c => SameScope(c.Scope, scope))) return null;
            return await Task.Run(() => ReadRamp(scope.DeviceName), cancellationToken).ConfigureAwait(false);
        }

        public async Task<bool> WriteAsync(ColorDisplayScope scope, ColorValue value, CancellationToken cancellationToken)
        {
            bool trial = Volatile.Read(ref _trialScope) is { } authorized && SameScope(authorized, scope);
            if (!value.IsValid || (!_validation.IsValidated(scope) && !trial)) return false;
            var current = await DiscoverAsync(cancellationToken).ConfigureAwait(false);
            if (!current.Any(c => (c.State == ColorCapabilityState.Supported || trial && c.CanValidate) && SameScope(c.Scope, scope))) return false;
            return await WriteRampAsync(scope, value, cancellationToken).ConfigureAwait(false);
        }

        // The service checks durable baseline, ownership and exact scope before calling this recovery-only path.
        public async Task<bool> RestoreAsync(ColorDisplayScope scope, ColorValue baseline, CancellationToken cancellationToken)
        {
            if (!baseline.IsValid) return false;
            var current = await DiscoverAsync(cancellationToken).ConfigureAwait(false);
            if (!current.Any(c => (c.State == ColorCapabilityState.Supported || c.CanValidate) && SameScope(c.Scope, scope))) return false;
            return await WriteRampAsync(scope, baseline, cancellationToken).ConfigureAwait(false);
        }

        private static async Task<bool> WriteRampAsync(ColorDisplayScope scope, ColorValue value, CancellationToken cancellationToken)
        {
            return await Task.Run(() =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                var dc = ColorNative.CreateDisplayDc("DISPLAY", scope.DeviceName, nint.Zero, nint.Zero);
                if (dc == nint.Zero) return false;
                try { return ColorNative.SetDeviceGammaRamp(dc, value.Channels); }
                finally { ColorNative.DeleteDC(dc); }
            }, cancellationToken).ConfigureAwait(false);
        }

        public ColorValue CreateTarget(ColorValue baseline, int kelvin, double warmth) => GammaTarget.Create(baseline, kelvin, warmth);

        internal static bool? EvaluateAdvancedColor(int legacyResult, uint legacyFlags, int detailedResult, uint detailedFlags, uint activeMode)
        {
            if (detailedResult == 0)
            {
                // INFO_2 reports the actual SDR/WCG/HDR mode. Its bit 2 is reserved; on newer
                // Windows the legacy wideColorEnforced bit can be set even while plain SDR is active.
                if (activeMode > 2) return null;
                if (activeMode != 0 || (detailedFlags & 2) != 0) return true;
                if (legacyResult != 0) return null;
                // Fail closed if the two consecutive reads disagree about active advanced color.
                return (legacyFlags & 2) != 0;
            }

            // Older Windows may not implement request 15. Other errors are not proof of SDR.
            if (detailedResult is not (1 or 50 or 87 or 120) || legacyResult != 0) return null;
            return (legacyFlags & 6) != 0;
        }

        internal static string AdvancedColorModeSuffix(int detailedResult, uint detailedFlags, uint activeMode, bool? isAdvanced)
        {
            // Keep existing SDR qualifications/recovery scopes stable. A newly detected non-SDR
            // or uncertain state must also invalidate read-only recovery/validation scope matching.
            if (isAdvanced == false) return "";
            return FormattableString.Invariant($"|advanced2:{detailedResult}:{detailedFlags}:{activeMode}");
        }

        public static bool SameScope(ColorDisplayScope a, ColorDisplayScope b) =>
            string.Equals(a.HardwareId, b.HardwareId, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(a.ScopeId, b.ScopeId, StringComparison.OrdinalIgnoreCase) &&
            a.DeviceName == b.DeviceName && a.TopologyFingerprint == b.TopologyFingerprint && a.ModeFingerprint == b.ModeFingerprint;

        private static ColorValue? ReadRamp(string device)
        {
            var dc = ColorNative.CreateDisplayDc("DISPLAY", device, nint.Zero, nint.Zero);
            if (dc == nint.Zero) return null;
            try
            {
                var channels = new ushort[768];
                return ColorNative.GetDeviceGammaRamp(dc, channels) && channels.Any(v => v > 0) ? new ColorValue { Channels = channels } : null;
            }
            finally { ColorNative.DeleteDC(dc); }
        }

        private static string HardwareId(string device, string interfacePath)
        {
            for (uint child = 0; child < 32; child++)
            {
                var monitor = new NativeMethods.DISPLAY_DEVICE { cb = Marshal.SizeOf<NativeMethods.DISPLAY_DEVICE>() };
                if (!NativeMethods.EnumDisplayDevices(device, child, ref monitor, NativeMethods.EDD_GET_DEVICE_INTERFACE_NAME)) break;
                if (!string.Equals(monitor.DeviceID, interfacePath, StringComparison.OrdinalIgnoreCase)) continue;
                monitor.cb = Marshal.SizeOf<NativeMethods.DISPLAY_DEVICE>();
                return NativeMethods.EnumDisplayDevices(device, child, ref monitor, 0) ? monitor.DeviceID : "";
            }
            return "";
        }

        private static string SourceKey(NativeMethods.DISPLAYCONFIG_PATH_INFO path) =>
            FormattableString.Invariant($"{path.sourceInfo.adapterId.HighPart}:{path.sourceInfo.adapterId.LowPart}:{path.sourceInfo.id}");
        private static string ModeBytes(NativeMethods.DISPLAYCONFIG_MODE_INFO[] modes, uint index) =>
            index < modes.Length && modes[index].union != null ? Convert.ToHexString(modes[index].union) : "unknown";
        private static string Hash(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
        private static NativeMethods.DISPLAYCONFIG_DEVICE_INFO_HEADER Header(NativeMethods.LUID adapter, uint id, uint type, int size) =>
            new() { adapterId = adapter, id = id, type = type, size = (uint)size };
    }

    public static class GammaTarget
    {
        // Approximate relative channel scaling, not a calibrated white point. Baseline calibration is never replaced by a linear ramp.
        public static ColorValue Create(ColorValue baseline, int kelvin, double warmth)
        {
            if (!baseline.IsValid || kelvin is < 3500 or > 6500 || !double.IsFinite(warmth) || warmth is < 0 or > 1)
                throw new ArgumentOutOfRangeException(nameof(kelvin));
            double t = kelvin / 100d;
            double green = Math.Clamp((99.4708025861 * Math.Log(t) - 161.1195681661) / 255, 0, 1);
            double blue = Math.Clamp((138.5177312231 * Math.Log(t - 10) - 305.0447927307) / 255, 0, 1);
            // 6500 K is the neutral end of this estimate; "original" still means the captured baseline.
            if (kelvin == 6500) green = blue = 1;
            var target = baseline.Copy();
            for (int channel = 0; channel < 3; channel++)
            {
                double scale = 1 - warmth * (1 - (channel == 0 ? 1 : channel == 1 ? green : blue));
                for (int i = 0; i < 256; i++) target.Channels[channel * 256 + i] = (ushort)Math.Round(baseline.Channels[channel * 256 + i] * scale);
            }
            return target;
        }
    }
}
