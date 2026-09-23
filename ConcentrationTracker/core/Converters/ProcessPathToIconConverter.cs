using ConcentrationTracker.Core.Services;
using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace ConcentrationTracker.Core.Converters
{
    public class ProcessPathToIconConverter : IValueConverter
    {
        public object Convert(
            object value,
            Type targetType,
            object parameter,
            CultureInfo culture)
        {
            string processPath =
                value as string;

            ImageSource icon =
                AppIconService.GetIconFromPath(processPath);

            return icon;
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
