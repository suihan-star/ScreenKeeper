using OLED_Sleeper.UI.Commands;
using OLED_Sleeper.UI.Localization;
using System.Windows.Input;
using System.Windows.Threading;

namespace OLED_Sleeper.UI.ViewModels;

public sealed partial class ColorSettingsViewModel
{
    private Func<string, int?, CancellationToken, Task<bool>>? _livePreview;
    private DispatcherTimer _previewTimer = null!;
    private DispatcherTimer _previewFeedbackTimer = null!;
    private Func<Features.ColorTemperature.ColorPreviewStatus?>? _previewStatusReader;
    private CancellationTokenSource _previewCancellation = new();
    private ColorScreenViewModel? _previewScreen;
    private PreviewEdit? _pendingPreview;
    private bool _sendingPreview, _trialRequested, _previewActive, _comparing, _lastPreviewWasWork;
    private long _previewEditVersion;
    private string? _previewMessage;
    public ICommand PreviewWorkCommand { get; private set; } = null!;
    public ICommand PreviewRestCommand { get; private set; } = null!;
    public ColorScreenViewModel? PreviewScreen
    {
        get => _previewScreen;
        set
        {
            if (ReferenceEquals(value, _previewScreen)) return;
            _previewScreen = value;
            _ = EndPreviewAsync();
            Notify();
        }
    }
    public bool CanLivePreview => CanEdit && Enabled && _livePreview != null && _previewScreen?.CanPreview == true;
    public bool HasPreviewScreen => _previewScreen != null;
    public string PreviewScreenStatus => _previewScreen == null ? UiText.Get("请选择屏幕查看状态；尚未验证的屏幕也可选择。") :
        !IsAvailable ? Notice : _previewScreen.CanPreview ? _previewScreen.Status : _previewScreen.PreviewUnavailableReason;
    public bool CanCompareOriginal => CanLivePreview && _trialRequested && _previewActive;
    private string ActivePreviewMessage
    {
        get
        {
            var status = _previewStatusReader?.Invoke();
            if (status == null || status.HardwareId != _previewScreen?.HardwareId)
                return UiText.Get("正在试调所选屏幕，停止调整 15 秒后恢复当前策略。");
            var target = status.Kelvin is { } kelvin ? kelvin + " K" : UiText.Get("原色");
            return _previewScreen.Name + " · " + target + " · " + (status.Remaining <= TimeSpan.Zero
                ? UiText.Get("正在恢复已保存策略…")
                : Math.Ceiling(status.Remaining.TotalSeconds) + " s " + UiText.Get("后恢复已保存策略"));
        }
    }
    public string LivePreviewMessage => !CanLivePreview ? PreviewScreenStatus : UiText.Get(_previewMessage ?? (
        _comparing ? "正在对比原色，松开回到刚才的试调值。" : _trialRequested ? ActivePreviewMessage :
        "点击开始试调后再拖动滑杆；停止调整 15 秒后恢复已保存设置。"));

    private void InitializePreview(Func<string, int?, CancellationToken, Task<bool>>? livePreview)
    {
        _livePreview = livePreview;
        _previewFeedbackTimer = new DispatcherTimer(DispatcherPriority.Background) { Interval = TimeSpan.FromSeconds(1) };
        _previewFeedbackTimer.Tick += (_, _) => OnPropertyChanged(nameof(LivePreviewMessage));
        _previewTimer = new DispatcherTimer(DispatcherPriority.Background) { Interval = TimeSpan.FromMilliseconds(150) };
        _previewTimer.Tick += async (_, _) => await SendPendingPreviewAsync();
        PreviewWorkCommand = new RelayCommand(() => QueuePreview(true, true), () => CanLivePreview);
        PreviewRestCommand = new RelayCommand(() => QueuePreview(false, true), () => CanLivePreview);
    }

    private void QueuePreview(bool work, bool start = false)
    {
        _lastPreviewWasWork = work;
        if (!CanLivePreview || (!start && !_trialRequested)) return;
        int? kelvin = null;
        if (!(IsAutomatic && (work ? DayUsesOriginal : NightUsesOriginal)))
        {
            if (!int.TryParse(work ? _dayKelvin : _kelvin, out var value) || value is < 3500 or > 6500)
            { _ = EndPreviewAsync(); return; }
            kelvin = value;
        }
        EnqueuePreview(_comparing ? null : kelvin);
    }

    private void EnqueuePreview(int? kelvin)
    {
        if (!CanLivePreview) return;
        _trialRequested = true; _previewMessage = null;
        _pendingPreview = new(_previewScreen!.HardwareId, kelvin, ++_previewEditVersion);
        if (!_sendingPreview) _previewTimer.Start();
        Notify();
    }

    private async Task SendPendingPreviewAsync()
    {
        _previewTimer.Stop();
        if (_sendingPreview || _pendingPreview is not { } edit || !CanLivePreview) return;
        _pendingPreview = null; _sendingPreview = true;
        try
        {
            bool applied = await _livePreview!(edit.HardwareId, edit.Kelvin, _previewCancellation.Token);
            if (edit.Version != _previewEditVersion) return;
            _previewActive = applied;
            if (applied) _previewFeedbackTimer.Start(); else _previewFeedbackTimer.Stop();
            if (!applied)
            {
                _trialRequested = false; _comparing = false;
                _previewMessage = "试调未生效，请检查所选屏幕状态。";
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception)
        {
            if (edit.Version == _previewEditVersion)
            {
                await EndPreviewAsync();
                _previewMessage = "试调未生效，请检查所选屏幕状态。";
            }
        }
        finally
        {
            _sendingPreview = false;
            if (_pendingPreview != null) _previewTimer.Start();
            Notify();
        }
    }

    public void BeginOriginalComparison()
    {
        if (!CanCompareOriginal || _comparing) return;
        _comparing = true;
        EnqueuePreview(null);
    }
    public void EndOriginalComparison()
    {
        if (!_comparing) return;
        _comparing = false;
        if (_trialRequested) QueuePreview(_lastPreviewWasWork);
        Notify();
    }
    public async Task EndPreviewAsync()
    {
        ++_previewEditVersion;
        _previewTimer.Stop(); _previewFeedbackTimer.Stop(); _pendingPreview = null;
        _trialRequested = false; _previewActive = false; _comparing = false; _previewMessage = null;
        _previewCancellation.Cancel(); _previewCancellation.Dispose(); _previewCancellation = new();
        Notify();
        try { await _cancelPreview(); }
        catch (Exception) { _previewMessage = "结束试调失败，请检查屏幕恢复状态。"; Notify(); }
    }
    public void ObservePreview(string? hardwareId)
    {
        _previewActive = hardwareId != null && string.Equals(hardwareId, _previewScreen?.HardwareId, StringComparison.OrdinalIgnoreCase);
        if (!_previewActive) _previewFeedbackTimer.Stop();
        if (!_previewActive && !_sendingPreview && _pendingPreview == null) { _trialRequested = false; _comparing = false; }
        Notify();
    }
    private sealed record PreviewEdit(string HardwareId, int? Kelvin, long Version);
}
