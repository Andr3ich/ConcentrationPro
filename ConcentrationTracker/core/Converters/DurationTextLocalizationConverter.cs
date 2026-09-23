using ConcentrationTracker.Core.Services;
using ConcentrationTracker.MVVM.Model;
using System;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows.Data;

namespace ConcentrationTracker.Core.Converters
{
    public class DurationTextLocalizationConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string text = value == null ? string.Empty : value.ToString();

            if (string.IsNullOrWhiteSpace(text))
                return text;

            if (LocalizationService.CurrentLanguage != AppLanguage.Ukrainian)
                return text;

            string result = text;

            result = Regex.Replace(result, @"\b(hours|hour|hrs|hr)\b", "год", RegexOptions.IgnoreCase);
            result = Regex.Replace(result, @"\b(minutes|minute|mins|min)\b", "хв", RegexOptions.IgnoreCase);
            result = Regex.Replace(result, @"\b(seconds|second|secs|sec)\b", "сек", RegexOptions.IgnoreCase);

            result = Regex.Replace(result, @"(?<=\d)\s*h\b", " год", RegexOptions.IgnoreCase);
            result = Regex.Replace(result, @"(?<=\d)h\b", " год", RegexOptions.IgnoreCase);
            result = Regex.Replace(result, @"(?<=\d)\s*m\b", " хв", RegexOptions.IgnoreCase);
            result = Regex.Replace(result, @"(?<=\d)m\b", " хв", RegexOptions.IgnoreCase);
            result = Regex.Replace(result, @"(?<=\d)\s*s\b", " сек", RegexOptions.IgnoreCase);
            result = Regex.Replace(result, @"(?<=\d)s\b", " сек", RegexOptions.IgnoreCase);

            return result;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Binding.DoNothing;
        }
    }
}
