using System;
using System.Globalization;
using System.Windows.Data;

namespace ConcentrationTracker.Core.Converters
{
    public class WindowTitleCleanerConverter : IValueConverter, IMultiValueConverter
    {
        public object Convert(
            object value,
            Type targetType,
            object parameter,
            CultureInfo culture)
        {
            string title =
                value as string;

            return CleanTitle(title);
        }

        public object[] ConvertBack(
            object value,
            Type[] targetTypes,
            object parameter,
            CultureInfo culture)
        {
            return null;
        }

        public object Convert(
            object[] values,
            Type targetType,
            object parameter,
            CultureInfo culture)
        {
            string title =
                values != null && values.Length > 0
                    ? values[0] as string
                    : string.Empty;

            return CleanTitle(title);
        }

        public object ConvertBack(
            object value,
            Type targetType,
            object parameter,
            CultureInfo culture)
        {
            return Binding.DoNothing;
        }

        private string CleanTitle(string title)
        {
            if (string.IsNullOrWhiteSpace(title))
                return "Untitled Window";

            string cleaned =
                title.Trim();

            cleaned =
                RemoveActivationNoise(cleaned);

            cleaned =
                RemoveBrowserSuffixes(cleaned);

            cleaned =
                RemoveOfficeSuffixes(cleaned);

            cleaned =
                RemoveCommonNoise(cleaned);

            cleaned =
                NormalizeSpaces(cleaned);

            if (string.IsNullOrWhiteSpace(cleaned))
                return title.Trim();

            return cleaned;
        }

        private string RemoveActivationNoise(string title)
        {
            string result =
                title;

            result =
                ReplaceIgnoreCase(result, "(Product Activation Failed)", "");

            result =
                ReplaceIgnoreCase(result, "(не вдалося активувати продукт)", "");

            result =
                ReplaceIgnoreCase(result, "(не удалось активировать продукт)", "");

            result =
                ReplaceIgnoreCase(result, "(Сбой активации продукта)", "");

            result =
                ReplaceIgnoreCase(result, "Product Activation Failed", "");

            result =
                ReplaceIgnoreCase(result, "не вдалося активувати продукт", "");

            result =
                ReplaceIgnoreCase(result, "не удалось активировать продукт", "");

            result =
                ReplaceIgnoreCase(result, "Сбой активации продукта", "");

            return result;
        }

        private string RemoveBrowserSuffixes(string title)
        {
            string result =
                title;

            result =
                RemoveKnownEnding(result, " - Google Chrome");

            result =
                RemoveKnownEnding(result, " — Google Chrome");

            result =
                RemoveKnownEnding(result, " – Google Chrome");

            result =
                RemoveKnownEnding(result, " - Microsoft Edge");

            result =
                RemoveKnownEnding(result, " — Microsoft Edge");

            result =
                RemoveKnownEnding(result, " – Microsoft Edge");

            result =
                RemoveKnownEnding(result, " - Mozilla Firefox");

            result =
                RemoveKnownEnding(result, " — Mozilla Firefox");

            result =
                RemoveKnownEnding(result, " – Mozilla Firefox");

            result =
                RemoveKnownEnding(result, " - Opera");

            result =
                RemoveKnownEnding(result, " — Opera");

            result =
                RemoveKnownEnding(result, " - Brave");

            result =
                RemoveKnownEnding(result, " — Brave");

            return result;
        }

        private string RemoveOfficeSuffixes(string title)
        {
            string result =
                title;

            result =
                RemoveKnownEnding(result, " - Word");

            result =
                RemoveKnownEnding(result, " — Word");

            result =
                RemoveKnownEnding(result, " - Microsoft Word");

            result =
                RemoveKnownEnding(result, " — Microsoft Word");

            result =
                RemoveKnownEnding(result, " - Excel");

            result =
                RemoveKnownEnding(result, " — Excel");

            result =
                RemoveKnownEnding(result, " - PowerPoint");

            result =
                RemoveKnownEnding(result, " — PowerPoint");

            result =
                RemoveKnownEnding(result, " - OneNote");

            result =
                RemoveKnownEnding(result, " — OneNote");

            return result;
        }

        private string RemoveCommonNoise(string title)
        {
            string result =
                title;

            result =
                ReplaceIgnoreCase(result, "Особистий: Microsoft Edge", "");

            result =
                ReplaceIgnoreCase(result, "Personal: Microsoft Edge", "");

            result =
                ReplaceIgnoreCase(result, "InPrivate", "");

            result =
                ReplaceIgnoreCase(result, "Incognito", "");

            return result;
        }

        private string RemoveKnownEnding(
            string value,
            string ending)
        {
            if (string.IsNullOrWhiteSpace(value) ||
                string.IsNullOrWhiteSpace(ending))
            {
                return value;
            }

            if (value.EndsWith(ending, StringComparison.OrdinalIgnoreCase))
            {
                return value.Substring(0, value.Length - ending.Length).Trim();
            }

            return value;
        }

        private string ReplaceIgnoreCase(
            string source,
            string oldValue,
            string newValue)
        {
            if (string.IsNullOrEmpty(source) ||
                string.IsNullOrEmpty(oldValue))
            {
                return source;
            }

            int index =
                source.IndexOf(oldValue, StringComparison.OrdinalIgnoreCase);

            while (index >= 0)
            {
                source =
                    source.Remove(index, oldValue.Length)
                          .Insert(index, newValue);

                index =
                    source.IndexOf(oldValue, StringComparison.OrdinalIgnoreCase);
            }

            return source;
        }

        private string NormalizeSpaces(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            string result =
                value.Trim();

            while (result.Contains("  "))
            {
                result =
                    result.Replace("  ", " ");
            }

            result =
                result.Trim(' ', '-', '—', '–', ':');

            return result.Trim();
        }
    }
}
