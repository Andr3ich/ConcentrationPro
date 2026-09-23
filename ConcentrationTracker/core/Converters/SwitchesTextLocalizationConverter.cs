using ConcentrationTracker.Core.Services;
using ConcentrationTracker.MVVM.Model;
using System;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows.Data;

namespace ConcentrationTracker.Core.Converters
{
    public class SwitchesTextLocalizationConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string text = value == null ? string.Empty : value.ToString();

            if (string.IsNullOrWhiteSpace(text))
                return text;

            if (LocalizationService.CurrentLanguage != AppLanguage.Ukrainian)
                return text;

            string result = text;

            result = Regex.Replace(result, @"\btotal\b", "всього", RegexOptions.IgnoreCase);
            result = Regex.Replace(result, @"\bdisruptive\b", "відволікаючих", RegexOptions.IgnoreCase);
            result = Regex.Replace(result, @"\bswitches\b", "перемикань", RegexOptions.IgnoreCase);
            result = Regex.Replace(result, @"\bswitch\b", "перемикання", RegexOptions.IgnoreCase);

            return result;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Binding.DoNothing;
        }
    }
}
