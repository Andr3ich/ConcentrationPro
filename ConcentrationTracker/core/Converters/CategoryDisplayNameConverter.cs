using ConcentrationTracker.Core.Services;
using System;
using System.Globalization;
using System.Windows.Data;

namespace ConcentrationTracker.Core.Converters
{
    public class CategoryDisplayNameConverter : IValueConverter
    {
        public object Convert(
            object value,
            Type targetType,
            object parameter,
            CultureInfo culture)
        {
            if (value == null)
                return string.Empty;

            string category =
                value.ToString();

            switch (category)
            {
                case "Productive":
                    return LocalizationService.GetString("Category_Productive");

                case "Neutral":
                    return LocalizationService.GetString("Category_Neutral");

                case "Communication":
                    return LocalizationService.GetString("Category_Communication");

                case "Distraction":
                    return LocalizationService.GetString("Category_Distraction");

                case "System":
                    return LocalizationService.GetString("Category_System");

                default:
                    return category;
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
