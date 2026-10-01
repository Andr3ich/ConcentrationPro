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
        private SummaryAppMetricModel GetTopAppMetricByDuration(
            string title,
            IEnumerable<AppCategory> allowedCategories)
        {
            IEnumerable<ActivityEventModel> query =
                ActivityEvents.Where(x => x != null);

            if (allowedCategories != null)
            {
                HashSet<AppCategory> categorySet =
                    new HashSet<AppCategory>(allowedCategories);

                query = query.Where(x => categorySet.Contains(x.Category));
            }

            var groupedItems =
                query

                    .GroupBy(x => GetSummaryContextKey(x))
                    .Select(group => new
                    {
                        ContextKey = group.Key,
                        DisplayName = GetSummaryDisplayName(group.FirstOrDefault()),
                        TotalDuration = TimeSpan.FromSeconds(
                            group.Sum(x => GetActivityDurationForAnalytics(x).TotalSeconds)),
                        RepresentativeEvent = group
                            .OrderByDescending(x => GetActivityDurationForAnalytics(x).TotalSeconds)
                            .ThenByDescending(x => x.StartTime)
                            .FirstOrDefault()
                    })
                    .Where(x => x.TotalDuration.TotalSeconds > 0)
                    .OrderByDescending(x => x.TotalDuration)
                    .ThenBy(x => x.DisplayName)
                    .ToList();

            var topItem = groupedItems.FirstOrDefault();

            if (topItem == null || topItem.RepresentativeEvent == null)
                return SummaryAppMetricModel.Empty(title);

            return CreateSummaryAppMetric(
                title,
                topItem.RepresentativeEvent,
                topItem.TotalDuration);
        }

        private SummaryAppMetricModel GetTopInterrupterMetric()
        {
            List<ActivityEventModel> disruptiveEntries =
                GetDisruptiveEntryEvents();

            if (disruptiveEntries.Count > 0)
            {
                var topDisruptiveItem =
                    disruptiveEntries
                        .GroupBy(x => GetSummaryContextKey(x))
                        .Select(group => new
                        {
                            AppName = group.Key,
                            TotalDuration = TimeSpan.FromSeconds(
                                group.Sum(x => GetActivityDurationForAnalytics(x).TotalSeconds)),
                            Count = group.Count(),
                            RepresentativeEvent = group
                                .OrderByDescending(x => GetActivityDurationForAnalytics(x).TotalSeconds)
                                .ThenByDescending(x => x.StartTime)
                                .FirstOrDefault()
                        })
                        .OrderByDescending(x => x.Count)
                        .ThenByDescending(x => x.TotalDuration)
                        .ThenBy(x => x.AppName)
                        .FirstOrDefault();

                if (topDisruptiveItem != null && topDisruptiveItem.RepresentativeEvent != null)
                {
                    return CreateSummaryAppMetric(
                        "Top Interrupter",
                        topDisruptiveItem.RepresentativeEvent,
                        topDisruptiveItem.TotalDuration);
                }
            }

            return GetTopAppMetricByDuration(
                "Top Interrupter",
                new[]
                {
                    AppCategory.Distraction,
                    AppCategory.Communication
                });
        }

        private SummaryAppMetricModel CreateSummaryAppMetric(
            string title,
            ActivityEventModel sourceEvent,
            TimeSpan totalDuration)
        {
            if (sourceEvent == null)
                return SummaryAppMetricModel.Empty(title);

            return new SummaryAppMetricModel
            {
                Title = title,
                AppName = GetSummaryDisplayName(sourceEvent),
                WindowTitle = sourceEvent.WindowTitle ?? string.Empty,
                ProcessPath = sourceEvent.ProcessPath ?? string.Empty,
                AppUserModelId = sourceEvent.AppUserModelId ?? string.Empty,
                PackageFullName = sourceEvent.PackageFullName ?? string.Empty,
                PackageInstallPath = sourceEvent.PackageInstallPath ?? string.Empty,
                Category = sourceEvent.Category,
                DurationText = FormatDurationShort(totalDuration),
                HasValue = true
            };
        }

        private double GetActivityStackSegmentWidth(AppCategory category)
        {
            TimeSpan displayedTotal =
                GetDisplayedSummaryDuration();

            if (displayedTotal.TotalSeconds <= 0)
                return 0;

            TimeSpan categoryDuration =
                _activityAnalyticsService.GetCategoryDuration(
                    ActivityEvents,
                    category,
                    GetMetricsNow());

            double ratio =
                categoryDuration.TotalSeconds / displayedTotal.TotalSeconds;

            return ClampRatio(ratio) * SummaryStackBarMaxWidth;
        }

        private double GetAfkStackSegmentWidth()
        {
            TimeSpan displayedTotal =
                GetDisplayedSummaryDuration();

            if (displayedTotal.TotalSeconds <= 0)
                return 0;

            double ratio =
                GetAfkDuration().TotalSeconds / displayedTotal.TotalSeconds;

            return ClampRatio(ratio) * SummaryStackBarMaxWidth;
        }

        private TimeSpan GetDisplayedSummaryDuration()
        {
            TimeSpan trackedElapsed =
                GetTrackedElapsedTime();

            TimeSpan afkDuration =
                GetAfkDuration();

            TimeSpan total =
                trackedElapsed + afkDuration;

            if (total.TotalSeconds <= 0)
                return TimeSpan.Zero;

            return total;
        }

        private double ClampRatio(double value)
        {
            if (double.IsNaN(value) || double.IsInfinity(value))
                return 0;

            if (value < 0)
                return 0;

            if (value > 1)
                return 1;

            return value;
        }

        private int GetDisruptiveSwitchCount()
        {
            List<ActivityEventModel> disruptiveEntries =
                GetDisruptiveEntryEvents();

            return disruptiveEntries.Count;
        }

        private string GetTopInterrupterName()
        {
            List<ActivityEventModel> disruptiveEntries =
                GetDisruptiveEntryEvents();

            if (disruptiveEntries.Count > 0)
            {
                var topItem =
                    disruptiveEntries
                        .GroupBy(x => GetSummaryContextKey(x))
                        .Select(group => new
                        {
                            DisplayName = GetSummaryDisplayName(group.FirstOrDefault()),
                            Count = group.Count(),
                            TotalSeconds = group.Sum(e => GetActivityDurationForAnalytics(e).TotalSeconds)
                        })
                        .OrderByDescending(x => x.Count)
                        .ThenByDescending(x => x.TotalSeconds)
                        .ThenBy(x => x.DisplayName)
                        .FirstOrDefault();

                if (topItem != null)
                    return topItem.DisplayName;
            }

            return GetTopInterruptiveAppByDuration();
        }

        private List<ActivityEventModel> GetDisruptiveEntryEvents()
        {
            List<ActivityEventModel> orderedEvents =
                GetOrderedActivityEventsForAnalytics();

            List<ActivityEventModel> result =
                new List<ActivityEventModel>();

            for (int i = 0; i < orderedEvents.Count; i++)
            {
                ActivityEventModel previousEvent =
                    i > 0
                        ? orderedEvents[i - 1]
                        : null;

                ActivityEventModel currentEvent =
                    orderedEvents[i];

                if (IsDisruptiveEntry(previousEvent, currentEvent))
                {
                    result.Add(currentEvent);
                }
            }

            return result;
        }

        private bool IsDisruptiveEntry(
            ActivityEventModel previousEvent,
            ActivityEventModel currentEvent)
        {
            if (currentEvent == null)
                return false;

            if (!IsInterruptiveCategory(currentEvent.Category))
                return false;

            TimeSpan currentDuration =
                GetActivityDurationForAnalytics(currentEvent);

            if (currentDuration.TotalSeconds < 3)
                return false;

            if (previousEvent == null)
                return true;

            if (IsSameActivityContext(previousEvent, currentEvent) &&
                IsInterruptiveCategory(previousEvent.Category))
            {
                return false;
            }

            return true;
        }

        private bool IsDisruptiveTransition(
            ActivityEventModel previousEvent,
            ActivityEventModel currentEvent)
        {
            return IsDisruptiveEntry(
                previousEvent,
                currentEvent);
        }

        private bool IsFocusFriendlyCategory(
            AppCategory category)
        {
            return category == AppCategory.Productive ||
                   category == AppCategory.Neutral;
        }

        private bool IsInterruptiveCategory(
            AppCategory category)
        {
            return category == AppCategory.Communication ||
                   category == AppCategory.Distraction;
        }

        private bool IsSameActivityContext(
            ActivityEventModel firstEvent,
            ActivityEventModel secondEvent)
        {
            if (firstEvent == null || secondEvent == null)
                return false;

            string firstApp =
                firstEvent.AppName ?? string.Empty;

            string secondApp =
                secondEvent.AppName ?? string.Empty;

            string firstTitle =
                firstEvent.WindowTitle ?? string.Empty;

            string secondTitle =
                secondEvent.WindowTitle ?? string.Empty;

            return firstApp.Equals(
                       secondApp,
                       StringComparison.OrdinalIgnoreCase) &&
                   firstTitle.Equals(
                       secondTitle,
                       StringComparison.OrdinalIgnoreCase);
        }

        private List<ActivityEventModel> GetOrderedActivityEventsForAnalytics()
        {
            return ActivityEvents
                .Where(x => x != null)
                .OrderBy(x => x.StartTime)
                .ToList();
        }

        private string GetSummaryContextKey(
            ActivityEventModel activityEvent)
        {
            if (activityEvent == null)
                return "unknown";

            string displayName =
                GetSummaryDisplayName(activityEvent);

            return activityEvent.Category.ToString().ToLowerInvariant() +
                   "|" +
                   displayName.ToLowerInvariant();
        }

        private string GetSummaryDisplayName(
            ActivityEventModel activityEvent)
        {
            if (activityEvent == null)
                return "Unknown";

            string appName =
                GetSafeAppName(activityEvent);

            if (IsBrowserAppName(appName))
            {
                string browserContext =
                    ExtractBrowserContextName(activityEvent.WindowTitle, appName);

                if (!string.IsNullOrWhiteSpace(browserContext))
                    return browserContext;
            }

            return appName;
        }

        private bool IsBrowserAppName(
            string appName)
        {
            if (string.IsNullOrWhiteSpace(appName))
                return false;

            string normalized =
                appName.ToLowerInvariant();

            return normalized.Contains("chrome") ||
                   normalized.Contains("msedge") ||
                   normalized.Contains("edge") ||
                   normalized.Contains("firefox") ||
                   normalized.Contains("opera") ||
                   normalized.Contains("brave") ||
                   normalized.Contains("browser");
        }

        private string ExtractBrowserContextName(
            string windowTitle,
            string appName)
        {
            if (string.IsNullOrWhiteSpace(windowTitle))
                return appName;

            string title =
                windowTitle.Trim();

            string lowered =
                title.ToLowerInvariant();

            if (lowered.Contains("youtube"))
                return "YouTube";

            if (lowered.Contains("github"))
                return "GitHub";

            if (lowered.Contains("stackoverflow") || lowered.Contains("stack overflow"))
                return "Stack Overflow";

            if (lowered.Contains("google docs"))
                return "Google Docs";

            if (lowered.Contains("gmail"))
                return "Gmail";

            if (lowered.Contains("figma"))
                return "Figma";

            string[] browserSuffixes =
            {
                " - Google Chrome",
                " - Microsoft Edge",
                " - Mozilla Firefox",
                " - Opera",
                " - Brave",
                " — Google Chrome",
                " — Microsoft Edge",
                " — Mozilla Firefox",
                " — Opera",
                " — Brave"
            };

            foreach (string suffix in browserSuffixes)
            {
                if (title.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
                {
                    title = title.Substring(0, title.Length - suffix.Length).Trim();
                    break;
                }
            }

            string[] separators =
            {
                " - ",
                " — ",
                " | "
            };

            foreach (string separator in separators)
            {
                string[] parts =
                    title.Split(new[] { separator }, StringSplitOptions.None);

                if (parts.Length >= 2)
                {
                    string lastPart =
                        parts[parts.Length - 1].Trim();

                    if (!string.IsNullOrWhiteSpace(lastPart) && lastPart.Length <= 32)
                        return lastPart;
                }
            }

            if (title.Length > 32)
                title = title.Substring(0, 32).Trim() + "...";

            return string.IsNullOrWhiteSpace(title)
                ? appName
                : title;
        }

        private string GetSafeAppName(
            ActivityEventModel activityEvent)
        {
            if (activityEvent == null ||
                string.IsNullOrWhiteSpace(activityEvent.AppName))
            {
                return "Unknown";
            }

            return activityEvent.AppName;
        }

        private string GetTopInterruptiveAppByDuration()
        {
            List<ActivityEventModel> interruptiveEvents =
                ActivityEvents
                    .Where(x => x != null && IsInterruptiveCategory(x.Category))
                    .ToList();

            if (interruptiveEvents.Count == 0)
                return LocalizationService.GetString("Common_None");

            var topItem =
                interruptiveEvents
                    .GroupBy(x => GetSummaryContextKey(x))
                    .Select(group => new
                    {
                        DisplayName = GetSummaryDisplayName(group.FirstOrDefault()),
                        TotalSeconds = group.Sum(x => GetActivityDurationForAnalytics(x).TotalSeconds)
                    })
                    .OrderByDescending(x => x.TotalSeconds)
                    .ThenBy(x => x.DisplayName)
                    .FirstOrDefault();

            if (topItem == null || topItem.TotalSeconds <= 0)
                return LocalizationService.GetString("Common_None");

            return topItem.DisplayName;
        }

        private TimeSpan GetActivityDurationForAnalytics(
            ActivityEventModel activityEvent)
        {
            if (activityEvent == null)
                return TimeSpan.Zero;

            DateTime endTime =
                activityEvent.EndTime ?? GetMetricsNow();

            TimeSpan duration =
                endTime - activityEvent.StartTime;

            if (duration.TotalSeconds < 0)
                return TimeSpan.Zero;

            return duration;
        }

        private TimeSpan GetCategoryDurationForAnalytics(
            AppCategory category)
        {
            TimeSpan total =
                TimeSpan.Zero;

            foreach (ActivityEventModel activityEvent in ActivityEvents)
            {
                if (activityEvent == null || activityEvent.Category != category)
                    continue;

                total += GetActivityDurationForAnalytics(activityEvent);
            }

            return total;
        }

        private TimeSpan GetInterruptiveDurationForAnalytics()
        {
            return GetCategoryDurationForAnalytics(AppCategory.Distraction) +
                   GetCategoryDurationForAnalytics(AppCategory.Communication);
        }

        private TimeSpan GetFocusFriendlyDurationForAnalytics()
        {
            return GetCategoryDurationForAnalytics(AppCategory.Productive) +
                   GetCategoryDurationForAnalytics(AppCategory.Neutral);
        }

        private string BuildSessionInsight()
        {
            TimeSpan trackedElapsed =
                GetTrackedElapsedTime();

            TimeSpan afkDuration =
                GetAfkDuration();

            if (!_hasSessionStarted)
            {
                return LocalizationService.GetString("Vm_StartMonitoringInsight");
            }

            if (trackedElapsed.TotalSeconds <= 0 && afkDuration.TotalSeconds <= 0)
            {
                return LocalizationService.GetString("Vm_StartWorkingInsight");
            }

            int score =
                0;

            int.TryParse(
                ConcentrationScore,
                out score);

            int disruptiveSwitches =
                GetDisruptiveSwitchCount();

            string topInterrupter =
                GetTopInterrupterName();

            int focusQuality =
                GetFocusQualityValue();

            int focusStability =
                GetFocusStabilityValue();

            TimeSpan interruptiveDuration =
                GetInterruptiveDurationForAnalytics();

            TimeSpan focusFriendlyDuration =
                GetFocusFriendlyDurationForAnalytics();

            if (trackedElapsed.TotalMinutes < 1 && afkDuration.TotalSeconds < 30)
            {
                return LocalizationService.GetString("Vm_NotEnoughActivityInsight");
            }

            if (afkDuration.TotalMinutes >= 2 &&
                afkDuration.TotalSeconds >= trackedElapsed.TotalSeconds * 0.35)
            {
                return LocalizationService.GetString("Vm_AwayHigh");
            }

            string noneLabel =
                LocalizationService.GetString("Common_None");

            if (interruptiveDuration.TotalSeconds >= 30 &&
                interruptiveDuration.TotalSeconds > focusFriendlyDuration.TotalSeconds)
            {
                if (topInterrupter != noneLabel)
                {
                    return LocalizationService.Format(
                        "Vm_DistractionInsightWithApp",
                        topInterrupter);
                }

                return LocalizationService.GetString("Vm_DistractionInsightNoApp");
            }

            if (disruptiveSwitches >= 3 && topInterrupter != noneLabel)
            {
                return LocalizationService.Format(
                    "Vm_MainInterrupter",
                    topInterrupter);
            }

            if (score >= 85 &&
                disruptiveSwitches <= 1 &&
                focusStability >= 75)
            {
                return LocalizationService.GetString("Vm_StrongFocusSession");
            }

            if (focusQuality >= 75 &&
                focusStability < 50)
            {
                return LocalizationService.GetString("Vm_UnstableFocus");
            }

            if (focusQuality < 45 &&
                disruptiveSwitches >= 2)
            {
                return LocalizationService.GetString("Vm_SwitchDrop");
            }

            if (focusStability >= 75 &&
                focusQuality < 60)
            {
                return LocalizationService.GetString("Vm_StableButLowProductive");
            }

            if (disruptiveSwitches == 0 &&
                interruptiveDuration.TotalSeconds <= 10 &&
                trackedElapsed.TotalMinutes >= 3)
            {
                return LocalizationService.GetString("Vm_CleanSession");
            }

            return LocalizationService.GetString("Vm_BalancedSession");
        }

        private int GetFocusQualityValue()
        {
            TimeSpan trackedElapsed =
                GetTrackedElapsedTime();

            if (!_hasSessionStarted || trackedElapsed.TotalSeconds <= 0)
                return 0;

            return _metricsService.CalculateFocusQualityPercent(
                ActivityEvents,
                FocusBlocks,
                trackedElapsed,
                InterruptionCount,
                GetMetricsNow());
        }

        private int GetFocusStabilityValue()
        {
            TimeSpan trackedElapsed =
                GetTrackedElapsedTime();

            if (!_hasSessionStarted || trackedElapsed.TotalSeconds <= 0)
                return 0;

            return _metricsService.CalculateFocusStabilityPercent(
                ActivityEvents,
                trackedElapsed,
                GetMetricsNow());
        }

        private double GetCategoryBarWidth(AppCategory category)
        {
            TimeSpan trackedElapsed =
                GetTrackedElapsedTime();

            if (!_hasSessionStarted || trackedElapsed.TotalSeconds <= 0)
                return 0;

            TimeSpan categoryDuration =
                _activityAnalyticsService.GetCategoryDuration(
                    ActivityEvents,
                    category,
                    GetMetricsNow());

            double percent =
                categoryDuration.TotalSeconds / trackedElapsed.TotalSeconds;

            if (percent < 0)
                percent = 0;

            if (percent > 1)
                percent = 1;

            return percent * SummaryBarMaxWidth;
        }
    }
}