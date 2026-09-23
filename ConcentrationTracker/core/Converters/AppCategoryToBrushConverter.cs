using ConcentrationTracker.MVVM.Model;
using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace ConcentrationTracker.Core.Converters
{
    public class AppCategoryToBrushConverter : IValueConverter
    {
        public object Convert(
            object value,
            Type targetType,
            object parameter,
            CultureInfo culture)
        {
            if (!(value is AppCategory))
                return new SolidColorBrush(Color.FromRgb(71, 85, 105));

            AppCategory category =
                (AppCategory)value;

            switch (category)
            {
                case AppCategory.Productive:
                    return new SolidColorBrush(Color.FromRgb(22, 163, 74));

                case AppCategory.Communication:
                    return new SolidColorBrush(Color.FromRgb(37, 99, 235));

                case AppCategory.Distraction:
                    return new SolidColorBrush(Color.FromRgb(225, 29, 72));

                case AppCategory.System:
                    return new SolidColorBrush(Color.FromRgb(71, 85, 105));

                case AppCategory.Neutral:
                    return new SolidColorBrush(Color.FromRgb(75, 85, 99));

                default:
                    return new SolidColorBrush(Color.FromRgb(71, 85, 105));
            }
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
