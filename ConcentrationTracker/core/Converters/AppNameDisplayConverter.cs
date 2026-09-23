using ConcentrationTracker.Core.Services;
using System;
using System.Globalization;
using System.Windows.Data;

namespace ConcentrationTracker.Core.Converters
{
    public class AppNameDisplayConverter : IValueConverter
    {
        public object Convert(
            object value,
            Type targetType,
            object parameter,
            CultureInfo culture)
        {
            string appName =
                value as string;

            return AppDisplayNameService.GetDisplayName(appName);
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
