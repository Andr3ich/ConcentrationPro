using ConcentrationTracker.Core.Services;
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace ConcentrationTracker.Core.Converters
{
    public class ProcessPathIconVisibilityConverter : IValueConverter
    {
        public object Convert(
            object value,
            Type targetType,
            object parameter,
            CultureInfo culture)
        {
            string processPath =
                value as string;

            bool hasIcon =
                AppIconService.GetIconFromPath(processPath) != null;

            bool invert =
                parameter != null &&
                parameter.ToString() == "Invert";

            if (invert)
                hasIcon = !hasIcon;

            return hasIcon
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        public object ConvertBack(
            object value,
            Type targetType,
            object parameter,
            CultureInfo culture)
        {
            return Binding.DoNothing;
        }
    }
}
