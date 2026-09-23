using Microsoft.Win32;
using OLED_Sleeper.Features.ColorTemperature;
using OLED_Sleeper.Features.HealthReminder;
using OLED_Sleeper.Features.MonitorInformation.Services.Interfaces;
using OLED_Sleeper.Features.MonitorPrivacy;
using OLED_Sleeper.Infrastructure.Runtime;
using OLED_Sleeper.Storage;
using OLED_Sleeper.Storage.Interfaces;
using OLED_Sleeper.UI.Localization;
using OLED_Sleeper.UI.ViewModels;
using OLED_Sleeper.UI.Views;
using Serilog;
using System.Windows.Threading;

namespace OLED_Sleeper.Infrastructure.Hosting;

public sealed class HealthRuntimeState
{
    public int Schema { get; set; } = 1;
    public DateTimeOffset? PausedUntilUtc { get; set; }
}

/// <summary>Independent settings and runtime lifetimes; all presentation stays on the UI dispatcher.</summary>
public sealed class WellbeingCoordinator : IDisposable
{
    private readonly Dispatcher _dispatcher;
    private readonly IMonitorInfoManager _monitors;
    private readonly IMonitorPrivacyService _privacy;
    private readonly ReminderDisplayGate _gate;
    private readonly Features.MonitorDimming.Services.Interfaces.IMonitorDimmingService _dimming;
    private DateTimeOffset _nextPauseCleanup;
    private readonly IndependentSettingsStore<HealthRuntimeState> _runtimeStore;
    private HealthRuntimeState _runtime;
    private readonly DispatcherTimer _colorPoll;
    private bool _polling, _stopping, _disposed;
    private bool _sessionLocked, _systemSleeping;
    private string? _colorFailure;
    private readonly ColorValidationRegistry _validation;
    public HealthReminderService Health { get; }
    public ColorTemperatureService Color { get; }
    public HealthSettingsViewModel HealthSettings { get; }
    public ColorSettingsViewModel ColorSettings { get; }

    public WellbeingCoordinator(IAppDataFileStore files, IFileSystem fileSystem, IStorageRoot root,
        IMonitorInfoManager monitors, IMonitorPrivacyService privacy, ReminderDisplayGate gate,
        Features.MonitorDimming.Services.Interfaces.IMonitorDimmingService dimming)
    {
        _dispatcher = Dispatcher.CurrentDispatcher; _monitors = monitors; _privacy = privacy; _gate = gate; _dimming = dimming;
        var healthStore = new IndependentSettingsStore<HealthReminderSettings>(files, fileSystem, root, "health-reminder.json", () => new(), s => s.Validate().FirstOrDefault());
        var colorStore = new IndependentSettingsStore<ColorTemperatureSettings>(files, fileSystem, root, "color-temperature.json", () => new(), s => s.Validate(), maxSchema: 2);
        _runtimeStore = new(files, fileSystem, root, "health-reminder-runtime.json", () => new(), s => s.Schema == 1 ? null : "配置版本不受支持。");
        _runtime = _runtimeStore.Load();
        var healthSaved = healthStore.Load(); var colorSaved = colorStore.Load();
        var images = new PrivacyImageStore(root, "HealthImages");
        Health = new(_dispatcher, IsScreenAvailable, presenter: new HealthReminderPresenter(images.Load));
        HealthSettings = new(healthStore, Health, healthSaved, ToggleHealthPause, () => _runtimeStore.IsAvailable ? null : _runtimeStore.NoticeKey, () =>
        {
            var picker = new OpenFileDialog { Filter = "PNG / JPEG / WebP|*.png;*.jpg;*.jpeg;*.webp", Multiselect = false };
            return picker.ShowDialog() == true ? images.Import(picker.FileName) : null;
        }, images.Load);
        _validation = new(new IndependentSettingsStore<ColorValidationDocument>(files, fileSystem, root, "color-temperature-validation.json", () => new(), s => s.Validate()));
        Color = new(new WindowsGammaBackend(_validation), new ColorRecoveryFileStore(files, fileSystem, root));
        ColorSettings = new(colorStore, colorSaved,
            saved => _ = RunColorAsync(() => Color.ApplySettingsAsync(saved)),
            () => RunColorAsync(() => Color.CancelPreviewAsync()),
            id => RunColorAsync(async () => { await Color.PauseAsync(id, DateTimeOffset.UtcNow.AddHours(1)); }),
            id => RunColorAsync(async () => { await Color.ResumeAsync(id); }),
            (id, draft) => RunColorAsync(async () => { await Color.PreviewAsync(id, draft.NightKelvin); }),
            id => RunColorAsync(async () => { await Color.PauseAsync(id, new DateTimeOffset(ColorSchedule.NextMorning(Color.Settings.ForScreen(id), DateTime.Now))); }),
            id =>
            {
                var capability = Color.Capabilities.FirstOrDefault(c => c.Scope.HardwareId == id);
                var name = capability == null ? id : UI.Helpers.MonitorTitleFormatter.Format(capability.Scope, Color.Capabilities);
                var dialog = new ColorValidationWindow(Color, _validation, id, name);
                if (System.Windows.Application.Current?.MainWindow is { IsVisible: true } owner) dialog.Owner = owner;
                dialog.ShowDialog();
                ColorChanged();
                return Task.CompletedTask;
            }, livePreview: RunColorPreviewAsync, recheckOriginal: RecheckOriginalAsync, resetBaseline: ResetBaselineAsync, previewStatus: () => Color.PreviewStatus, applyAndWait: async value =>
            {
                await Color.ApplySettingsAsync(value);
                ColorChanged();
                if (Color.ValidationBlockReason != null) throw new InvalidOperationException(Color.ValidationBlockReason);
            }, refresh: () => RunColorAsync(() => Color.RefreshAsync()));
        Color.Changed += ColorChanged;
        Health.StateChanged += HealthChanged;
        _privacy.Changed += PrivacyChanged;
        _gate.Unavailable += HideReminder;
        _colorPoll = new DispatcherTimer(TimeSpan.FromSeconds(5), DispatcherPriority.Background, async (_, _) =>
        {
            if (_polling || _stopping) return;
            _polling = true;
            try { await RunColorAsync(() => Color.RefreshAsync()); }
            finally { _polling = false; }
        }, _dispatcher);
        _colorPoll.Stop();
        if (!healthStore.IsAvailable || !_runtimeStore.IsAvailable) healthSaved.Enabled = false;
        Health.ApplySaved(healthSaved);
        Health.RestorePause(_runtime.PausedUntilUtc);
        // Even a disabled or unreadable policy must resolve pending recovery before discovering a new baseline.
        if (!colorStore.IsAvailable) colorSaved.DisableAll();
        _ = RunColorAsync(() => Color.ApplySettingsAsync(colorSaved));
    }

    public void Start()
    {
        _dispatcher.VerifyAccess(); Health.Start(); _colorPoll.Start();
        SystemEvents.PowerModeChanged += PowerChanged;
        SystemEvents.SessionSwitch += SessionChanged;
    }
    private bool IsScreenAvailable(string device)
    {
        try
        {
            var screen = _monitors.GetLatestMonitorsBasicInfo().FirstOrDefault(m => m.DeviceName.Equals(device, StringComparison.OrdinalIgnoreCase));
            return screen != null && !string.IsNullOrWhiteSpace(screen.HardwareId) && !_gate.IsSleeping(screen.HardwareId) && !_privacy.IsProtected(screen.HardwareId) && !_dimming.GetDimmedMonitors().ContainsKey(screen.HardwareId);
        }
        catch { return false; }
    }
    private void PrivacyChanged()
    {
        var device = Health.Snapshot.CurrentScreenDeviceName;
        if (device != null && !IsScreenAvailable(device)) HideReminder();
    }
    private void HideReminder()
    {
        if (_stopping || _dispatcher.HasShutdownStarted) return;
        if (!_dispatcher.CheckAccess()) { _dispatcher.BeginInvoke(DispatcherPriority.Send, new Action(HideReminder)); return; }
        if (Health.Snapshot.CurrentScreenDeviceName is { } device && !IsScreenAvailable(device)) Health.DismissWindow();
    }
    private void HealthChanged()
    {
        var now = DateTimeOffset.UtcNow;
        if (_runtime.PausedUntilUtc is { } until && until <= now && now >= _nextPauseCleanup)
        {
            _nextPauseCleanup = now.AddMinutes(5);
            if (_runtimeStore.TrySave(new())) _runtime = new();
        }
    }
    public bool ToggleHealthPause()
    {
        var state = new HealthRuntimeState { PausedUntilUtc = Health.Snapshot.IsManuallyPaused ? null : DateTimeOffset.UtcNow.AddHours(1) };
        if (!_runtimeStore.TrySave(state)) return false;
        _runtime = state;
        if (state.PausedUntilUtc.HasValue) Health.PauseUntil(state.PausedUntilUtc); else Health.Resume();
        return true;
    }
    public Task ToggleOriginalAsync() => RunColorAsync(async () =>
    {
        if (Color.States.Any(s => s.State == ColorControlState.Paused)) await Color.ResumeAsync();
        else await Color.PauseAsync(null, DateTimeOffset.UtcNow.AddHours(1));
    });
    private async Task RunColorAsync(Func<Task> operation)
    {
        try { await operation(); _colorFailure = null; }
        catch (Exception ex) { Log.Error(ex, "Color operation failed; recovery records are retained."); _colorFailure = "色温操作失败，已保留恢复记录。"; }
        ColorChanged();
    }
    private async Task<bool> RunColorPreviewAsync(string id, int? kelvin, CancellationToken cancellationToken)
    {
        try
        {
            bool result = kelvin.HasValue ? await Color.PreviewAsync(id, kelvin.Value, cancellationToken) : await Color.PreviewOriginalAsync(id, cancellationToken);
            _colorFailure = null;
            return result;
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex) { Log.Error(ex, "Color preview failed; recovery records are retained."); _colorFailure = "色温操作失败，已保留恢复记录。"; return false; }
        finally { ColorChanged(); }
    }
    private void ColorChanged()
    {
        if (_stopping || _dispatcher.HasShutdownStarted) return;
        if (!_dispatcher.CheckAccess()) { _dispatcher.BeginInvoke(new Action(ColorChanged)); return; }
        ColorSettings.UpdateCapabilities(Color.Capabilities, Color.States,
            _colorFailure ?? Color.LastError ?? (!_validation.IsAvailable ? "色温验证记录不可读取，请保留原文件并重试。" :
                Color.IsValidating ? "正在进行 15 秒调暖验证，将自动恢复原色。" : Color.HasPreview ? "正在预览，15 秒后回到已保存策略。" :
                Color.States.Any(s => s.State is ColorControlState.Conflict or ColorControlState.RecoveryPending or ColorControlState.Failed) ? "部分屏幕需要处理原色恢复记录，请选择屏幕查看原因。" :
                Color.Settings.Schema == 1 && Color.Settings.Enabled && !Color.Settings.ParticipatingScreens.Any(s => s.Value) ? "旧设置未开启任何屏幕。请在作息与色温中选择屏幕，开启并保存。" :
                Color.Capabilities.Any(c => c.State == ColorCapabilityState.Supported) ? Color.Settings.AnyEnabled ? "色温按各屏幕已保存策略运行。" : "在作息与色温中选择屏幕，开启并保存后生效。" :
                "首次使用请前往屏幕与验证，完成调暖与恢复确认。"), !Color.IsValidating && _validation.IsAvailable,
                !_validation.IsAvailable ? "色温验证记录不可读取，请保留原文件并重试。" : Color.IsValidating ? "正在进行 15 秒调暖验证，将自动恢复原色。" : Color.ValidationBlockReason);
        ColorSettings.ObservePreview(Color.PreviewHardwareId);
    }
    private async Task RecheckOriginalAsync(string id)
    {
        string message;
        try { message = await Color.RecheckOriginalAsync(id); }
        catch (Exception ex) { Log.Error(ex, "Read-only color recovery check failed."); message = "原色检查失败，已保留恢复记录。"; }
        ColorChanged();
        if (_stopping) return;
        var dialog = new AppDialog(UiText.Get(message), UiText.Get("重新检查原色"), false);
        if (System.Windows.Application.Current?.MainWindow is { IsVisible: true } owner) dialog.Owner = owner;
        dialog.ShowDialog();
    }
    private Task ResetBaselineAsync()
    {
        var dialog = new ColorBaselineResetWindow(Color, _validation);
        if (System.Windows.Application.Current?.MainWindow is { IsVisible: true } owner) dialog.Owner = owner;
        dialog.ShowDialog();
        ColorChanged();
        return Task.CompletedTask;
    }
    private void PowerChanged(object sender, PowerModeChangedEventArgs e)
    {
        if (_stopping) return;
        if (e.Mode is PowerModes.Suspend or PowerModes.Resume)
        {
            _systemSleeping = e.Mode == PowerModes.Suspend;
            _ = RunColorAsync(() => Color.SetSuspendedAsync(_sessionLocked || _systemSleeping));
        }
    }
    private void SessionChanged(object sender, SessionSwitchEventArgs e)
    {
        if (_stopping) return;
        if (e.Reason is SessionSwitchReason.SessionLock or SessionSwitchReason.SessionLogoff or SessionSwitchReason.RemoteDisconnect or SessionSwitchReason.ConsoleDisconnect) _sessionLocked = true;
        else if (e.Reason is SessionSwitchReason.SessionUnlock or SessionSwitchReason.SessionLogon or SessionSwitchReason.RemoteConnect or SessionSwitchReason.ConsoleConnect) _sessionLocked = false;
        else return;
        _ = RunColorAsync(() => Color.SetSuspendedAsync(_sessionLocked || _systemSleeping));
    }
    public void StopPresentation()
    {
        Color.CancelValidation();
        _dispatcher.VerifyAccess(); _stopping = true; _colorPoll.Stop(); Health.Stop();
        SystemEvents.PowerModeChanged -= PowerChanged;
        SystemEvents.SessionSwitch -= SessionChanged;
    }
    public Task RestoreColorAsync() => Color.StopAsync();
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true; StopPresentation();
        _privacy.Changed -= PrivacyChanged; _gate.Unavailable -= HideReminder;
        Health.StateChanged -= HealthChanged; Color.Changed -= ColorChanged; Health.Dispose();
    }
}
