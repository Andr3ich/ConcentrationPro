using ConcentrationTracker.Core.Services;
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace ConcentrationTracker.Core.Converters
{
    public class ActivityIconResolverConverter : IMultiValueConverter
    {
        public object Convert(
            object[] values,
            Type targetType,
            object parameter,
            CultureInfo culture)
        {
            string appName = GetString(values, 0);
            string windowTitle = GetString(values, 1);
            string processPath = GetString(values, 2);
            string packageInstallPath = GetString(values, 3);
            string appUserModelId = GetString(values, 4);
            string packageFullName = GetString(values, 5);

            ImageSource icon =
                ActivityIconResolverService.ResolveIcon(
                    appName,
                    windowTitle,
                    processPath,
                    packageInstallPath,
                    appUserModelId,
                    packageFullName);

            string mode =
                parameter?.ToString() ?? string.Empty;

            if (mode == "HasIcon")
                return icon != null ? Visibility.Visible : Visibility.Collapsed;

            if (mode == "NoIcon")
                return icon == null ? Visibility.Visible : Visibility.Collapsed;

            return icon;
        }

        public object[] ConvertBack(
            object value,
            Type[] targetTypes,
            object parameter,
            CultureInfo culture)
        {
            return null;
        }

        private string GetString(
            object[] values,
            int index)
        {
            if (values == null ||
                values.Length <= index ||
                values[index] == null)
            {
                return string.Empty;
            }

            return values[index].ToString();
        }
    }
}
