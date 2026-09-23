using OLED_Sleeper.UI.Localization;
using OLED_Sleeper.UI.ViewModels;
using System.Text;

namespace OLED_Sleeper.UI.Helpers
{
    /// <summary>
    /// Provides validation logic for monitor settings.
    /// This is a static helper class and does not hold any state.
    /// </summary>
    public static class MonitorSettingsValidator
    {
        /// <summary>
        /// Validates the provided monitor settings.
        /// </summary>
        /// <param name="monitors">The collection of monitor layout view models to validate.</param>
        /// <returns>An error message describing every invalid monitor, or null when all are valid.</returns>
        public static string? BuildValidationError(IEnumerable<MonitorLayoutViewModel> monitors, Features.UserSettings.Models.MonitorSettingsScope scope = Features.UserSettings.Models.MonitorSettingsScope.All)
            => BuildConfigurationError(monitors.Select(m => m.Configuration.ForScope(scope)));

        public static string? BuildConfigurationError(IEnumerable<MonitorConfigurationViewModel> monitors)
        {
            var list = monitors.ToList();
            var applications = list.Where(m => m.IsApplication).ToArray();
            for (int i = 0; i < applications.Length; i++)
                for (int j = i + 1; j < applications.Length; j++)
                    if (OLED_Sleeper.Features.MonitorPrivacy.PrivacyOptions.Conflicts(applications[i].Options, applications[j].Options))
                        return UiText.Get("多个屏幕不能使用同一个应用窗口，请分别选择不同窗口。");
            var invalidMonitors = GetInvalidMonitors(list);

            return invalidMonitors.Count > 0 ? BuildMessage(invalidMonitors) : null;
        }

        /// <summary>
        /// Returns a list of monitors that are managed but have invalid configuration.
        /// </summary>
        /// <param name="monitors">The collection of monitor layout view models to check.</param>
        /// <returns>A list of invalid monitor view models.</returns>
        private static List<MonitorConfigurationViewModel> GetInvalidMonitors(IEnumerable<MonitorConfigurationViewModel> monitors)
        {
            return monitors
                .Where(m => !m.IsValid)
                .ToList();
        }

        /// <summary>
        /// Builds the validation error message for the provided invalid monitors.
        /// </summary>
        /// <param name="invalidMonitors">The list of invalid monitor view models.</param>
        /// <returns>The assembled error message.</returns>
        private static string BuildMessage(List<MonitorConfigurationViewModel> invalidMonitors)
        {
            var errorBuilder = new StringBuilder();
            errorBuilder.AppendLine(UiText.Get("以下显示器的设置有误，无法保存："));
            foreach (var monitor in invalidMonitors)
            {
                errorBuilder.AppendLine($" - {monitor.MonitorTitle}:");
                var config = monitor;
                if (!string.IsNullOrWhiteSpace(config.PrivacyError))
                    errorBuilder.AppendLine($"     • {config.PrivacyError}");
                if (!string.IsNullOrWhiteSpace(config.BehaviorError))
                    errorBuilder.AppendLine($"     • {config.BehaviorError}");
                if (!string.IsNullOrWhiteSpace(config.IdleValueError))
                    errorBuilder.AppendLine($"     • {config.IdleValueError}");
                if (!string.IsNullOrWhiteSpace(config.ActiveConditionsError))
                    errorBuilder.AppendLine($"     • {config.ActiveConditionsError}");
            }
            errorBuilder.AppendLine(UiText.Get("\n请修正标记的设置项，或取消勾选对应显示器以停止管理。"));

            return errorBuilder.ToString();
        }
    }
}
