using OLED_Sleeper.Features.ColorTemperature;
using OLED_Sleeper.Features.MonitorInformation.Models;
using OLED_Sleeper.UI.Localization;
using System.Text.RegularExpressions;

namespace OLED_Sleeper.UI.Helpers
{
    /// <summary>
    /// Builds the title a monitor is shown under. This is a static helper class and does not hold any state.
    /// </summary>
    public static class MonitorTitleFormatter
    {
        /// <summary>
        /// Prefers the model, disambiguating shared models and cloned surfaces with Windows display numbers.
        /// </summary>
        /// <param name="monitor">The monitor to title.</param>
        /// <returns>The title, marked as the primary display where applicable.</returns>
        public static string Format(MonitorInfo monitor)
        {
            string number = string.IsNullOrEmpty(monitor.TopologyLabel)
                ? monitor.DisplayNumber.ToString()
                : monitor.TopologyLabel;

            string title = FormatModel(monitor.ModelName, number, monitor.HasSharedModelName || !string.IsNullOrEmpty(monitor.TopologyLabel));
            return title + (monitor.IsPrimary ? UiText.Get("（主屏）") : "");
        }

        /// <summary>Uses all connected capabilities, including unverified ones, so filtering does not renumber a model.</summary>
        public static string Format(ColorDisplayScope scope, IReadOnlyList<ColorCapability> capabilities)
        {
            bool shared = capabilities.Count(c => string.Equals(c.Scope.DisplayName.Trim(), scope.DisplayName.Trim(), StringComparison.OrdinalIgnoreCase)) > 1;
            string number = Regex.Match(scope.DeviceName, @"\d+$").Value;
            if (number.Length == 0) number = (capabilities.ToList().FindIndex(c => c.Scope == scope) + 1).ToString();
            return FormatModel(scope.DisplayName, number, shared);
        }

        private static string FormatModel(string model, string number, bool shared) => string.IsNullOrWhiteSpace(model)
            ? string.Format(UiText.Get("显示器 {0}"), number)
            : model.Trim() + (shared ? " · " + number : "");
    }
}
