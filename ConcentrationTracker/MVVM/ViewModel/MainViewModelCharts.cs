using ConcentrationTracker.Core.Services;
using ConcentrationTracker.MVVM.Model;
using ConcentrationTracker.MVVM.View.Windows;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Threading;

namespace ConcentrationTracker.MVVM.ViewModel
{
    public partial class MainViewModel
    {
        private void UpdateChartData()
        {
            TryAddScoreSample();
            SwitchesPlotModel = BuildSwitchesPlotModel();
            ScorePlotModel = BuildScorePlotModel();
        }

        private PlotModel BuildSwitchesPlotModel()
        {
            PlotModel model = CreateBasePlotModel();

            int firstVisibleIntervalIndex = GetFirstVisibleChartIntervalIndex();
            List<int> normalSwitchCounts = new List<int>();
            List<int> disruptiveSwitchCounts = new List<int>();
            List<int> totalSwitchCounts = new List<int>();

            for (int i = 0; i < MaxChartItems; i++)
            {
                int intervalIndex = firstVisibleIntervalIndex + i;
                DateTime intervalStart = _sessionStartTime + TimeSpan.FromTicks(_chartInterval.Ticks * intervalIndex);
                DateTime intervalEnd = intervalStart + _chartInterval;

                int normalCount = 0;
                int disruptiveCount = 0;

                if (_hasSessionStarted)
                {
                    normalCount = _switchEvents.Count(x =>
                        x.Timestamp >= intervalStart &&
                        x.Timestamp < intervalEnd &&
                        !x.IsDisruptive);

                    disruptiveCount = _switchEvents.Count(x =>
                        x.Timestamp >= intervalStart &&
                        x.Timestamp < intervalEnd &&
                        x.IsDisruptive);

                    if (_switchEvents.Count == 0)
                    {
                        normalCount = _switchTimestamps.Count(x =>
                            x >= intervalStart &&
                            x < intervalEnd);
                    }
                }

                normalSwitchCounts.Add(normalCount);
                disruptiveSwitchCounts.Add(disruptiveCount);
                totalSwitchCounts.Add(normalCount + disruptiveCount);
            }

            int maxCount = totalSwitchCounts.Count > 0
                ? totalSwitchCounts.Max()
                : 0;

            int yAxisMaximum = Math.Max(
                SwitchChartYAxisMax,
                RoundUpToStep(maxCount + 8, 5));

            LinearAxis xAxis = new LinearAxis
            {
                Key = "SwitchTimeAxis",
                Position = AxisPosition.Bottom,
                Minimum = -0.5,
                Maximum = MaxChartItems - 0.5,
                MajorStep = 1,
                MinorStep = 1,
                TextColor = GetOxyColor("#64748B"),
                TicklineColor = GetOxyColor("#CBD5E1"),
                AxislineColor = GetOxyColor("#64748B"),
                MajorGridlineStyle = LineStyle.None,
                MinorGridlineStyle = LineStyle.None,
                IsZoomEnabled = false,
                IsPanEnabled = false,
                LabelFormatter = value =>
                {
                    int index = (int)Math.Round(value);

                    if (index < 0 || index >= MaxChartItems)
                        return string.Empty;

                    if (!_hasSessionStarted)
                        return "--";

                    DateTime labelTime = _sessionStartTime +
                        TimeSpan.FromTicks(_chartInterval.Ticks * (firstVisibleIntervalIndex + index));

                    return labelTime.ToString("HH:mm");
                }
            };

            LinearAxis yAxis = new LinearAxis
            {
                Key = "SwitchValueAxis",
                Position = AxisPosition.Left,
                Title = LocalizationService.GetString("ChartAxis_Switches"),
                Minimum = 0,
                Maximum = yAxisMaximum,
                MajorStep = 5,
                MinorStep = 1,
                TextColor = GetOxyColor("#64748B"),
                TitleColor = GetOxyColor("#64748B"),
                AxislineColor = GetOxyColor("#64748B"),
                TicklineColor = GetOxyColor("#CBD5E1"),
                MajorGridlineStyle = LineStyle.Dash,
                MajorGridlineColor = GetOxyColor("#E2E8F0"),
                MinorGridlineStyle = LineStyle.None,
                IsZoomEnabled = false,
                IsPanEnabled = false
            };

            RectangleBarSeries normalSeries = new RectangleBarSeries
            {
                Title = LocalizationService.GetString("ChartSeries_NormalSwitches"),
                XAxisKey = "SwitchTimeAxis",
                YAxisKey = "SwitchValueAxis",
                FillColor = GetOxyColor("#3B82F6"),
                StrokeColor = GetOxyColor("#3B82F6"),
                StrokeThickness = 0,
                TrackerFormatString = LocalizationService.GetString("ChartTracker_NormalSwitches")
            };

            RectangleBarSeries disruptiveSeries = new RectangleBarSeries
            {
                Title = LocalizationService.GetString("ChartSeries_DisruptiveSwitches"),
                XAxisKey = "SwitchTimeAxis",
                YAxisKey = "SwitchValueAxis",
                FillColor = GetOxyColor("#E11D48"),
                StrokeColor = GetOxyColor("#E11D48"),
                StrokeThickness = 0,
                TrackerFormatString = LocalizationService.GetString("ChartTracker_DisruptiveSwitches")
            };

            for (int i = 0; i < MaxChartItems; i++)
            {
                int normalCount = normalSwitchCounts[i];
                int disruptiveCount = disruptiveSwitchCounts[i];
                int totalCount = totalSwitchCounts[i];

                if (normalCount > 0)
                {
                    normalSeries.Items.Add(
                        new RectangleBarItem(
                            i - 0.30,
                            0,
                            i + 0.30,
                            normalCount));
                }

                if (disruptiveCount > 0)
                {
                    disruptiveSeries.Items.Add(
                        new RectangleBarItem(
                            i - 0.30,
                            normalCount,
                            i + 0.30,
                            normalCount + disruptiveCount));
                }
            }

            model.Axes.Add(xAxis);
            model.Axes.Add(yAxis);
            model.Series.Add(normalSeries);
            model.Series.Add(disruptiveSeries);

            return model;
        }

        private void AddSwitchBarAnnotations(
            PlotModel model,
            int barIndex,
            int normalCount,
            int disruptiveCount,
            int totalCount,
            int yAxisMaximum)
        {
            if (model == null || totalCount <= 0)
                return;

            double topOffset =
                Math.Max(0.9, yAxisMaximum * 0.04);

            model.Annotations.Add(
                new OxyPlot.Annotations.TextAnnotation
                {
                    Text = totalCount.ToString(),
                    TextPosition = new DataPoint(
                        barIndex,
                        totalCount + topOffset),
                    Stroke = OxyColors.Transparent,
                    TextColor = GetOxyColor("#334155"),
                    FontSize = 12,
                    TextHorizontalAlignment = OxyPlot.HorizontalAlignment.Center,
                    TextVerticalAlignment = OxyPlot.VerticalAlignment.Bottom
                });

            if (normalCount >= 2)
            {
                AddSwitchSegmentLabel(
                    model,
                    barIndex,
                    normalCount / 2.0,
                    normalCount.ToString());
            }

            if (disruptiveCount >= 2)
            {
                AddSwitchSegmentLabel(
                    model,
                    barIndex,
                    normalCount + disruptiveCount / 2.0,
                    disruptiveCount.ToString());
            }
        }

        private void AddSwitchSegmentLabel(
            PlotModel model,
            int barIndex,
            double yPosition,
            string text)
        {
            model.Annotations.Add(
                new OxyPlot.Annotations.TextAnnotation
                {
                    Text = text,
                    TextPosition = new DataPoint(
                        barIndex,
                        yPosition),
                    Stroke = OxyColors.Transparent,
                    TextColor = OxyColors.White,
                    FontSize = 11,
                    TextHorizontalAlignment = OxyPlot.HorizontalAlignment.Center,
                    TextVerticalAlignment = OxyPlot.VerticalAlignment.Middle
                });
        }

        private PlotModel BuildScorePlotModel()
        {
            PlotModel model = CreateBasePlotModel();

            int firstVisibleIntervalIndex = GetFirstVisibleChartIntervalIndex();

            DateTime visibleStart = _hasSessionStarted
                ? _sessionStartTime + TimeSpan.FromTicks(_chartInterval.Ticks * firstVisibleIntervalIndex)
                : DateTime.Now;

            DateTime visibleEnd = visibleStart + TimeSpan.FromTicks(_chartInterval.Ticks * MaxChartItems);

            LinearAxis xAxis = new LinearAxis
            {
                Key = "ScoreTimeAxis",
                Position = AxisPosition.Bottom,
                Minimum = -0.5,
                Maximum = MaxChartItems - 0.5,
                MajorStep = 1,
                MinorStep = 1,
                TextColor = GetOxyColor("#64748B"),
                TicklineColor = GetOxyColor("#CBD5E1"),
                AxislineColor = GetOxyColor("#64748B"),
                MajorGridlineStyle = LineStyle.Dash,
                MajorGridlineColor = GetOxyColor("#E2E8F0"),
                MinorGridlineStyle = LineStyle.None,
                IsZoomEnabled = false,
                IsPanEnabled = false,
                LabelFormatter = value =>
                {
                    int index = (int)Math.Round(value);

                    if (index < 0 || index >= MaxChartItems)
                        return string.Empty;

                    if (!_hasSessionStarted)
                        return "--";

                    DateTime labelTime = _sessionStartTime +
                        TimeSpan.FromTicks(_chartInterval.Ticks * (firstVisibleIntervalIndex + index));

                    return labelTime.ToString("HH:mm");
                }
            };

            LinearAxis yAxis = new LinearAxis
            {
                Key = "ScoreValueAxis",
                Position = AxisPosition.Left,
                Title = LocalizationService.GetString("ChartAxis_ScorePercent"),
                Minimum = 0,
                Maximum = 100,
                MajorStep = 25,
                MinorStep = 5,
                TextColor = GetOxyColor("#64748B"),
                TitleColor = GetOxyColor("#64748B"),
                AxislineColor = GetOxyColor("#64748B"),
                TicklineColor = GetOxyColor("#CBD5E1"),
                MajorGridlineStyle = LineStyle.Dash,
                MajorGridlineColor = GetOxyColor("#E2E8F0"),
                MinorGridlineStyle = LineStyle.None,
                IsZoomEnabled = false,
                IsPanEnabled = false
            };

            LineSeries series = new LineSeries
            {
                Title = LocalizationService.GetString("ChartSeries_ConcentrationScore"),
                XAxisKey = "ScoreTimeAxis",
                YAxisKey = "ScoreValueAxis",
                Color = GetOxyColor("#10B981"),
                StrokeThickness = 3,
                MarkerType = MarkerType.Circle,
                MarkerSize = 4,
                MarkerFill = GetOxyColor("#10B981"),
                MarkerStroke = OxyColors.White,
                MarkerStrokeThickness = 1.5,
                CanTrackerInterpolatePoints = false,
                TrackerFormatString = LocalizationService.GetString("ChartTracker_ConcentrationScore")
            };

            List<ScoreSampleModel> visibleSamples = _scoreSamples
                .Where(x =>
                    x.CapturedAt >= visibleStart &&
                    x.CapturedAt <= visibleEnd)
                .OrderBy(x => x.CapturedAt)
                .ToList();

            foreach (ScoreSampleModel sample in visibleSamples)
            {
                double x = (sample.CapturedAt - visibleStart).TotalSeconds /
                    _chartInterval.TotalSeconds;

                series.Points.Add(
                    new DataPoint(
                        x,
                        sample.Score));
            }

            model.Axes.Add(xAxis);
            model.Axes.Add(yAxis);
            model.Series.Add(series);

            return model;
        }

        private PlotModel CreateBasePlotModel()
        {
            PlotModel model = new PlotModel
            {
                Background = OxyColors.Transparent,
                PlotAreaBackground = OxyColors.Transparent,
                PlotAreaBorderColor = GetOxyColor("#64748B"),
                TextColor = GetOxyColor("#64748B")
            };

            model.Padding = new OxyThickness(10, 6, 10, 0);

            return model;
        }

        private void TryAddScoreSample()
        {
            if (!_hasSessionStarted)
                return;

            if (!IsMonitoring || IsIdle || _isIgnoringOwnApp)
                return;

            TimeSpan trackedElapsed = GetTrackedElapsedTime();

            if (trackedElapsed.TotalSeconds <= 0)
                return;

            DateTime now = GetMetricsNow();

            bool shouldAdd =
                _lastScoreSampleTime == DateTime.MinValue ||
                now - _lastScoreSampleTime >= _scoreSampleInterval;

            if (!shouldAdd)
                return;

            int score = 0;
            int.TryParse(ConcentrationScore, out score);

            if (score < 0)
                score = 0;

            if (score > 100)
                score = 100;

            _scoreSamples.Add(
                new ScoreSampleModel
                {
                    CapturedAt = now,
                    Score = score
                });

            _lastScoreSampleTime = now;

            RemoveOldScoreSamples();
        }

        private void RemoveOldScoreSamples()
        {
            if (!_hasSessionStarted)
                return;

            int firstVisibleIntervalIndex = GetFirstVisibleChartIntervalIndex();

            DateTime visibleStart = _sessionStartTime + TimeSpan.FromTicks(_chartInterval.Ticks * firstVisibleIntervalIndex);
            DateTime visibleEnd = visibleStart + TimeSpan.FromTicks(_chartInterval.Ticks * MaxChartItems);

            _scoreSamples.RemoveAll(x =>
                x.CapturedAt < visibleStart ||
                x.CapturedAt > visibleEnd);
        }

        private int GetFirstVisibleChartIntervalIndex()
        {
            if (!_hasSessionStarted)
                return 0;

            TimeSpan sessionElapsed = GetSessionElapsedTime();
            int currentIntervalIndex = (int)(sessionElapsed.Ticks / _chartInterval.Ticks);
            int firstVisibleIntervalIndex = currentIntervalIndex - MaxChartItems + 1;

            if (firstVisibleIntervalIndex < 0)
                firstVisibleIntervalIndex = 0;

            return firstVisibleIntervalIndex;
        }

        private int RoundUpToStep(int value, int step)
        {
            if (value <= 0)
                return step;

            return (int)(Math.Ceiling(value / (double)step) * step);
        }

        private OxyColor GetOxyColor(string hex)
        {
            return OxyColor.Parse(hex);
        }
    }
}