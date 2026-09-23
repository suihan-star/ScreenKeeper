using OLED_Sleeper.Features.MonitorPrivacy;
using OLED_Sleeper.UI.Localization;

namespace OLED_Sleeper.UI.ViewModels;

public partial class MainViewModel
{
    private PrivacyRuntimeSnapshot PrivacySnapshot => _privacy?.GetSnapshot(SelectedMonitor?.HardwareId) ??
        new(PrivacyRuntimeState.Off, 0, "保存并启用本屏隐私保护后，可使用立即保护和暂缓。");
    public bool PrivacyRuntimeError => PrivacySnapshot.State == PrivacyRuntimeState.Unavailable;
    public string PrivacyRuntimeTitle
    {
        get
        {
            var state = PrivacySnapshot;
            return state.State switch
            {
                PrivacyRuntimeState.Waiting => string.Format(UiText.Get("等待保护 · {0} 秒"), state.RemainingSeconds),
                PrivacyRuntimeState.InUse => UiText.Get("正在使用"),
                PrivacyRuntimeState.Quiet => UiText.Get("免打扰中 · 暂不保护"),
                PrivacyRuntimeState.Paused => UiText.Get("已暂缓"),
                PrivacyRuntimeState.Protected => UiText.Get("已保护"),
                PrivacyRuntimeState.Unavailable => UiText.Get("请检查保护状态"),
                _ => UiText.Get("未开启")
            };
        }
    }
    public string PrivacyRuntimeDetail
    {
        get
        {
            var state = PrivacySnapshot;
            var pause = SnoozeNotice(Features.UserSettings.Models.MonitorSettingsScope.Privacy);
            if (state.State == PrivacyRuntimeState.Protected && pause.Length > 0)
                return pause + " · " + UiText.Get("已有隐私幕仍需手动进入。");
            if (state.State == PrivacyRuntimeState.Paused && pause.Length > 0) return pause;
            return UiText.Get(state.Message);
        }
    }
}
