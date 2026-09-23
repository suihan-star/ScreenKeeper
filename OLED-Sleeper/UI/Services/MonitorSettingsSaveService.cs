using OLED_Sleeper.Features.UserSettings.Services.Interfaces;
using OLED_Sleeper.UI.Helpers;
using OLED_Sleeper.UI.Localization;
using OLED_Sleeper.UI.Services.Interfaces;
using OLED_Sleeper.UI.ViewModels;

namespace OLED_Sleeper.UI.Services
{
    /// <summary>
    /// Saves monitor settings through <see cref="IMonitorSettingsFileService"/>, reporting an invalid
    /// configuration with a modal dialog rather than writing it.
    /// </summary>
    public class MonitorSettingsSaveService : IMonitorSettingsSaveService
    {
        private readonly IMonitorSettingsFileService _settingsService;
        private readonly IDialogService _dialogService;

        public MonitorSettingsSaveService(
            IMonitorSettingsFileService settingsService,
            IDialogService dialogService)
        {
            _settingsService = settingsService;
            _dialogService = dialogService;
        }

        /// <inheritdoc />
        public bool TrySave(IReadOnlyList<MonitorLayoutViewModel> monitors)
            => TrySave(monitors, Features.UserSettings.Models.MonitorSettingsScope.All);

        public bool TrySave(IReadOnlyList<MonitorLayoutViewModel> monitors, Features.UserSettings.Models.MonitorSettingsScope scope)
        {
            var error = MonitorSettingsValidator.BuildValidationError(monitors, scope);
            if (error != null)
            {
                _dialogService.ShowError(error, UiText.Get("显示器设置有误"));
                return false;
            }

            if (!_settingsService.SaveSettings(monitors.Select(m => m.Configuration.ToSettings(scope)).ToList()))
            {
                _dialogService.ShowError(UiText.Get("请确认配置目录可写后重试。你的更改仍保留在当前窗口中。"), UiText.Get("设置未能保存"));
                return false;
            }

            foreach (var monitor in monitors)
            {
                monitor.Configuration.MarkAsSaved(scope);
            }

            return true;
        }
    }
}
