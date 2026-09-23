using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace ConcentrationTracker.Core.Converters
{
    public class ScoreToArcGeometryConverter : IValueConverter
    {
        private const double Size = 230;
        private const double Center = Size / 2;
        private const double Radius = 78;

        public object Convert(
            object value,
            Type targetType,
            object parameter,
            CultureInfo culture)
        {
            double score = 0;

            if (value != null)
            {
                double.TryParse(
                    value.ToString(),
                    NumberStyles.Any,
                    CultureInfo.InvariantCulture,
                    out score);
            }

            if (score <= 0)
            {
                return Geometry.Empty;
            }

            if (score > 100)
            {
                score = 100;
            }

            if (score >= 100)
            {
                return new EllipseGeometry(
                    new Point(Center, Center),
                    Radius,
                    Radius);
            }

            double angle =
                score / 100.0 * 360.0;

            double startAngle =
                -90;

            double endAngle =
                startAngle + angle;

            Point startPoint =
                GetPointOnCircle(startAngle);

            Point endPoint =
                GetPointOnCircle(endAngle);

            bool isLargeArc =
                angle > 180;

            PathFigure figure =
                new PathFigure
                {
                    StartPoint =
                        startPoint,

                    IsClosed =
                        false
                };

            ArcSegment arcSegment =
                new ArcSegment
                {
                    Point =
                        endPoint,

                    Size =
                        new Size(Radius, Radius),

                    SweepDirection =
                        SweepDirection.Clockwise,

                    IsLargeArc =
                        isLargeArc
                };

            figure.Segments.Add(arcSegment);

            PathGeometry geometry =
                new PathGeometry();

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

        private Point GetPointOnCircle(
            double angleDegrees)
        {
            double angleRadians =
                angleDegrees * Math.PI / 180.0;

            double x =
                Center + Radius * Math.Cos(angleRadians);

            double y =
                Center + Radius * Math.Sin(angleRadians);

            return new Point(x, y);
        }
    }
}
