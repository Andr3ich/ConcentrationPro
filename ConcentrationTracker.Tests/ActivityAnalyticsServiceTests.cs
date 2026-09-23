using ConcentrationTracker.Core.Services;
using ConcentrationTracker.MVVM.Model;
using ConcentrationTracker.Tests.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;

namespace ConcentrationTracker.Tests
{
    [TestClass]
    public class ActivityAnalyticsServiceTests
    {
        private readonly ActivityAnalyticsService _service =
            new ActivityAnalyticsService();

        [TestMethod]
        public void GetCategoryDuration_ProductiveEvents_ReturnsTotalProductiveDuration()
        {
            DateTime now = new DateTime(2026, 1, 1, 12, 0, 0);
            DateTime start = now.AddMinutes(-40);

            List<ActivityEventModel> events = new List<ActivityEventModel>
            {
                TestActivityFactory.CreateActivity(
                    "Visual Studio",
                    AppCategory.Productive,
                    start,
                    TimeSpan.FromMinutes(10)),

                TestActivityFactory.CreateActivity(
                    "Microsoft Word",
                    AppCategory.Productive,
                    start.AddMinutes(10),
                    TimeSpan.FromMinutes(15)),

                TestActivityFactory.CreateActivity(
                    "YouTube",
                    AppCategory.Distraction,
                    start.AddMinutes(25),
                    TimeSpan.FromMinutes(15))
            };

            TimeSpan result = _service.GetCategoryDuration(
                events,
                AppCategory.Productive,
                now);

            Assert.AreEqual(TimeSpan.FromMinutes(25), result);
        }

        [TestMethod]
        public void GetTopAppByDuration_MultipleApps_ReturnsAppWithLargestDuration()
        {
            DateTime now = new DateTime(2026, 1, 1, 12, 0, 0);
            DateTime start = now.AddMinutes(-40);

            List<ActivityEventModel> events = new List<ActivityEventModel>
            {
                TestActivityFactory.CreateActivity(
                    "Visual Studio",
                    AppCategory.Productive,
                    start,
                    TimeSpan.FromMinutes(15)),

                TestActivityFactory.CreateActivity(
                    "Google Chrome",
                    AppCategory.Neutral,
                    start.AddMinutes(15),
                    TimeSpan.FromMinutes(20)),

                TestActivityFactory.CreateActivity(
                    "Telegram",
                    AppCategory.Communication,
                    start.AddMinutes(35),
                    TimeSpan.FromMinutes(5))
            };

            string result = _service.GetTopAppByDuration(events, now);

            Assert.AreEqual("Google Chrome · 20 min", result);
        }

        [TestMethod]
        public void GetTopAppByCategoryDuration_ProductiveCategory_ReturnsOnlyProductiveTopApp()
        {
            DateTime now = new DateTime(2026, 1, 1, 12, 0, 0);
            DateTime start = now.AddMinutes(-45);

            List<ActivityEventModel> events = new List<ActivityEventModel>
            {
                TestActivityFactory.CreateActivity(
                    "Visual Studio",
                    AppCategory.Productive,
                    start,
                    TimeSpan.FromMinutes(15)),

                TestActivityFactory.CreateActivity(
                    "Microsoft Word",
                    AppCategory.Productive,
                    start.AddMinutes(15),
                    TimeSpan.FromMinutes(20)),

                TestActivityFactory.CreateActivity(
                    "YouTube",
                    AppCategory.Distraction,
                    start.AddMinutes(35),
                    TimeSpan.FromMinutes(10))
            };

            string result = _service.GetTopAppByCategoryDuration(
                events,
                AppCategory.Productive,
                now);

            Assert.AreEqual("Microsoft Word · 20 min", result);
        }

        [TestMethod]
        public void GetTopAppByDuration_EmptyEvents_ReturnsNoData()
        {
            DateTime now = new DateTime(2026, 1, 1, 12, 0, 0);
            List<ActivityEventModel> events = new List<ActivityEventModel>();

            string result = _service.GetTopAppByDuration(events, now);

            Assert.AreEqual("No data", result);
        }
    }
}
