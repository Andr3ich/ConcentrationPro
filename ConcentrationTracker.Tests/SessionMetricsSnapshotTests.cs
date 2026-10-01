using ConcentrationTracker.Core.Services;
using ConcentrationTracker.MVVM.Model;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ConcentrationTracker.Tests
{
    [TestClass]
    public class SessionMetricsSnapshotTests
    {
        [TestMethod]
        public void SerializeAndApply_SavedSession_RestoresAllMetrics()
        {
            SessionRecordModel saved = new SessionRecordModel
            {
                ConcentrationScore = 58,
                FocusQuality = 72,
                FocusStability = 0,
                TotalSwitches = 48,
                DisruptiveSwitches = 3,
                Interruptions = 24,
                IdleBreaks = 1,
                SessionSeconds = 3720.5,
                TrackedSeconds = 3600,
                AfkSeconds = 120.25,
                BestFocusBlockSeconds = 139.2,
                ProductiveSeconds = 3480,
                NeutralSeconds = 0,
                CommunicationSeconds = 120,
                DistractionSeconds = 0
            };

            SessionRecordModel restored = new SessionRecordModel();

            bool applied = SessionMetricsSnapshot.TryApply(
                SessionMetricsSnapshot.Serialize(saved),
                restored);

            Assert.IsTrue(applied);
            Assert.AreEqual(58, restored.ConcentrationScore);
            Assert.AreEqual(72, restored.FocusQuality);
            Assert.AreEqual(0, restored.FocusStability);
            Assert.AreEqual(24, restored.Interruptions);
            Assert.AreEqual(48, restored.TotalSwitches);
            Assert.AreEqual(3600, restored.TrackedSeconds, 0.001);
            Assert.AreEqual(120.25, restored.AfkSeconds, 0.001);
            Assert.AreEqual(139.2, restored.BestFocusBlockSeconds, 0.001);
        }

        [TestMethod]
        public void TryApply_EmptyValue_ReturnsFalse()
        {
            SessionRecordModel session = new SessionRecordModel();

            Assert.IsFalse(SessionMetricsSnapshot.TryApply(string.Empty, session));
        }

        [TestMethod]
        public void TryApply_UnknownFormatVersion_ReturnsFalse()
        {
            SessionRecordModel session = new SessionRecordModel();

            Assert.IsFalse(SessionMetricsSnapshot.TryApply("v=2;score=90;quality=90;stability=90", session));
        }
    }
}
