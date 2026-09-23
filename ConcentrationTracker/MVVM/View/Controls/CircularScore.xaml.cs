using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ConcentrationTracker.MVVM.View.Controls
{
    public partial class CircularScore : UserControl
    {
        public static readonly DependencyProperty ScoreProperty =
            DependencyProperty.Register(
                nameof(Score),
                typeof(string),
                typeof(CircularScore),
                new PropertyMetadata("0", OnScoreChanged));

        public string Score
        {
            get { return (string)GetValue(ScoreProperty); }
            set { SetValue(ScoreProperty, value); }
        }

        public CircularScore()
        {
            InitializeComponent();

            SizeChanged +=
                CircularScore_SizeChanged;
        }

        private static void OnScoreChanged(
            DependencyObject dependencyObject,
            DependencyPropertyChangedEventArgs e)
        {
            CircularScore control =
                dependencyObject as CircularScore;

            if (control != null)
            {
                control.UpdateArc();
            }
        }

        private void CircularScore_SizeChanged(
            object sender,
            SizeChangedEventArgs e)
        {
            UpdateArc();
        }

        private void UpdateArc()
        {
            if (ActualWidth <= 0 ||
                ActualHeight <= 0 ||
                ScoreArc == null)
            {
                return;
            }

            double score =
                ParseScore(Score);

            score =
                Math.Max(0, Math.Min(100, score));

            if (score <= 0)
            {
                ScoreArc.Data =
                    Geometry.Empty;

                return;
            }

            double strokeThickness =
                ScoreArc.StrokeThickness;

            double radius =
                Math.Min(ActualWidth, ActualHeight) / 2 - strokeThickness / 2;

            Point center =
                new Point(
                    ActualWidth / 2,
                    ActualHeight / 2);

            double startAngle =
                -90;

            double endAngle =
                startAngle + score / 100.0 * 359.9;

            Point startPoint =
                PointOnCircle(
                    center,
                    radius,
                    startAngle);

            Point endPoint =
                PointOnCircle(
                    center,
                    radius,
                    endAngle);

            bool isLargeArc =
                endAngle - startAngle > 180;

            PathFigure figure =
                new PathFigure
                {
                    StartPoint =
                        startPoint,

                    IsClosed =
                        false
                };

            ArcSegment arc =
                new ArcSegment
                {
                    Point =
                        endPoint,

                    Size =
                        new Size(
                            radius,
                            radius),

                    SweepDirection =
                        SweepDirection.Clockwise,

                    IsLargeArc =
                        isLargeArc
                };

            figure.Segments.Add(
                arc);

            PathGeometry geometry =
                new PathGeometry();

            geometry.Figures.Add(
                figure);

            ScoreArc.Data =
                geometry;
        }

        private Point PointOnCircle(
            Point center,
            double radius,
            double angleDegrees)
        {
            double angleRadians =
                angleDegrees * Math.PI / 180.0;

            return new Point(
                center.X + radius * Math.Cos(angleRadians),
                center.Y + radius * Math.Sin(angleRadians));
        }

        private double ParseScore(
            string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return 0;

            string normalized =
                value
                    .Replace("%", string.Empty)
                    .Trim()
                    .Replace(',', '.');

            double result;

            if (double.TryParse(
                    normalized,
                    NumberStyles.Float,
                    CultureInfo.InvariantCulture,
                    out result))
            {
                return result;
            }

            return 0;
        }
    }
}
