using ConcentrationTracker.MVVM.Model;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace ConcentrationTracker.Tests
{
    [TestClass]
    public class FocusBlockModelTests
    {
        [TestMethod]
        public void StartSegment_NewFocusBlock_ActivatesCurrentSegment()
        {
            DateTime start = new DateTime(2026, 1, 1, 10, 0, 0);

            FocusBlockModel block = new FocusBlockModel
            {
                MainApp = "Visual Studio",
                Category = AppCategory.Productive,
                StartTime = start
            };

            block.StartSegment(start);

            Assert.IsTrue(block.IsActive);
            Assert.IsFalse(block.IsSuspended);
            Assert.AreEqual(start, block.CurrentSegmentStartTime);
            Assert.AreEqual("Active now", block.StatusText);
        }

        [TestMethod]
        public void PauseSegment_ActiveFocusBlock_AccumulatesDurationAndSuspendsBlock()
        {
            DateTime start = new DateTime(2026, 1, 1, 10, 0, 0);
            DateTime pause = start.AddMinutes(5);

            FocusBlockModel block = new FocusBlockModel
            {
                MainApp = "Visual Studio",
                Category = AppCategory.Productive,
                StartTime = start
            };

            block.StartSegment(start);
            block.PauseSegment(pause);

            Assert.IsFalse(block.IsActive);
            Assert.IsTrue(block.IsSuspended);
            Assert.IsNull(block.CurrentSegmentStartTime);
            Assert.AreEqual(TimeSpan.FromMinutes(5), block.AccumulatedDuration);
            Assert.AreEqual("Paused for dashboard", block.StatusText);
        }

        [TestMethod]
        public void Complete_ActiveFocusBlock_SetsEndTimeReasonAndFinalDuration()
        {
            DateTime start = new DateTime(2026, 1, 1, 10, 0, 0);
            DateTime end = start.AddMinutes(15);

            FocusBlockModel block = new FocusBlockModel
            {
                MainApp = "Visual Studio",
                Category = AppCategory.Productive,
                StartTime = start
            };

            block.StartSegment(start);
            block.Complete(end, FocusBreakReason.Distraction);

            Assert.IsFalse(block.IsActive);
            Assert.IsFalse(block.IsSuspended);
            Assert.IsNull(block.CurrentSegmentStartTime);
            Assert.AreEqual(end, block.EndTime);
            Assert.AreEqual(FocusBreakReason.Distraction, block.BreakReason);
            Assert.AreEqual(TimeSpan.FromMinutes(15), block.AccumulatedDuration);
            Assert.AreEqual("Completed", block.StatusText);
        }
    }
}
