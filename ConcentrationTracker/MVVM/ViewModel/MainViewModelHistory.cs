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
        private void LoadSessionHistory()
        {
            SessionHistory.Clear();

            foreach (SessionRecordModel session in _sessionHistoryStorageService.LoadSessions())
            {
                SessionHistory.Add(session);
            }

            SessionHistoryStatus = LocalizationService.Format("History_LoadedStatus", SessionHistory.Count);
            RefreshSessionHistoryProperties();
            UpdateHistoryCharts();
        }

        private void SaveCurrentSessionSnapshot(DateTime endTime, string status)
        {
            if (!_hasSessionStarted)
            {
                SessionHistoryStatus = LocalizationService.GetString("History_StartBeforeSave");
                return;
            }

            TimeSpan sessionElapsed = GetSessionElapsedTime();
            TimeSpan trackedElapsed = GetTrackedElapsedTime();
            TimeSpan afkDuration = GetAfkDuration();

            if (sessionElapsed.TotalSeconds < 5 &&
                trackedElapsed.TotalSeconds < 3 &&
                TotalSwitches == 0)
            {
                SessionHistoryStatus = LocalizationService.GetString("History_TooShort");
                return;
            }

            SessionRecordModel record = BuildCurrentSessionRecord(endTime, status);

            _sessionHistoryStorageService.SaveOrUpdate(record);

            _sessionDetailStorageService.SaveDetails(
                record.SessionId,
                ActivityEvents.ToList(),
                GetMetricsNow());

            LoadSessionHistory();

            SessionHistoryStatus =
                LocalizationService.Format(
                    "History_SavedSessionStatus",
                    record.StartedAtText,
                    record.ConcentrationScore);
        }

        private SessionRecordModel BuildCurrentSessionRecord(DateTime endTime, string status)
        {
            TimeSpan trackedElapsed = GetTrackedElapsedTime();
            DateTime metricsNow = GetMetricsNow();

            int score = ParseIntSafe(ConcentrationScore);
            int focusQuality = GetFocusQualityValue();
            int focusStability = GetFocusStabilityValue();
            int disruptiveSwitches = GetDisruptiveSwitchCount();

            return new SessionRecordModel
            {
                SessionId = _currentSessionId,
                StartedAt = _sessionStartTime,
                EndedAt = endTime,
                Status = status ?? "Saved",

                ConcentrationScore = score,
                FocusQuality = focusQuality,
                FocusStability = focusStability,

                TotalSwitches = TotalSwitches,
                DisruptiveSwitches = disruptiveSwitches,
                Interruptions = InterruptionCount,
                IdleBreaks = _idleBreakCount,

                SessionSeconds = GetSessionElapsedTime().TotalSeconds,
                TrackedSeconds = trackedElapsed.TotalSeconds,
                AfkSeconds = GetAfkDuration().TotalSeconds,
                BestFocusBlockSeconds = GetBestFocusBlockDurationForHistory().TotalSeconds,

                ProductiveSeconds = _activityAnalyticsService.GetCategoryDuration(ActivityEvents, AppCategory.Productive, metricsNow).TotalSeconds,
                NeutralSeconds = _activityAnalyticsService.GetCategoryDuration(ActivityEvents, AppCategory.Neutral, metricsNow).TotalSeconds,
                CommunicationSeconds = _activityAnalyticsService.GetCategoryDuration(ActivityEvents, AppCategory.Communication, metricsNow).TotalSeconds,
                DistractionSeconds = _activityAnalyticsService.GetCategoryDuration(ActivityEvents, AppCategory.Distraction, metricsNow).TotalSeconds,

                MostUsedApp = SafeHistoryText(MostUsedApp),
                MainWorkContext = SafeHistoryText(MainWorkContext),
                TopDistraction = SafeHistoryText(TopDistraction),
                TopInterrupter = SafeHistoryText(TopInterrupter),
                Insight = SafeHistoryText(SessionInsight)
            };
        }

        private TimeSpan GetBestFocusBlockDurationForHistory()
        {
            TimeSpan best = TimeSpan.Zero;

            foreach (FocusBlockModel block in FocusBlocks)
            {
                TimeSpan duration = GetFocusBlockDurationForDisplay(block);

                if (duration > best)
                    best = duration;
            }

            return best;
        }

        private void OpenSessionDetails(object parameter)
        {
            SessionRecordModel session =
                parameter as SessionRecordModel ?? SelectedSessionRecord;

            if (session == null)
            {
                SessionHistoryStatus = LocalizationService.GetString("History_NeedSessionOpen");
                return;
            }

            try
            {
                SessionDetailsWindow window =
                    new SessionDetailsWindow(session);

                window.Owner =
                    Application.Current.MainWindow;

                window.Show();

                SessionHistoryStatus =
                    "Opened details for session: " +
                    session.StartedAtText +
                    ".";
            }
            catch (Exception ex)
            {
                SessionHistoryStatus =
                    "Could not open session details: " +
                    ex.Message;
            }
        }

        private void DeleteSessionRecord(object parameter)
        {
            SessionRecordModel session = parameter as SessionRecordModel ?? SelectedSessionRecord;

            if (session == null)
            {
                SessionHistoryStatus = LocalizationService.GetString("History_NeedSessionDelete");
                return;
            }

            _sessionHistoryStorageService.DeleteSession(session.SessionId);
            SelectedSessionRecord = null;
            LoadSessionHistory();
            SessionHistoryStatus = LocalizationService.GetString("History_DeletedStatus");
        }

        private void ClearSessionHistory()
        {
            _sessionHistoryStorageService.ClearSessions();
            SelectedSessionRecord = null;
            LoadSessionHistory();
            SessionHistoryStatus = LocalizationService.GetString("History_ClearedStatus");
        }

        private void UpdateHistoryCharts()
        {
            HistoryScoreTrendPlotModel =
                BuildHistoryScoreTrendPlotModel();
        }

        private PlotModel BuildHistoryScoreTrendPlotModel()
        {
            PlotModel model =
                CreateBasePlotModel();

            model.Padding =
                new OxyThickness(10, 8, 10, 0);

            List<SessionRecordModel> allSessions =
                SessionHistory == null
                    ? new List<SessionRecordModel>()
                    : SessionHistory
                        .OrderBy(x => x.StartedAt)
                        .ToList();

            int maxVisibleItems =
                5;

            List<SessionRecordModel> visibleSessions =
                allSessions
                    .Skip(Math.Max(0, allSessions.Count - maxVisibleItems))
                    .ToList();

            int itemCount =
                Math.Max(visibleSessions.Count, 1);

            LinearAxis xAxis =
                new LinearAxis
                {
                    Key = "HistorySessionAxis",
                    Position = AxisPosition.Bottom,
                    Minimum = -0.5,
                    Maximum = itemCount - 0.5,
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
                        int index =
                            (int)Math.Round(value);

                        if (index < 0 ||
                            index >= visibleSessions.Count)
                        {
                            return string.Empty;
                        }

                        return visibleSessions[index].StartedAt.ToString("dd.MM\nHH:mm");
                    }
                };

            LinearAxis yAxis =
                new LinearAxis
                {
                    Key = "HistoryScoreAxis",
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

            LineSeries scoreSeries =
                new LineSeries
                {
                    Title = LocalizationService.GetString("ChartSeries_Score"),
                    XAxisKey = "HistorySessionAxis",
                    YAxisKey = "HistoryScoreAxis",
                    Color = GetOxyColor("#2563EB"),
                    StrokeThickness = 3,
                    MarkerType = MarkerType.Circle,
                    MarkerSize = 4.5,
                    MarkerFill = GetOxyColor("#2563EB"),
                    MarkerStroke = OxyColors.White,
                    MarkerStrokeThickness = 1.5,
                    CanTrackerInterpolatePoints = false,
                    TrackerFormatString = LocalizationService.GetString("ChartTracker_SessionScore")
                };

            for (int i = 0; i < visibleSessions.Count; i++)
            {
                scoreSeries.Points.Add(
                    new DataPoint(
                        i,
                        visibleSessions[i].ConcentrationScore));
            }

            model.Axes.Add(
                xAxis);

            model.Axes.Add(
                yAxis);

            model.Series.Add(
                scoreSeries);

            return model;
        }

        private string FormatHistoryDuration(double seconds)
        {
            if (seconds <= 0)
                return LocalizationService.GetString("Duration_Zero");

            return FormatDurationShort(
                TimeSpan.FromSeconds(seconds));
        }

        private void RefreshSessionHistoryProperties()
        {
            OnPropertyChanged(nameof(SessionHistory));
            OnPropertyChanged(nameof(SelectedSessionRecord));
            OnPropertyChanged(nameof(SavedSessionsCount));
            OnPropertyChanged(nameof(AverageSavedScore));
            OnPropertyChanged(nameof(LastSavedSessionText));
            OnPropertyChanged(nameof(BestSavedScore));
            OnPropertyChanged(nameof(BestSavedSessionText));
            OnPropertyChanged(nameof(TotalSavedTrackedTime));
            OnPropertyChanged(nameof(TotalSavedProductiveTime));
            OnPropertyChanged(nameof(TotalSavedDistractionTime));
            OnPropertyChanged(nameof(TotalSavedAfkTime));
            OnPropertyChanged(nameof(MostCommonDistraction));
            OnPropertyChanged(nameof(AverageDisruptiveSwitches));
            OnPropertyChanged(nameof(HistoryProductiveRatio));
            OnPropertyChanged(nameof(HistoryDistractionRatio));
            OnPropertyChanged(nameof(HistoryScoreTrendPlotModel));
            OnPropertyChanged(nameof(SessionHistoryStatus));
        }

        private string SafeHistoryText(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return LocalizationService.GetString("Common_None");

            return value.Trim();
        }
    }
}