using ConcentrationTracker.MVVM.Model;
using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace ConcentrationTracker.Core.Converters
{
    public class AppCategoryToLightBrushConverter : IValueConverter
    {
        public object Convert(
            object value,
            Type targetType,
            object parameter,
            CultureInfo culture)
        {
            if (!(value is AppCategory))
                return new SolidColorBrush(Color.FromRgb(241, 245, 249));

            AppCategory category =
                (AppCategory)value;

            switch (category)
            {
                case AppCategory.Productive:
                    return new SolidColorBrush(Color.FromRgb(220, 252, 231));

                case AppCategory.Communication:
                    return new SolidColorBrush(Color.FromRgb(219, 234, 254));

                case AppCategory.Distraction:
                    return new SolidColorBrush(Color.FromRgb(255, 228, 230));

                case AppCategory.System:
                    return new SolidColorBrush(Color.FromRgb(226, 232, 240));

                case AppCategory.Neutral:
                    return new SolidColorBrush(Color.FromRgb(229, 231, 235));

                default:
                    return new SolidColorBrush(Color.FromRgb(241, 245, 249));
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
