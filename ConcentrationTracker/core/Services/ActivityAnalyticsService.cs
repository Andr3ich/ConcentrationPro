using ConcentrationTracker.MVVM.Model;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ConcentrationTracker.Core.Services
{
    public class ActivityAnalyticsService
    {
        public TimeSpan GetCategoryDuration(IEnumerable<ActivityEventModel> events, AppCategory category, DateTime now)
        {
            double totalSeconds = events
                .Where(x => x.Category == category)
                .Sum(x => GetDurationSeconds(x, now));

            return TimeSpan.FromSeconds(totalSeconds);
        }

        public string GetCategoryDurationText(IEnumerable<ActivityEventModel> events, AppCategory category, DateTime now)
        {
            TimeSpan duration = GetCategoryDuration(events, category, now);
            return FormatDuration(duration);
        }

        public string GetTopAppByDuration(IEnumerable<ActivityEventModel> events, DateTime now)
        {
            var topApp = events
                .Where(x => !string.IsNullOrWhiteSpace(x.AppName))
                .GroupBy(x => x.AppName)
                .Select(group => new
                {
                    AppName = group.Key,
                    Seconds = group.Sum(x => GetDurationSeconds(x, now))
                })
                .OrderByDescending(x => x.Seconds)
                .FirstOrDefault();

            if (topApp == null || topApp.Seconds <= 0)
                return LocalizationService.GetString("Common_NoData");

            return topApp.AppName + " · " + FormatDuration(TimeSpan.FromSeconds(topApp.Seconds));
        }

        public string GetTopAppByCategoryDuration(IEnumerable<ActivityEventModel> events, AppCategory category, DateTime now)
        {
            var topApp = events
                .Where(x => x.Category == category && !string.IsNullOrWhiteSpace(x.AppName))
                .GroupBy(x => x.AppName)
                .Select(group => new
                {
                    AppName = group.Key,
                    Seconds = group.Sum(x => GetDurationSeconds(x, now))
                })
                .OrderByDescending(x => x.Seconds)
                .FirstOrDefault();

            if (topApp == null || topApp.Seconds <= 0)
                return LocalizationService.GetString("Common_NoData");

            return topApp.AppName + " · " + FormatDuration(TimeSpan.FromSeconds(topApp.Seconds));
        }

        public string GetMainWorkContext(IEnumerable<ActivityEventModel> events, DateTime now)
        {
            var topWorkApp = events
                .Where(x => x.Category == AppCategory.Productive || x.Category == AppCategory.Neutral)
                .Where(x => !string.IsNullOrWhiteSpace(x.AppName))
                .GroupBy(x => x.AppName)
                .Select(group => new
                {
                    AppName = group.Key,
                    Seconds = group.Sum(x => GetDurationSeconds(x, now))
                })
                .OrderByDescending(x => x.Seconds)
                .FirstOrDefault();

            if (topWorkApp == null || topWorkApp.Seconds <= 0)
                return LocalizationService.GetString("Common_NoData");

            return topWorkApp.AppName + " · " + FormatDuration(TimeSpan.FromSeconds(topWorkApp.Seconds));
        }

        private double GetDurationSeconds(ActivityEventModel activityEvent, DateTime now)
        {
            if (activityEvent == null)
                return 0;

            DateTime finishTime = activityEvent.EndTime ?? now;
            double seconds = (finishTime - activityEvent.StartTime).TotalSeconds;

            if (seconds < 0)
                return 0;

            return seconds;
        }

        private string FormatDuration(TimeSpan duration)
        {
            if (duration.TotalSeconds < 1)
                return LocalizationService.GetString("Duration_Zero");

            int hours = (int)duration.TotalHours;
            int minutes = duration.Minutes;
            int seconds = duration.Seconds;

            List<string> parts = new List<string>();

            if (hours > 0)
                parts.Add(hours + LocalizationService.GetString("Common_HoursSuffix"));
            if (minutes > 0)
                parts.Add(minutes + LocalizationService.GetString("Common_MinutesSuffix"));
            if (seconds > 0)
                parts.Add(seconds + LocalizationService.GetString("Common_SecondsSuffix"));

            if (parts.Count == 0)
                return LocalizationService.GetString("Duration_Zero");

            return string.Join(" ", parts);
        }
    }
}
