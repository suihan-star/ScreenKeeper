using OLED_Sleeper.UI.Localization;
using OLED_Sleeper.UI.Models;
using System.Globalization;
using System.Windows.Data;

namespace OLED_Sleeper.UI.Converters
{
    /// <summary>
    /// Translates presentation only; selection and duration calculations retain the original enum.
    /// </summary>
    public class TimeUnitDisplayConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) => value switch
        {
            TimeUnit.Seconds => UiText.Get("秒"),
            TimeUnit.Minutes => UiText.Get("分钟"),
            TimeUnit.Hours => UiText.Get("小时"),
            _ => string.Empty
        };

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => Binding.DoNothing;
    }
}
