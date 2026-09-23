using System;
using System.Globalization;
using System.Windows.Data;

namespace ConcentrationTracker.Core.Converters
{
    public class CurrentActivityStatusTextConverter : IMultiValueConverter
    {
        public object Convert(
            object[] values,
            Type targetType,
            object parameter,
            CultureInfo culture)
        {
            string appName =
                values != null && values.Length > 0
                    ? values[0] as string
                    : null;

            string windowTitle =
                values != null && values.Length > 1
                    ? values[1] as string
                    : null;

            if (string.Equals(
                    appName,
                    "Dashboard",
                    StringComparison.OrdinalIgnoreCase))
            {
                return "Dashboard is open. Session timer continues; dashboard time is excluded from focus metrics.";
            }

            if (string.Equals(
                    appName,
                    "Idle",
                    StringComparison.OrdinalIgnoreCase))
            {
                return "No keyboard or mouse input detected. Away time is saved separately.";
            }

            if (string.Equals(
                    windowTitle,
                    "ConcentrationPro is open. This time is not counted.",
                    StringComparison.OrdinalIgnoreCase))
            {
                return "Dashboard is open. Session timer continues; dashboard time is excluded from focus metrics.";
            }

            if (string.Equals(
                    windowTitle,
                    "No keyboard or mouse input detected. Idle time is not counted.",
                    StringComparison.OrdinalIgnoreCase))
            {
                return "No keyboard or mouse input detected. Away time is saved separately.";
            }

            if (string.IsNullOrWhiteSpace(windowTitle))
                return "Tracking current activity.";

            return windowTitle;
        }

        public object[] ConvertBack(
            object value,
            Type[] targetTypes,
            object parameter,
            CultureInfo culture)
        {
            return null;
        }
    }
}
