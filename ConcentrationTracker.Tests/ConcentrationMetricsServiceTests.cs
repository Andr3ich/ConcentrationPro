using ConcentrationTracker.Core.Services;
using ConcentrationTracker.MVVM.Model;
using ConcentrationTracker.Tests.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;

namespace ConcentrationTracker.Tests
{
    [TestClass]
    public class ConcentrationMetricsServiceTests
    {
        private readonly ConcentrationMetricsService _service =
            new ConcentrationMetricsService();

        [TestMethod]
        public void CalculateProductiveTimePercent_MixedActivity_ReturnsFortyPercent()
        {
            DateTime now = new DateTime(2026, 1, 1, 10, 30, 0);
            DateTime start = now.AddMinutes(-30);

            List<ActivityEventModel> events = new List<ActivityEventModel>
            {
                TestActivityFactory.CreateActivity(
                    "Visual Studio",
                    AppCategory.Productive,
                    start,
                    TimeSpan.FromMinutes(12)),

                TestActivityFactory.CreateActivity(
                    "Microsoft Word",
                    AppCategory.Neutral,
                    start.AddMinutes(12),
                    TimeSpan.FromMinutes(8)),

                TestActivityFactory.CreateActivity(
                    "YouTube",
                    AppCategory.Distraction,
                    start.AddMinutes(20),
                    TimeSpan.FromMinutes(10))
            };

            int result = _service.CalculateProductiveTimePercent(
                events,
                TimeSpan.FromMinutes(30),
                now);

            Assert.AreEqual(40, result);
        }

        [TestMethod]
        public void CalculateProductiveTimePercent_ZeroTrackedTime_ReturnsZero()
        {
            DateTime now = new DateTime(2026, 1, 1, 10, 0, 0);

            List<ActivityEventModel> events = new List<ActivityEventModel>
            {
                TestActivityFactory.CreateActivity(
                    "Visual Studio",
                    AppCategory.Productive,
                    now.AddMinutes(-10),
                    TimeSpan.FromMinutes(10))
            };

            int result = _service.CalculateProductiveTimePercent(
                events,
                TimeSpan.Zero,
                now);

            Assert.AreEqual(0, result);
        }

        [TestMethod]
        public void CalculateProductiveTimePercent_ActiveEventWithoutEndTime_UsesCurrentTime()
        {
            DateTime now = new DateTime(2026, 1, 1, 10, 20, 0);

            List<ActivityEventModel> events = new List<ActivityEventModel>
            {
                TestActivityFactory.CreateActiveActivity(
                    "Visual Studio",
                    AppCategory.Productive,
                    now.AddMinutes(-10))
            };

            int result = _service.CalculateProductiveTimePercent(
                events,
                TimeSpan.FromMinutes(20),
                now);

            Assert.AreEqual(50, result);
        }

        [TestMethod]
        public void CalculateFocusQualityPercent_LongProductiveFocusBlock_ReturnsMaximumValue()
        {
            DateTime now = new DateTime(2026, 1, 1, 10, 30, 0);
            DateTime start = now.AddMinutes(-30);

            List<ActivityEventModel> events = new List<ActivityEventModel>
            {
                TestActivityFactory.CreateActivity(
                    "Visual Studio",
                    AppCategory.Productive,
                    start,
                    TimeSpan.FromMinutes(30))
            };

            List<FocusBlockModel> focusBlocks = new List<FocusBlockModel>
            {
                new FocusBlockModel
                {
                    MainApp = "Visual Studio",
                    Category = AppCategory.Productive,
                    StartTime = start,
                    EndTime = start.AddMinutes(25),
                    AccumulatedDuration = TimeSpan.FromMinutes(25),
                    BreakReason = FocusBreakReason.None
                }
            };

            int result = _service.CalculateFocusQualityPercent(
                events,
                focusBlocks,
                TimeSpan.FromMinutes(30),
                interruptionCount: 0,
                now: now);

            Assert.AreEqual(100, result);
        }

        [TestMethod]
        public void CalculateFocusStabilityPercent_NoRecentEvents_ReturnsMaximumValue()
        {
            DateTime now = new DateTime(2026, 1, 1, 10, 30, 0);

            List<ActivityEventModel> events = new List<ActivityEventModel>
            {
                TestActivityFactory.CreateActivity(
                    "Visual Studio",
                    AppCategory.Productive,
                    now.AddMinutes(-30),
                    TimeSpan.FromMinutes(10))
            };

            int result = _service.CalculateFocusStabilityPercent(
                activityEvents: events,
                trackedElapsed: TimeSpan.FromMinutes(30),
                now: now);

            Assert.AreEqual(100, result);
        }

        [TestMethod]
        public void CalculateConcentrationScore_MixedSessionWithInterruptions_ReturnsExpectedScore()
        {
            DateTime now = new DateTime(2026, 1, 1, 10, 30, 0);
            DateTime start = now.AddMinutes(-30);

            List<ActivityEventModel> events = new List<ActivityEventModel>
            {
                TestActivityFactory.CreateActivity(
                    "Visual Studio",
                    AppCategory.Productive,
                    start,
                    TimeSpan.FromMinutes(10)),

                TestActivityFactory.CreateActivity(
                    "Telegram",
                    AppCategory.Communication,
                    start.AddMinutes(10),
                    TimeSpan.FromMinutes(10)),

                TestActivityFactory.CreateActivity(
                    "YouTube",
                    AppCategory.Distraction,
                    start.AddMinutes(20),
                    TimeSpan.FromMinutes(10))
            };

            int result = _service.CalculateConcentrationScore(
                activityEvents: events,
                focusBlocks: new List<FocusBlockModel>(),
                trackedElapsed: TimeSpan.FromMinutes(30),
                interruptionCount: 2,
                now: now);

            Assert.AreEqual(33, result);
        }
    }
}
