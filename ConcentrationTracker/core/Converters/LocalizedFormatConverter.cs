using ConcentrationTracker.Core.Services;
using System;
using System.Globalization;
using System.Windows.Data;

namespace ConcentrationTracker.Core.Converters
{
    public class LocalizedFormatConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string key = parameter == null ? string.Empty : parameter.ToString();

            if (string.IsNullOrWhiteSpace(key))
                return value == null ? string.Empty : value.ToString();

            object displayValue = value;

            if (value is string)
            {
                DurationTextLocalizationConverter durationConverter =
                    new DurationTextLocalizationConverter();

                displayValue = durationConverter.Convert(
                    value,
                    typeof(string),
                    null,
                    culture);
            }

            return LocalizationService.Format(key, displayValue);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Binding.DoNothing;
        }
    }
}
