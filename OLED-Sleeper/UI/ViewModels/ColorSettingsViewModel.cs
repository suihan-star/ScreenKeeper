using OLED_Sleeper.Features.ColorTemperature;
using OLED_Sleeper.Storage;
using OLED_Sleeper.UI.Commands;
using OLED_Sleeper.UI.Helpers;
using OLED_Sleeper.UI.Localization;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Text.Json;
using System.Windows.Input;

namespace OLED_Sleeper.UI.ViewModels;

public sealed class ColorScreenViewModel : ViewModelBase
{
    private readonly Action<string, bool> _choose;
    public string HardwareId { get; }
    public string Name { get; private set; }
    public string Status { get; private set; }
    private bool _selected;
    public bool IsSupported { get; private set; }
    public bool HasDraft { get; set; }
    public string SelectionState => UiText.Get(Selected ? "已开启" : "已关闭") + (HasDraft ? " •" : "");
    public string SelectionLabel => Name + " · " + SelectionState;
    public string PrimaryActionText => IsSupported && !NeedsRecovery ? UiText.Get("设置这块屏幕") : SetupActionText;
    public bool CanPreview { get; private set; }
    public string PreviewLabel => Name + " · " + UiText.Get(CanPreview ? "可试调" : NeedsRecovery ? "需处理恢复记录" : NeedsValidation ? "待验证" : "暂不可试调");
    public string PreviewUnavailableReason { get; private set; } = "";
    public string ValidationMessage { get; private set; } = "";
    public bool CanUseTemporaryControls => IsSupported && !NeedsRecovery;
    public bool NeedsSetup => !IsSupported || NeedsRecovery;
    public string SetupActionText => UiText.Get(NeedsRecovery ? "查看处理步骤" : NeedsValidation ? "开始验证" : "查看说明");
    public string ValidationActionText => UiText.Get(CanValidate ? "验证此屏幕" : "查看验证条件");
    public bool NeedsRecovery { get; private set; }
    public bool IsPaused { get; private set; }
    public ICommand RecheckOriginalCommand { get; }
    public bool CanChoose => IsSupported || _selected;
    public bool CanTurnOffHere => Selected && NeedsSetup;
    public bool Selected { get => _selected; set { _selected = value; _choose(HardwareId, value); OnPropertyChanged(string.Empty); } }
    public ICommand OriginalCommand { get; }
    public ICommand ResumeCommand { get; }
    public ICommand PreviewCommand { get; }
    public ICommand UntilMorningCommand { get; }
    public ICommand ValidateCommand { get; }
    public bool NeedsValidation { get; private set; }
    public bool CanValidate { get; private set; }
    public ColorScreenViewModel(string id, bool selected, Action<string, bool> choose,
        Func<string, Task> pause, Func<string, Task> resume, Func<string, Task> preview, Func<string, Task> untilMorning, Func<string, Task>? validate = null,
        Func<string, Task>? recheckOriginal = null)
    {
        HardwareId = id; _selected = selected; _choose = choose;
        Name = UiText.Get("未连接的屏幕"); Status = UiText.Get("屏幕未连接，已保留偏好。");
        OriginalCommand = new AsyncRelayCommand(() => pause(id), () => IsSupported);
        ResumeCommand = new AsyncRelayCommand(() => resume(id), () => IsSupported);
        PreviewCommand = new AsyncRelayCommand(() => preview(id), () => IsSupported);
        UntilMorningCommand = new AsyncRelayCommand(() => untilMorning(id), () => IsSupported);
        ValidateCommand = new AsyncRelayCommand(() => validate?.Invoke(id) ?? Task.CompletedTask, () => NeedsValidation && validate != null);
        RecheckOriginalCommand = new AsyncRelayCommand(() => recheckOriginal?.Invoke(id) ?? Task.CompletedTask, () => NeedsRecovery && recheckOriginal != null);
    }
    public void Update(ColorCapability? capability, ColorScreenState? state, bool selected, bool validationAvailable = true, string? validationBlockReason = null,
        IReadOnlyList<ColorCapability>? connectedScreens = null)
    {
        _selected = selected; IsSupported = capability?.State == ColorCapabilityState.Supported;
        NeedsValidation = capability?.State == ColorCapabilityState.Unverified;
        CanValidate = capability?.CanValidate == true && validationAvailable && validationBlockReason == null;
        CanPreview = IsSupported && validationAvailable && state?.State is not (ColorControlState.Conflict or ColorControlState.Failed or ColorControlState.RecoveryPending or ColorControlState.Paused);
        Name = capability == null ? UiText.Get("未连接的屏幕") : MonitorTitleFormatter.Format(capability.Scope, connectedScreens ?? [capability]);
        NeedsRecovery = state?.State is ColorControlState.RecoveryPending or ColorControlState.Conflict or ColorControlState.Failed;
        IsPaused = state?.State == ColorControlState.Paused;
        Status = UiText.Get(NeedsRecovery ? state!.Message : capability is { State: not ColorCapabilityState.Supported } ? capability.Reason : state?.Message ?? "屏幕未连接，已保留偏好。");
        if (state?.PausedUntil is { } until) Status += " " + until.ToLocalTime().ToString("MM-dd HH:mm", CultureInfo.InvariantCulture);
        ValidationMessage = UiText.Get(validationBlockReason ?? (!validationAvailable ? "验证暂不可用，请查看页面顶部状态。" :
            capability?.CanValidate == true ? "请先完成 15 秒调暖与恢复验证，再拖动滑杆试调。" : capability?.Reason ?? "屏幕未连接，已保留偏好。"));
        PreviewUnavailableReason = CanPreview ? "" : NeedsRecovery ? Status + " " + UiText.Get("请先关闭色温调节并保存，恢复原色后点击“重新检查原色”。") :
            state?.State == ColorControlState.Paused ? Status + " " + UiText.Get("如需现在试调，请点击“恢复已保存策略”。") :
            NeedsValidation ? ValidationMessage : !validationAvailable ? UiText.Get("验证暂不可用，请查看页面顶部状态。") : Status;
        OnPropertyChanged(string.Empty);
    }
}

public sealed partial class ColorSettingsViewModel : ViewModelBase
{
    private readonly IndependentSettingsStore<ColorTemperatureSettings> _store;
    private readonly Action<ColorTemperatureSettings> _apply;
    private readonly Func<ColorTemperatureSettings, Task>? _applyAndWait;
    private readonly Func<Task> _cancelPreview;
    private readonly Func<string, Task> _pause, _resume, _untilMorning;
    private readonly Func<string, Task>? _validate, _recheckOriginal;
    private readonly Func<string, ColorTemperatureSettings, Task> _preview;
    private readonly Dictionary<string, ScreenDraft> _drafts = new(StringComparer.OrdinalIgnoreCase);
    private readonly ScreenDraft _empty = new(new());
    private ColorTemperatureSettings _saved;
    private ColorScreenViewModel? _selectedScreen;
    private bool _validationAvailable, _busy, _compact, _trialExpanded;
    private string? _validationBlockReason, _saveError;
    private int _selectedPage;
    private IReadOnlyList<ColorCapability> _capabilities = [];
    private IReadOnlyList<ColorScreenState> _states = [];
    private string _runtimeNotice = "正在检测色温能力…";
    private ScreenDraft Current => _selectedScreen != null && _drafts.TryGetValue(_selectedScreen.HardwareId, out var value) ? value : _empty;
    private ColorTemperatureProfile Draft => Current.Profile;
    private string _transition { get => Current.Transition; set => Current.Transition = value; }
    private string _kelvin { get => Current.Night; set => Current.Night = value; }
    private string _dayKelvin { get => Current.Day; set => Current.Day = value; }

    public ColorSettingsViewModel(IndependentSettingsStore<ColorTemperatureSettings> store, ColorTemperatureSettings saved,
        Action<ColorTemperatureSettings> apply, Func<Task> cancelPreview, Func<string, Task> pause,
        Func<string, Task> resume, Func<string, ColorTemperatureSettings, Task> preview, Func<string, Task>? untilMorning = null, Func<string, Task>? validate = null,
        Func<string, int?, CancellationToken, Task<bool>>? livePreview = null, Func<string, Task>? recheckOriginal = null, Func<Task>? resetBaseline = null,
        Func<ColorPreviewStatus?>? previewStatus = null, Func<ColorTemperatureSettings, Task>? applyAndWait = null, Func<Task>? refresh = null)
    {
        _store = store; _saved = saved.Copy(); _apply = apply; _applyAndWait = applyAndWait; _cancelPreview = cancelPreview;
        _pause = pause; _resume = resume; _preview = preview; _untilMorning = untilMorning ?? pause;
        _validate = validate; _recheckOriginal = recheckOriginal; _previewStatusReader = previewStatus;
        foreach (var id in saved.ScreenIds) _drafts[id] = new(saved.ForScreen(id));
        InitializePreview(livePreview);
        CancelPreviewCommand = new AsyncRelayCommand(EndPreviewAsync);
        ConfigureScreensCommand = new RelayCommand(() => SelectedPage = 1);
        ResetBaselineCommand = new AsyncRelayCommand(() => resetBaseline?.Invoke() ?? Task.CompletedTask, () => IsAvailable && HasRecoveryIssues && resetBaseline != null);
        RefreshScreensCommand = new AsyncRelayCommand(() => refresh?.Invoke() ?? Task.CompletedTask, () => IsAvailable && !IsDirty && !_busy && refresh != null);
        PropertyChangedEventManager.AddHandler(UiText.Current, (_, _) => UpdateCapabilities(_capabilities, _states, _runtimeNotice, _validationAvailable, _validationBlockReason), string.Empty);
    }
    public ObservableCollection<ColorScreenViewModel> Screens { get; } = [];
    public ObservableCollection<ColorScreenViewModel> VerifiedScreens { get; } = [];
    public ColorScreenViewModel? SelectedScreen
    {
        get => _selectedScreen;
        set
        {
            if (ReferenceEquals(value, _selectedScreen) || value != null && !VerifiedScreens.Contains(value)) return;
            LeavePage(); _selectedScreen = value; PreviewScreen = value; Notify();
        }
    }
    public int SelectedPage { get => _selectedPage; set { if (_selectedPage == value) return; LeavePage(); _selectedPage = value; Notify(); } }
    public bool HasVerifiedScreens => VerifiedScreens.Count != 0;
    public bool HasNoVerifiedScreens => !HasVerifiedScreens;
    public bool HasNoScreens => Screens.Count == 0;
    public bool UseScreenDropdown => _compact || VerifiedScreens.Count > 3;
    public bool UseScreenCards => !UseScreenDropdown;
    public int TemperatureColumns => _compact || !IsAutomatic ? 1 : 2;
    public void SetCompactLayout(bool compact) { if (_compact == compact) return; _compact = compact; Notify(); }
    public ICommand ConfigureScreensCommand { get; }
    public ICommand CancelPreviewCommand { get; }
    public ICommand ResetBaselineCommand { get; }
    public ICommand RefreshScreensCommand { get; }
    public bool SavedEnabled => _saved.AnyEnabled;
    public string EnabledScreenNames => string.Join("、", Screens.Where(s => _saved.ForScreen(s.HardwareId).Enabled).Select(s => s.Name));
    public bool HasRecoveryIssues => Screens.Any(s => s.NeedsRecovery);
    public bool IsAvailable => _store.IsAvailable;
    public bool CanEdit => IsAvailable && !_busy;
    public bool CanEnable => CanEdit && SelectedScreen?.IsSupported == true && !SelectedScreen.NeedsRecovery;
    public bool Enabled { get => Draft.Enabled; set { if (Draft.Enabled == value) return; LeavePage(); Draft.Enabled = value; Notify(); } }
    public bool IsScreenOff => !Enabled;
    public string Mode { get => Draft.Mode.ToString(); set { if (Enum.TryParse<ColorTemperatureMode>(value, out var mode)) Draft.Mode = mode; LeavePage(); Notify(); } }
    public bool IsAutomatic => Draft.Mode == ColorTemperatureMode.Automatic;
    public string EveningStart { get => Draft.EveningStart; set { Draft.EveningStart = value; Notify(); } }
    public string MorningStart { get => Draft.MorningStart; set { Draft.MorningStart = value; Notify(); } }
    public string TransitionMinutes { get => _transition; set { _transition = value; Notify(); } }
    public string NightKelvin { get => _kelvin; set { if (_kelvin == value) return; _kelvin = value; if (int.TryParse(value, out var kelvin) && kelvin is >= 3500 and <= 6500) Draft.NightKelvin = kelvin; Notify(); QueuePreview(false); } }
    public string DayKelvin { get => _dayKelvin; set { if (_dayKelvin == value) return; _dayKelvin = value; if (int.TryParse(value, out var kelvin) && kelvin is >= 3500 and <= 6500) Draft.DayKelvin = kelvin; Notify(); QueuePreview(true); } }
    public bool DayUsesOriginal { get => Draft.DayUsesOriginal; set { if (Draft.DayUsesOriginal == value) return; _ = EndPreviewAsync(); Draft.DayUsesOriginal = value; Notify(); } }
    public bool UsesDayTemperature { get => !DayUsesOriginal; set { if (value) DayUsesOriginal = false; } }
    public bool NightUsesOriginal { get => Draft.NightUsesOriginal; set { if (Draft.NightUsesOriginal == value) return; _ = EndPreviewAsync(); Draft.NightUsesOriginal = value; Notify(); } }
    public bool UsesNightTemperature { get => !IsAutomatic || !NightUsesOriginal; set { if (value) NightUsesOriginal = false; } }
    public bool ShowRestOriginal => IsAutomatic && NightUsesOriginal;
    public double DaySlider { get => int.TryParse(_dayKelvin, out var value) ? Math.Clamp(value, 3500, 6500) : 5300; set => DayKelvin = ((int)Math.Round(value)).ToString(CultureInfo.InvariantCulture); }
    public double NightSlider { get => int.TryParse(_kelvin, out var value) ? Math.Clamp(value, 3500, 6500) : 4500; set => NightKelvin = ((int)Math.Round(value)).ToString(CultureInfo.InvariantCulture); }
    public bool TrialExpanded { get => _trialExpanded; set { _trialExpanded = value; if (!value) _ = EndPreviewAsync(); Notify(); } }
    public string Notice => UiText.Get(_saveError ?? _store.NoticeKey ?? _runtimeNotice);
    public bool HasNotice => !string.IsNullOrWhiteSpace(Notice);
    public string EnableStatus => UiText.Get(Current.IsDirty ? "有未保存的修改" : Enabled ? "已开启" : "已关闭");
    public string ScheduleSummary => IsAutomatic ? EveningStart + " → " + MorningStart : UiText.Get("手动暖色");
    public bool IsDirty => _drafts.Values.Any(d => d.IsDirty);
    public int DirtyScreenCount => _drafts.Values.Count(d => d.IsDirty);
    public string DraftSummary => string.Format(CultureInfo.InvariantCulture, UiText.Get("{0} 块屏幕有未保存修改"), DirtyScreenCount);
    public string ActiveScreenSummary => string.Format(CultureInfo.InvariantCulture, UiText.Get("{0} 块屏幕正在调节"), _states.Count(s => s.State is ColorControlState.Warm or ColorControlState.Transitioning));
    public string SavedScheduleInfo
    {
        get
        {
            if (SelectedScreen == null) return "";
            var saved = _saved.ForScreen(SelectedScreen.HardwareId);
            if (!saved.Enabled) return UiText.Get("已保存状态：保持原色，未开启");
            var state = _states.FirstOrDefault(s => s.HardwareId.Equals(SelectedScreen.HardwareId, StringComparison.OrdinalIgnoreCase));
            if (state?.State is ColorControlState.Conflict or ColorControlState.Failed or ColorControlState.RecoveryPending or ColorControlState.Paused)
                return UiText.Get(state.Message);
            var target = ColorSchedule.Evaluate(saved, DateTime.Now);
            return UiText.Get("已保存策略的当前目标：") + (target.Warmth == 0 ? UiText.Get("原色") : target.Kelvin.ToString(CultureInfo.InvariantCulture) + " K");
        }
    }
    private ColorTemperatureProfile SavedProfile => SelectedScreen == null ? new() : _saved.ForScreen(SelectedScreen.HardwareId);
    public bool SavedIsAutomatic => SavedProfile.Mode == ColorTemperatureMode.Automatic;
    public string SavedWorkSummary => UiText.Get("工作时：") + (SavedProfile.DayUsesOriginal ? UiText.Get("保持原色") : UiText.Get("使用") + " " + SavedProfile.DayKelvin + " K");
    public string SavedRestSummary => UiText.Get(SavedIsAutomatic ? "休息时：" : "手动：") + (SavedIsAutomatic && SavedProfile.NightUsesOriginal ? UiText.Get("保持原色") : UiText.Get("使用") + " " + SavedProfile.NightKelvin + " K");
    public string SavedStateNotice => !SavedProfile.Enabled ? UiText.Get("已保存状态：保持原色，未开启") : SelectedScreen is { } screen &&
        _states.FirstOrDefault(s => s.HardwareId.Equals(screen.HardwareId, StringComparison.OrdinalIgnoreCase)) is { State: ColorControlState.Conflict or ColorControlState.Failed or ColorControlState.RecoveryPending or ColorControlState.Paused } state ? UiText.Get(state.Message) : "";
    private ColorTemperatureSettings? Build()
    {
        var value = _saved.ToPerScreen();
        foreach (var pair in _drafts)
        {
            if (pair.Value.Build() is not { } profile) return null;
            value.ScreenProfiles![pair.Key] = profile;
        }
        return value;
    }
    public string? Error
    {
        get
        {
            if (!IsAvailable) return UiText.Get(_store.NoticeKey ?? "此模块暂不可用。");
            foreach (var pair in _drafts)
            {
                var error = pair.Value.Build()?.Validate() ?? (pair.Value.Build() == null ? "过渡时间和色温须为整数。" : null);
                if (error != null) return (Screens.FirstOrDefault(s => s.HardwareId == pair.Key)?.Name ?? UiText.Get("未连接的屏幕")) + "：" + UiText.Get(error);
            }
            return null;
        }
    }
    public bool TrySave()
    {
        if (_busy) return false;
        LeavePage(); var value = Build();
        if (Error != null || value == null || !_store.TrySave(value)) { Notify(); return false; }
        _saveError = null;
        _saved = value.Copy(); foreach (var draft in _drafts.Values) draft.MarkSaved();
        _apply(value); Notify(); return true;
    }
    public async Task<bool> DisableForValidationAsync()
    {
        if (_busy || !IsAvailable) return false;
        _busy = true; Notify();
        try
        {
            await EndPreviewAsync();
            var value = _saved.ToPerScreen(); value.DisableAll();
            if (!_store.TrySave(value)) { Notify(); return false; }
            // Only previously saved active switches are changed; other unsaved fields survive.
            foreach (var pair in _drafts)
            {
                bool wasOn = _saved.ForScreen(pair.Key).Enabled;
                if (wasOn) pair.Value.Profile.Enabled = false;
                pair.Value.SetBaseline(value.ForScreen(pair.Key));
            }
            _saved = value.Copy();
            if (_applyAndWait != null) await _applyAndWait(value); else _apply(value);
            _saveError = null; return true;
        }
        catch (Exception) { _saveError = "关闭状态已保存，但恢复未完成。请检查屏幕与恢复记录。"; return false; }
        finally { _busy = false; Notify(); }
    }
    public void Discard()
    {
        if (_busy) return;
        LeavePage(); foreach (var pair in _drafts) pair.Value.Reset(_saved.ForScreen(pair.Key));
        _saveError = null; Notify();
    }
    public void LeavePage() { _trialExpanded = false; _ = EndPreviewAsync(); }
    private Task Preview(string id) => Error == null && _drafts.GetValueOrDefault(id)?.Build() is { } value
        ? _preview(id, new() { NightKelvin = value.NightKelvin }) : Task.CompletedTask;
    public void UpdateCapabilities(IReadOnlyList<ColorCapability> capabilities, IReadOnlyList<ColorScreenState> states, string notice, bool validationAvailable = true, string? validationBlockReason = null)
    {
        _capabilities = capabilities; _states = states; _runtimeNotice = notice; _validationAvailable = validationAvailable; _validationBlockReason = validationBlockReason;
        var ids = capabilities.Select(c => c.Scope.HardwareId).Concat(_saved.ScreenIds).Concat(_drafts.Keys).Concat(states.Select(s => s.HardwareId)).Where(id => !string.IsNullOrWhiteSpace(id)).Distinct(StringComparer.OrdinalIgnoreCase);
        foreach (var id in ids.ToArray())
        {
            if (!_drafts.ContainsKey(id)) _drafts[id] = new(_saved.ForScreen(id));
            var screen = Screens.FirstOrDefault(s => s.HardwareId.Equals(id, StringComparison.OrdinalIgnoreCase));
            if (screen == null)
            {
                screen = new(id, _drafts[id].Profile.Enabled, (key, chosen) => { _drafts[key].Profile.Enabled = chosen; Notify(); }, _pause, _resume, Preview, _untilMorning, _validate, _recheckOriginal);
                Screens.Add(screen);
            }
            screen.Update(capabilities.FirstOrDefault(c => c.Scope.HardwareId.Equals(id, StringComparison.OrdinalIgnoreCase)), states.FirstOrDefault(s => s.HardwareId.Equals(id, StringComparison.OrdinalIgnoreCase)), _drafts[id].Profile.Enabled, validationAvailable, validationBlockReason, capabilities);
        }
        foreach (var stale in VerifiedScreens.Where(s => !s.IsSupported || s.NeedsRecovery).ToArray()) VerifiedScreens.Remove(stale);
        foreach (var screen in Screens.Where(s => s.IsSupported && !s.NeedsRecovery)) if (!VerifiedScreens.Contains(screen)) VerifiedScreens.Add(screen);
        if (SelectedScreen == null || !VerifiedScreens.Contains(SelectedScreen)) SelectedScreen = VerifiedScreens.FirstOrDefault();
        if (_trialRequested && _previewScreen?.CanPreview != true) _ = EndPreviewAsync();
        Notify();
    }
    private void Notify()
    {
        foreach (var screen in Screens)
        {
            screen.HasDraft = _drafts[screen.HardwareId].IsDirty;
            screen.Update(_capabilities.FirstOrDefault(c => c.Scope.HardwareId.Equals(screen.HardwareId, StringComparison.OrdinalIgnoreCase)), _states.FirstOrDefault(s => s.HardwareId.Equals(screen.HardwareId, StringComparison.OrdinalIgnoreCase)), _drafts[screen.HardwareId].Profile.Enabled, _validationAvailable, _validationBlockReason, _capabilities);
        }
        OnPropertyChanged(string.Empty); CommandManager.InvalidateRequerySuggested();
    }
    private sealed class ScreenDraft
    {
        public ColorTemperatureProfile Profile { get; private set; } = new();
        public string Transition = "", Night = "", Day = "";
        private string _baseline = "";
        private string Identity => JsonSerializer.Serialize(new { Profile, Transition, Night, Day });
        public bool IsDirty => Identity != _baseline;
        public ScreenDraft(ColorTemperatureProfile saved) => Reset(saved);
        public void Reset(ColorTemperatureProfile saved)
        {
            Profile = saved.CopyProfile(); Transition = saved.TransitionMinutes.ToString(CultureInfo.InvariantCulture);
            Night = saved.NightKelvin.ToString(CultureInfo.InvariantCulture); Day = saved.DayKelvin.ToString(CultureInfo.InvariantCulture); MarkSaved();
        }
        public void MarkSaved()
        {
            if (Build() is { } profile)
            {
                Profile = profile;
                Day = profile.DayKelvin.ToString(CultureInfo.InvariantCulture);
                Night = profile.NightKelvin.ToString(CultureInfo.InvariantCulture);
            }
            _baseline = Identity;
        }
        public void SetBaseline(ColorTemperatureProfile saved) => _baseline = new ScreenDraft(saved).Identity;
        public ColorTemperatureProfile? Build()
        {
            if (!int.TryParse(Transition, out var transition)) return null;
            bool validNight = int.TryParse(Night, out var night), validDay = int.TryParse(Day, out var day);
            // Hidden original-color fields retain their last valid custom candidate.
            // Invalid visible input still blocks saving instead of being silently replaced.
            if (Profile.Mode == ColorTemperatureMode.Automatic && Profile.NightUsesOriginal && (!validNight || night is < 3500 or > 6500)) night = Profile.NightKelvin;
            else if (!validNight) return null;
            if ((Profile.Mode == ColorTemperatureMode.Manual || Profile.DayUsesOriginal) && (!validDay || day is < 3500 or > 6500)) day = Profile.DayKelvin;
            else if (!validDay) return null;
            var value = Profile.CopyProfile(); value.TransitionMinutes = transition; value.NightKelvin = night; value.DayKelvin = day; return value;
        }
    }
}
