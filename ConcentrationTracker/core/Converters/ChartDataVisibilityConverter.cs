using ConcentrationTracker.MVVM.Model;
using System;
using System.Collections;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Data;

namespace ConcentrationTracker.Core.Converters
{
    public class ChartDataVisibilityConverter : IValueConverter
    {
        public object Convert(
            object value,
            Type targetType,
            object parameter,
            CultureInfo culture)
        {
            bool hasData = false;

            IEnumerable collection =
                value as IEnumerable;

            if (collection != null)
            {
                foreach (object item in collection)
                {
                    SwitchChartItemModel switchItem =
                        item as SwitchChartItemModel;

                    if (switchItem != null &&
                        switchItem.SwitchCount > 0)
                    {
                        hasData = true;
                        break;
                    }

                    ScoreChartPointModel scorePoint =
                        item as ScoreChartPointModel;

                    if (scorePoint != null)
                    {
                        hasData = true;
                        break;
                    }
                }
            }

            bool invert =
                parameter != null &&
                parameter.ToString() == "Invert";

            if (invert)
            {
                hasData = !hasData;
            }

            return hasData
                ? Visibility.Visible
                : Visibility.Collapsed;
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
