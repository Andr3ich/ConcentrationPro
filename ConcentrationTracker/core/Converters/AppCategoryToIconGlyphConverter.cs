using ConcentrationTracker.MVVM.Model;
using System;
using System.Globalization;
using System.Windows.Data;

namespace ConcentrationTracker.Core.Converters
{
    public class AppCategoryToIconGlyphConverter : IValueConverter
    {
        public object Convert(
            object value,
            Type targetType,
            object parameter,
            CultureInfo culture)
        {
            if (!(value is AppCategory))
                return "●";

            AppCategory category =
                (AppCategory)value;

            switch (category)
            {
                case AppCategory.Productive:
                    return "💼";

                case AppCategory.Communication:
                    return "💬";

                case AppCategory.Distraction:
                    return "⚠";

                case AppCategory.System:
                    return "⚙";

                case AppCategory.Neutral:
                    return "●";

                default:
                    return "●";
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
