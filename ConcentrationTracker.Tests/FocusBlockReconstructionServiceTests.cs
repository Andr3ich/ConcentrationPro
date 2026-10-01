using ConcentrationTracker.Core.Services;
using ConcentrationTracker.MVVM.Model;
using ConcentrationTracker.Tests.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;

namespace ConcentrationTracker.Tests
{
    [TestClass]
    public class FocusBlockReconstructionServiceTests
    {
        private readonly FocusBlockReconstructionService _service =
            new FocusBlockReconstructionService();

        [TestMethod]
        public void Reconstruct_ProductiveThenCommunication_CountsInterruption()
        {
            DateTime start = new DateTime(2026, 1, 1, 10, 0, 0);

            List<ActivityEventModel> events = new List<ActivityEventModel>
            {
                TestActivityFactory.CreateActivity("Visual Studio", AppCategory.Productive, start, TimeSpan.FromMinutes(10)),
                TestActivityFactory.CreateActivity("Telegram", AppCategory.Communication, start.AddMinutes(10), TimeSpan.FromMinutes(2)),
                TestActivityFactory.CreateActivity("Visual Studio", AppCategory.Productive, start.AddMinutes(12), TimeSpan.FromMinutes(5))
            };

            FocusBlockReconstructionResult result =
                _service.Reconstruct(events, start.AddMinutes(17));

            Assert.AreEqual(2, result.FocusBlocks.Count);
            Assert.AreEqual(1, result.InterruptionCount);
            Assert.AreEqual(FocusBreakReason.Communication, result.FocusBlocks[0].BreakReason);
            Assert.AreEqual(TimeSpan.FromMinutes(10), result.BestFocusBlockDuration);
        }

        [TestMethod]
        public void Reconstruct_SystemWindowWithinGracePeriod_ContinuesSameBlock()
        {
            DateTime start = new DateTime(2026, 1, 1, 10, 0, 0);

            List<ActivityEventModel> events = new List<ActivityEventModel>
            {
                TestActivityFactory.CreateActivity("Visual Studio", AppCategory.Productive, start, TimeSpan.FromMinutes(10)),
                TestActivityFactory.CreateActivity("Task Switching", AppCategory.System, start.AddMinutes(10), TimeSpan.FromSeconds(30)),
                TestActivityFactory.CreateActivity("Visual Studio", AppCategory.Productive, start.AddMinutes(10.5), TimeSpan.FromMinutes(5))
            };

            FocusBlockReconstructionResult result =
                _service.Reconstruct(events, start.AddMinutes(15.5));

            Assert.AreEqual(1, result.FocusBlocks.Count);
            Assert.AreEqual(0, result.InterruptionCount);
            Assert.AreEqual(TimeSpan.FromMinutes(15), result.BestFocusBlockDuration);
        }

        [TestMethod]
        public void Reconstruct_SystemWindowLongerThanGracePeriod_StartsNewBlock()
        {
            DateTime start = new DateTime(2026, 1, 1, 10, 0, 0);

            List<ActivityEventModel> events = new List<ActivityEventModel>
            {
                TestActivityFactory.CreateActivity("Visual Studio", AppCategory.Productive, start, TimeSpan.FromMinutes(10)),
                TestActivityFactory.CreateActivity("Task Switching", AppCategory.System, start.AddMinutes(10), TimeSpan.FromMinutes(2)),
                TestActivityFactory.CreateActivity("Visual Studio", AppCategory.Productive, start.AddMinutes(12), TimeSpan.FromMinutes(5))
            };

            FocusBlockReconstructionResult result =
                _service.Reconstruct(events, start.AddMinutes(17));

            Assert.AreEqual(2, result.FocusBlocks.Count);
            Assert.AreEqual(0, result.InterruptionCount);
            Assert.AreEqual(FocusBreakReason.ContextChanged, result.FocusBlocks[0].BreakReason);
        }

        [TestMethod]
        public void Reconstruct_ShortBlockBeforeDistraction_DoesNotCountInterruption()
        {
            DateTime start = new DateTime(2026, 1, 1, 10, 0, 0);

            List<ActivityEventModel> events = new List<ActivityEventModel>
            {
                TestActivityFactory.CreateActivity("Visual Studio", AppCategory.Productive, start, TimeSpan.FromSeconds(3)),
                TestActivityFactory.CreateActivity("YouTube", AppCategory.Distraction, start.AddSeconds(3), TimeSpan.FromMinutes(1))
            };

            FocusBlockReconstructionResult result =
                _service.Reconstruct(events, start.AddSeconds(63));

            Assert.AreEqual(1, result.FocusBlocks.Count);
            Assert.AreEqual(0, result.InterruptionCount);
        }
    }
}
