using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace ConcentrationTracker.Core.Converters
{
    public class PositiveNumberToVisibilityConverter : IValueConverter
    {
        public object Convert(
            object value,
            Type targetType,
            object parameter,
            CultureInfo culture)
        {
            int number = 0;

            if (value is int)
            {
                number = (int)value;
            }
            else if (value is double)
            {
                number = (int)(double)value;
            }
            else if (value != null)
            {
                int.TryParse(value.ToString(), out number);
            }

            bool invert =
                parameter != null &&
                parameter.ToString() == "Invert";

            bool isPositive = number > 0;

            if (invert)
                isPositive = !isPositive;

            return isPositive
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