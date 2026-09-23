using ConcentrationTracker.MVVM.Model;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace ConcentrationTracker.Core.Converters
{
    public class ScorePointsToPointCollectionConverter : IValueConverter
    {
        public object Convert(
            object value,
            Type targetType,
            object parameter,
            CultureInfo culture)
        {
            PointCollection points =
                new PointCollection();

            IEnumerable<ScoreChartPointModel> chartPoints =
                value as IEnumerable<ScoreChartPointModel>;

            if (chartPoints == null)
                return points;

            foreach (ScoreChartPointModel point in chartPoints)
            {
                points.Add(
                    new Point(
                        point.X,
                        point.Y));
            }

            return points;
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
