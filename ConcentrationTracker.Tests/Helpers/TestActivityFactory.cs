using ConcentrationTracker.MVVM.Model;
using System;

namespace ConcentrationTracker.Tests.Helpers
{
    internal static class TestActivityFactory
    {
        public static ActivityEventModel CreateActivity(
            string appName,
            AppCategory category,
            DateTime startTime,
            TimeSpan duration)
        {
            return new ActivityEventModel
            {
                AppName = appName,
                Category = category,
                StartTime = startTime,
                EndTime = startTime.Add(duration),
                IsActive = false
            };
        }

        public static ActivityEventModel CreateActiveActivity(
            string appName,
            AppCategory category,
            DateTime startTime)
        {
            return new ActivityEventModel
            {
                AppName = appName,
                Category = category,
                StartTime = startTime,
                EndTime = null,
                IsActive = true
            };
        }
    }
}
