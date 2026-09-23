using ConcentrationTracker.MVVM.Model;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace ConcentrationTracker.Core.Converters
{
    public class ScorePointsToSmoothPathConverter : IValueConverter
    {
        public object Convert(
            object value,
            Type targetType,
            object parameter,
            CultureInfo culture)
        {
            IEnumerable<ScoreChartPointModel> source =
                value as IEnumerable<ScoreChartPointModel>;

            PathGeometry geometry =
                new PathGeometry();

            if (source == null)
                return geometry;

            List<ScoreChartPointModel> points =
                source.ToList();

            if (points.Count == 0)
                return geometry;

            PathFigure figure =
                new PathFigure();

            figure.StartPoint =
                new Point(points[0].X, points[0].Y);

            if (points.Count == 1)
            {
                geometry.Figures.Add(figure);
                return geometry;
            }

            for (int i = 0; i < points.Count - 1; i++)
            {
                Point p0 =
                    i == 0
                        ? new Point(points[i].X, points[i].Y)
                        : new Point(points[i - 1].X, points[i - 1].Y);

                Point p1 =
                    new Point(points[i].X, points[i].Y);

                Point p2 =
                    new Point(points[i + 1].X, points[i + 1].Y);

                Point p3 =
                    i + 2 < points.Count
                        ? new Point(points[i + 2].X, points[i + 2].Y)
                        : p2;

                double smoothness = 0.18;

                Point control1 =
                    new Point(
                        p1.X + (p2.X - p0.X) * smoothness,
                        p1.Y + (p2.Y - p0.Y) * smoothness);

                Point control2 =
                    new Point(
                        p2.X - (p3.X - p1.X) * smoothness,
                        p2.Y - (p3.Y - p1.Y) * smoothness);

                BezierSegment segment =
                    new BezierSegment(
                        control1,
                        control2,
                        p2,
                        true);

                figure.Segments.Add(segment);
            }

            geometry.Figures.Add(figure);

            return geometry;
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
