using OLED_Sleeper.Features.HealthReminder;
using OLED_Sleeper.Storage;
using OLED_Sleeper.UI.Commands;
using OLED_Sleeper.UI.Localization;
using System.ComponentModel;
using System.Globalization;
using System.Text.Json;
using System.Windows.Input;

namespace OLED_Sleeper.UI.ViewModels;

public sealed class HealthSettingsViewModel : ViewModelBase
{
    private readonly IndependentSettingsStore<HealthReminderSettings> _store;
    private readonly HealthReminderService _service;
    private readonly Func<bool> _togglePause;
    private readonly Func<string?> _runtimeError;
    private HealthReminderSettings _saved;
    private string _work = "50", _rest = "5", _snooze = "5", _restUnit = "Minutes";
    private bool _enabled, _idle, _fullscreen;
    private string _baseline = "", _feedback = "";
    private HealthAppearance _appearance = new();
    private readonly Func<string?>? _importImage;
    public HealthSettingsViewModel(IndependentSettingsStore<HealthReminderSettings> store, HealthReminderService service,
        HealthReminderSettings saved, Func<bool> togglePause, Func<string?>? runtimeError = null, Func<string?>? importImage = null, Func<string, System.Windows.Media.ImageSource?>? loadImage = null)
    {
        _store = store; _service = service; _saved = saved.Copy(); _togglePause = togglePause; _runtimeError = runtimeError ?? (() => null);
        _importImage = importImage; _loadImage = loadImage;
        ResetFields();
        StartBreakCommand = new RelayCommand(_service.StartBreak, () => _saved.Enabled && IsAvailable);
        PreviewCommand = new RelayCommand(() => { _feedback = _service.Preview(_appearance.Copy(), PreviewRestStage) ? "外观预览中，不改变工作计时。" : _service.PreviewError; Notify(); }, () => IsAvailable && _appearance.Validate() == null);
        ImportImageCommand = new RelayCommand(() =>
        {
            try { if (_importImage?.Invoke() is { } name) { _appearance.Image = name; _feedback = "图片已导入，保存后用于提醒。"; } }
            catch { _feedback = "图片导入失败，请选择 10 MB 内的 PNG、JPEG 或 WebP 图片。"; }
            Notify();
        });
        ResetImageCommand = new RelayCommand(() => { _appearance.Image = null; Notify(); });
        PauseCommand = new RelayCommand(() => { _feedback = _togglePause() ? "" : "暂停状态保存失败，当前计时保持不变。"; Notify(); }, () => _saved.Enabled && IsAvailable);
        _service.StateChanged += Notify;
        PropertyChangedEventManager.AddHandler(UiText.Current, (_, _) => Notify(), string.Empty);
    }
    private readonly Func<string, System.Windows.Media.ImageSource?>? _loadImage;
    private int _selectedPage;
    private bool _previewRestStage;
    public int SelectedPage { get => _selectedPage; set { if (_selectedPage == value) return; LeavePage(); _selectedPage = value; Notify(); } }
    public bool PreviewRestStage { get => _previewRestStage; set { if (_previewRestStage == value) return; LeavePage(); _previewRestStage = value; Notify(); } }
    public bool PreviewReminderStage { get => !PreviewRestStage; set { if (value) PreviewRestStage = false; } }
    public bool StageUsesPicture => PreviewRestStage ? _appearance.RestStyle == HealthRestStyle.Picture : _appearance.ReminderStyle == HealthReminderStyle.Scenic;
    public bool StageUsesOverlay => !PreviewRestStage && UsesOverlay;
    public bool StageUsesMotion => PreviewRestStage || UsesOverlay;
    public HealthAppearance DraftAppearance => _appearance.Copy();
    public System.Windows.Media.ImageSource? LoadPreviewImage()
    {
        try { return _appearance.Image is { } image ? _loadImage?.Invoke(image) : null; }
        catch { return null; }
    }
    public string EnableStatus => UiText.Get(_enabled != _saved.Enabled ? _enabled ? "开启待保存" : "关闭待保存" : _saved.Enabled ? "已开启" : "已关闭");
    public ICommand StartBreakCommand { get; }
    public ICommand PreviewCommand { get; }
    public ICommand PauseCommand { get; }
    public ICommand ImportImageCommand { get; }
    public ICommand ResetImageCommand { get; }
    public string ReminderStyle { get => _appearance.ReminderStyle.ToString(); set { if (Enum.TryParse<HealthReminderStyle>(value, out var style)) _appearance.ReminderStyle = style; Notify(); } }
    public string RestStyle { get => _appearance.RestStyle.ToString(); set { if (Enum.TryParse<HealthRestStyle>(value, out var style)) _appearance.RestStyle = style; Notify(); } }
    public int FadeSeconds { get => _appearance.FadeSeconds; set { _appearance.FadeSeconds = value; Notify(); } }
    public int OverlayPercent { get => _appearance.OverlayPercent; set { _appearance.OverlayPercent = value; Notify(); } }
    public bool MotionEnabled { get => _appearance.MotionEnabled; set { _appearance.MotionEnabled = value; Notify(); } }
    public int MotionStrength { get => _appearance.MotionStrength; set { _appearance.MotionStrength = value; Notify(); } }
    public bool UsesPicture => _appearance.ReminderStyle == HealthReminderStyle.Scenic || _appearance.RestStyle == HealthRestStyle.Picture;
    public bool UsesOverlay => _appearance.ReminderStyle != HealthReminderStyle.Compact;
    public string ImageStatus => UiText.Get(_appearance.Image == null ? "内置山林背景" : "已选择自定义图片");
    public bool IsAvailable => _store.IsAvailable && _runtimeError() == null;
    public bool Enabled { get => _enabled; set { _enabled = value; Notify(); } }
    public string WorkMinutes { get => _work; set { _work = value; Notify(); } }
    public string RestValue { get => _rest; set { _rest = value; Notify(); } }
    public string RestUnit
    {
        get => _restUnit;
        set
        {
            if (_restUnit == value) return;
            if (int.TryParse(_rest, out var amount))
            {
                var seconds = (long)amount * (_restUnit == "Minutes" ? 60 : 1);
                _rest = (seconds / (value == "Minutes" ? 60d : 1d)).ToString("0.##", CultureInfo.InvariantCulture);
            }
            _restUnit = value; Notify();
        }
    }
    public string SnoozeMinutes { get => _snooze; set { _snooze = value; Notify(); } }
    public bool PauseOnIdle { get => _idle; set { _idle = value; Notify(); } }
    public bool FullscreenDnd { get => _fullscreen; set { _fullscreen = value; Notify(); } }
    private string Identity => JsonSerializer.Serialize(new { _enabled, _work, _rest, _restUnit, _snooze, _idle, _fullscreen, _appearance });
    public bool IsDirty => _baseline != Identity;
    private HealthReminderSettings? Build()
    {
        if (!int.TryParse(_work, out var work) || !int.TryParse(_snooze, out var snooze) ||
            !int.TryParse(_rest, out var rest) || rest is < 0 or > 900) return null;
        return new()
        {
            Enabled = _enabled,
            WorkMinutes = work,
            SnoozeMinutes = snooze,
            BreakSeconds = rest * (_restUnit == "Minutes" ? 60 : 1),
            PauseOnIdle = _idle,
            FullscreenDnd = _fullscreen,
            Appearance = _appearance.Copy()
        };
    }
    public string? Error => !IsAvailable ? UiText.Get(_runtimeError() ?? _store.NoticeKey ?? "此模块暂不可用。") :
        Build() is not { } value ? UiText.Get("时长须为整数，请检查输入及单位。") :
        value.Validate().FirstOrDefault() is { } key ? UiText.Get(key) : null;
    public string Notice => UiText.Get(_feedback.Length > 0 ? _feedback : _store.NoticeKey ?? "");
    public string Status => UiText.Get(_service.Snapshot.ReasonKey) + (_service.Snapshot.PauseUntil is { } until ? " " + until.ToLocalTime().ToString("HH:mm", CultureInfo.InvariantCulture) : "");
    public string Remaining => _service.Snapshot.State == HealthReminderState.Disabled ? "—" :
        $"{(int)Math.Max(0, _service.Snapshot.Remaining.TotalMinutes):00}:{Math.Max(0, _service.Snapshot.Remaining.Seconds):00}";
    public string PauseText => UiText.Get(_service.Snapshot.IsManuallyPaused ? "恢复健康提醒" : "暂停提醒 1 小时");
    public bool TrySave()
    {
        var value = Build();
        if (Error != null || value == null || !_store.TrySave(value)) { Notify(); return false; }
        LeavePage();
        _saved = value.Copy(); _baseline = Identity; _feedback = "";
        _service.ApplySaved(value); Notify(); return true;
    }
    public void Discard() { LeavePage(); ResetFields(); Notify(); }
    public void LeavePage() { if (_service.IsPreviewVisible) _service.DismissWindow(); }
    private void ResetFields()
    {
        _enabled = _saved.Enabled; _work = _saved.WorkMinutes.ToString(CultureInfo.InvariantCulture);
        _restUnit = _saved.BreakSeconds % 60 == 0 ? "Minutes" : "Seconds";
        _rest = (_saved.BreakSeconds / (_restUnit == "Minutes" ? 60 : 1)).ToString(CultureInfo.InvariantCulture);
        _snooze = _saved.SnoozeMinutes.ToString(CultureInfo.InvariantCulture); _idle = _saved.PauseOnIdle; _fullscreen = _saved.FullscreenDnd;
        _appearance = _saved.Appearance.Copy();
        _baseline = Identity; _feedback = "";
    }
    private void Notify() { OnPropertyChanged(string.Empty); CommandManager.InvalidateRequerySuggested(); }
}
