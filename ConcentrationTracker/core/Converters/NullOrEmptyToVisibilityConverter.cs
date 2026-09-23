using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace ConcentrationTracker.Core.Converters
{
    public class NullOrEmptyToVisibilityConverter : IValueConverter
    {
        public object Convert(
            object value,
            Type targetType,
            object parameter,
            CultureInfo culture)
        {
            bool isEmpty =
                value == null ||
                string.IsNullOrWhiteSpace(value.ToString());

            bool invert =
                parameter != null &&
                parameter.ToString() == "Invert";

            if (invert)
                isEmpty = !isEmpty;

            return isEmpty
                ? Visibility.Collapsed
                : Visibility.Visible;
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
