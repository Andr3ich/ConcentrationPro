using ConcentrationTracker.MVVM.Model;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ConcentrationTracker.Core.Services
{
    public class FocusBlockReconstructionResult
    {
        public List<FocusBlockModel> FocusBlocks { get; } = new List<FocusBlockModel>();
        public int InterruptionCount { get; set; }
        public TimeSpan BestFocusBlockDuration { get; set; }
    }

    public class FocusBlockReconstructionService
    {
        private static readonly TimeSpan GracePeriod = TimeSpan.FromSeconds(60);
        private static readonly TimeSpan MinimumInterruptedBlock = TimeSpan.FromSeconds(5);

        private FocusBlockReconstructionResult _result;
        private FocusBlockModel _current;
        private DateTime? _suspendedAt;

        public FocusBlockReconstructionResult Reconstruct(
            IEnumerable<ActivityEventModel> activityEvents,
            DateTime sessionEnd)
        {
            _result = new FocusBlockReconstructionResult();
            _current = null;
            _suspendedAt = null;

            List<ActivityEventModel> events =
                (activityEvents ?? Enumerable.Empty<ActivityEventModel>())
                    .Where(x => x != null)
                    .OrderBy(x => x.StartTime)
                    .ToList();

            DateTime? previousEnd = null;

            foreach (ActivityEventModel activityEvent in events)
            {
                DateTime start = activityEvent.StartTime;
                DateTime end = activityEvent.EndTime ?? sessionEnd;

                if (end < start)
                    end = start;

                if (previousEnd.HasValue && start > previousEnd.Value)
                    HandleGap(previousEnd.Value, start);

                switch (activityEvent.Category)
                {
                    case AppCategory.Productive:
                    case AppCategory.Neutral:
                        StartOrResume(activityEvent, start);
                        break;

                    case AppCategory.Communication:
                    case AppCategory.Distraction:
                        Interrupt(activityEvent.Category, start);
                        break;

                    case AppCategory.System:
                        Suspend(start);
                        break;
                }

                previousEnd = end;
            }

            if (_current != null)
            {
                DateTime finish = _current.IsActive
                    ? previousEnd ?? sessionEnd
                    : _suspendedAt ?? previousEnd ?? sessionEnd;

                Complete(finish, FocusBreakReason.SessionPaused);
            }

            foreach (FocusBlockModel block in _result.FocusBlocks)
            {
                if (block.AccumulatedDuration > _result.BestFocusBlockDuration)
                    _result.BestFocusBlockDuration = block.AccumulatedDuration;
            }

            return _result;
        }

        private void HandleGap(
            DateTime gapStart,
            DateTime gapEnd)
        {
            if (_current == null)
                return;

            if (gapEnd - gapStart > GracePeriod)
            {
                Complete(_current.IsActive ? gapStart : _suspendedAt ?? gapStart, FocusBreakReason.Idle);
                return;
            }

            Suspend(gapStart);
        }

        private void StartOrResume(
            ActivityEventModel activityEvent,
            DateTime start)
        {
            if (_current != null && _current.IsActive)
            {
                _current.LastWindowTitle = activityEvent.WindowTitle;
                return;
            }

            if (_current != null && _current.IsSuspended)
            {
                if (!_suspendedAt.HasValue || start - _suspendedAt.Value <= GracePeriod)
                {
                    _current.LastWindowTitle = activityEvent.WindowTitle;
                    _current.StartSegment(start);
                    _suspendedAt = null;
                    return;
                }

                Complete(_suspendedAt.Value, FocusBreakReason.ContextChanged);
            }

            FocusBlockModel block =
                new FocusBlockModel
                {
                    MainApp = activityEvent.AppName,
                    LastWindowTitle = activityEvent.WindowTitle,
                    Category = activityEvent.Category,
                    StartTime = start,
                    AccumulatedDuration = TimeSpan.Zero,
                    BreakReason = FocusBreakReason.None
                };

            block.StartSegment(start);
            _result.FocusBlocks.Add(block);
            _current = block;
            _suspendedAt = null;
        }

        private void Interrupt(
            AppCategory category,
            DateTime start)
        {
            if (_current == null)
                return;

            TimeSpan duration = GetDurationAt(_current, start);

            Complete(
                start,
                category == AppCategory.Distraction
                    ? FocusBreakReason.Distraction
                    : FocusBreakReason.Communication);

            if (duration >= MinimumInterruptedBlock)
                _result.InterruptionCount++;
        }

        private void Suspend(DateTime moment)
        {
            if (_current == null || !_current.IsActive)
                return;

            _current.PauseSegment(moment);
            _suspendedAt = moment;
        }

        private void Complete(
            DateTime endTime,
            FocusBreakReason reason)
        {
            _current.Complete(endTime, reason);
            _current = null;
            _suspendedAt = null;
        }

        private TimeSpan GetDurationAt(
            FocusBlockModel block,
            DateTime moment)
        {
            TimeSpan duration = block.AccumulatedDuration;

            if (block.IsActive && block.CurrentSegmentStartTime.HasValue)
            {
                TimeSpan currentPart = moment - block.CurrentSegmentStartTime.Value;

                if (currentPart > TimeSpan.Zero)
                    duration += currentPart;
            }

            return duration;
        }
    }
}
