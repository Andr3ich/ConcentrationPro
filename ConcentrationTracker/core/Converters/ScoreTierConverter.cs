using System;
using System.Globalization;
using System.Windows.Data;

namespace ConcentrationTracker.Core.Converters
{
    public class ScoreTierConverter : IValueConverter
    {
        public object Convert(
            object value,
            Type targetType,
            object parameter,
            CultureInfo culture)
        {
            int score = 0;

            if (value != null)
            {
                int.TryParse(
                    value.ToString(),
                    NumberStyles.Integer,
                    CultureInfo.InvariantCulture,
                    out score);
            }

            if (score >= 80)
                return 0;

            if (score >= 60)
                return 1;

            if (score >= 40)
                return 2;

            return 3;
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
