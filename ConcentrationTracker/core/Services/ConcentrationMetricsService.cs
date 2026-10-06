using ConcentrationTracker.MVVM.Model;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ConcentrationTracker.Core.Services
{
    public class ConcentrationMetricsService
    {
        private const double ProductiveWeight = 0.30;
        private const double FocusQualityWeight = 0.40;
        private const double FocusStabilityWeight = 0.30;

        private const double NeutralFocusMultiplier = 0.55;

        private const double CommunicationRatioWeight = 15.75;
        private const double HarmfulRatioWeight = 65.0;

        private const double InterruptionPenaltyPerEvent = 5.0;
        private const double InterruptionPenaltyCap = 25.0;

        private const double ShortActivityPenaltyPerEvent = 1.5;
        private const double ShortActivityPenaltyCap = 18.0;

        private const double SwitchPenaltyPerEvent = 7.0;
        private const double DisruptivePenaltyPerEvent = 12.0;
        private const double ShortEventPenaltyPerEvent = 4.0;

        private const double RecoveryBonusPerMinute = 3.0;
        private const double RecoveryBonusCap = 12.0;
        private const double RecoveryBonusMinDurationSeconds = 30;

        private const double BestBlockBonusTier1Minutes = 25;
        private const double BestBlockBonusTier1Value = 15;
        private const double BestBlockBonusTier2Minutes = 15;
        private const double BestBlockBonusTier2Value = 10;
        private const double BestBlockBonusTier3Minutes = 8;
        private const double BestBlockBonusTier3Value = 6;
        private const double BestBlockBonusTier4Minutes = 3;
        private const double BestBlockBonusTier4Value = 3;

        private const double ShortEventThresholdSeconds = 5;

        private readonly TimeSpan _stabilityWindow =
            TimeSpan.FromMinutes(12);

        public int CalculateProductiveTimePercent(
            IEnumerable<ActivityEventModel> activityEvents,
            TimeSpan trackedElapsed,
            DateTime now)
        {
            if (activityEvents == null ||
                trackedElapsed.TotalSeconds <= 0)
            {
                return 0;
            }

            double productiveSeconds =
                activityEvents
                    .Where(x => x.Category == AppCategory.Productive)
                    .Sum(x => GetActivityDuration(x, now).TotalSeconds);

            double percent =
                productiveSeconds / trackedElapsed.TotalSeconds * 100.0;

            return ClampPercent(percent);
        }

        public int CalculateConcentrationScore(
            IEnumerable<ActivityEventModel> activityEvents,
            IEnumerable<FocusBlockModel> focusBlocks,
            TimeSpan trackedElapsed,
            int interruptionCount,
            DateTime now)
        {
            if (trackedElapsed.TotalSeconds <= 0)
                return 0;

            int productivePercent =
                CalculateProductiveTimePercent(
                    activityEvents,
                    trackedElapsed,
                    now);

            int focusQuality =
                CalculateFocusQualityPercent(
                    activityEvents,
                    focusBlocks,
                    trackedElapsed,
                    interruptionCount,
                    now);

            int focusStability =
                CalculateFocusStabilityPercent(
                    activityEvents,
                    trackedElapsed,
                    now);

            double score =
                productivePercent * ProductiveWeight +
                focusQuality * FocusQualityWeight +
                focusStability * FocusStabilityWeight;

            return ClampPercent(score);
        }

        public int CalculateFocusQualityPercent(
            IEnumerable<ActivityEventModel> activityEvents,
            IEnumerable<FocusBlockModel> focusBlocks,
            TimeSpan trackedElapsed,
            int interruptionCount,
            DateTime now)
        {
            if (trackedElapsed.TotalSeconds <= 0)
                return 0;

            List<ActivityEventModel> events =
                activityEvents?
                    .ToList() ??
                new List<ActivityEventModel>();

            if (events.Count == 0)
                return 0;

            double productiveSeconds =
                GetCategorySeconds(
                    events,
                    AppCategory.Productive,
                    now);

            double neutralSeconds =
                GetCategorySeconds(
                    events,
                    AppCategory.Neutral,
                    now);

            double communicationSeconds =
                GetCategorySeconds(
                    events,
                    AppCategory.Communication,
                    now);

            double distractionSeconds =
                GetCategorySeconds(
                    events,
                    AppCategory.Distraction,
                    now);

            double focusFriendlySeconds =
                productiveSeconds +
                neutralSeconds * NeutralFocusMultiplier;

            double harmfulSeconds =
                distractionSeconds;

            double focusFriendlyRatio =
                focusFriendlySeconds / trackedElapsed.TotalSeconds;

            double communicationRatio =
                communicationSeconds / trackedElapsed.TotalSeconds;

            double harmfulRatio =
                harmfulSeconds / trackedElapsed.TotalSeconds;

            double bestFocusBlockBonus =
                GetBestFocusBlockBonus(focusBlocks);

            double interruptionPenalty =
                Math.Min(
                    InterruptionPenaltyCap,
                    interruptionCount * InterruptionPenaltyPerEvent);

            double shortActivityPenalty =
                CalculateShortActivityPenalty(
                    events,
                    now);

            double result =
                focusFriendlyRatio * 100.0 +
                communicationRatio * CommunicationRatioWeight -
                harmfulRatio * HarmfulRatioWeight -
                interruptionPenalty -
                shortActivityPenalty +
                bestFocusBlockBonus;

            return ClampPercent(result);
        }

        public int CalculateFocusStabilityPercent(
            IEnumerable<ActivityEventModel> activityEvents,
            TimeSpan trackedElapsed,
            DateTime now)
        {
            if (trackedElapsed.TotalSeconds <= 0)
                return 0;

            List<ActivityEventModel> events =
                activityEvents?
                    .ToList() ??
                new List<ActivityEventModel>();

            if (events.Count == 0)
                return 0;

            DateTime windowStart =
                now - _stabilityWindow;

            List<ActivityEventModel> recentEvents =
                events
                    .Where(x => ActivityOverlapsWindow(x, windowStart, now))
                    .OrderBy(x => x.StartTime)
                    .ToList();

            if (recentEvents.Count == 0)
                return 100;

            List<ActivityEventModel> substantialEvents =
                recentEvents
                    .Where(x =>
                        GetActivityDurationInsideWindow(x, windowStart, now).TotalSeconds
                            >= ShortEventThresholdSeconds)
                    .ToList();

            int recentWindowTransitions =
                Math.Max(
                    0,
                    substantialEvents.Count - 1);

            int disruptiveEvents =
                substantialEvents.Count(x =>
                    x.Category == AppCategory.Distraction ||
                    x.Category == AppCategory.Communication);

            int shortEvents =
                recentEvents.Count(x =>
                {
                    TimeSpan duration =
                        GetActivityDurationInsideWindow(
                            x,
                            windowStart,
                            now);

                    return duration.TotalSeconds > 0 &&
                           duration.TotalSeconds < ShortEventThresholdSeconds;
                });

            ActivityEventModel currentEvent =
                recentEvents
                    .OrderByDescending(x => x.StartTime)
                    .FirstOrDefault();

            double recoveryBonus =
                CalculateStabilityRecoveryBonus(
                    currentEvent,
                    now);

            double switchPenalty =
                recentWindowTransitions * SwitchPenaltyPerEvent;

            double disruptivePenalty =
                disruptiveEvents * DisruptivePenaltyPerEvent;

            double shortEventPenalty =
                shortEvents * ShortEventPenaltyPerEvent;

            double result =
                100.0 -
                switchPenalty -
                disruptivePenalty -
                shortEventPenalty +
                recoveryBonus;

            return ClampPercent(result);
        }

        private double GetCategorySeconds(
            IEnumerable<ActivityEventModel> activityEvents,
            AppCategory category,
            DateTime now)
        {
            return activityEvents
                .Where(x => x.Category == category)
                .Sum(x => GetActivityDuration(x, now).TotalSeconds);
        }

        private TimeSpan GetActivityDuration(
            ActivityEventModel activityEvent,
            DateTime now)
        {
            if (activityEvent == null)
                return TimeSpan.Zero;

            DateTime endTime =
                activityEvent.EndTime ?? now;

            TimeSpan duration =
                endTime - activityEvent.StartTime;

            if (duration.TotalSeconds < 0)
                return TimeSpan.Zero;

            return duration;
        }

        private bool ActivityOverlapsWindow(
            ActivityEventModel activityEvent,
            DateTime windowStart,
            DateTime now)
        {
            if (activityEvent == null)
                return false;

            DateTime activityEnd =
                activityEvent.EndTime ?? now;

            return activityEvent.StartTime <= now &&
                   activityEnd >= windowStart;
        }

        private TimeSpan GetActivityDurationInsideWindow(
            ActivityEventModel activityEvent,
            DateTime windowStart,
            DateTime now)
        {
            if (activityEvent == null)
                return TimeSpan.Zero;

            DateTime activityStart =
                activityEvent.StartTime > windowStart
                    ? activityEvent.StartTime
                    : windowStart;

            DateTime activityEnd =
                activityEvent.EndTime ?? now;

            if (activityEnd > now)
                activityEnd = now;

            TimeSpan duration =
                activityEnd - activityStart;

            if (duration.TotalSeconds < 0)
                return TimeSpan.Zero;

            return duration;
        }

        private double CalculateStabilityRecoveryBonus(
            ActivityEventModel currentEvent,
            DateTime now)
        {
            if (currentEvent == null)
                return 0;

            bool isFocusFriendly =
                currentEvent.Category == AppCategory.Productive ||
                currentEvent.Category == AppCategory.Neutral;

            if (!isFocusFriendly)
                return 0;

            TimeSpan currentDuration =
                GetActivityDuration(
                    currentEvent,
                    now);

            if (currentDuration.TotalSeconds < RecoveryBonusMinDurationSeconds)
                return 0;

            double bonus =
                currentDuration.TotalMinutes * RecoveryBonusPerMinute;

            if (bonus > RecoveryBonusCap)
                bonus = RecoveryBonusCap;

            return bonus;
        }

        private double GetBestFocusBlockBonus(
            IEnumerable<FocusBlockModel> focusBlocks)
        {
            if (focusBlocks == null)
                return 0;

            TimeSpan best =
                TimeSpan.Zero;

            foreach (FocusBlockModel block in focusBlocks)
            {
                if (block == null)
                    continue;

                if (block.CurrentDuration > best)
                    best = block.CurrentDuration;
            }

            if (best.TotalMinutes >= BestBlockBonusTier1Minutes)
                return BestBlockBonusTier1Value;

            if (best.TotalMinutes >= BestBlockBonusTier2Minutes)
                return BestBlockBonusTier2Value;

            if (best.TotalMinutes >= BestBlockBonusTier3Minutes)
                return BestBlockBonusTier3Value;

            if (best.TotalMinutes >= BestBlockBonusTier4Minutes)
                return BestBlockBonusTier4Value;

            return 0;
        }

        private double CalculateShortActivityPenalty(
            IEnumerable<ActivityEventModel> activityEvents,
            DateTime now)
        {
            if (activityEvents == null)
                return 0;

            int shortEvents =
                activityEvents.Count(x =>
                {
                    TimeSpan duration =
                        GetActivityDuration(x, now);

                    return duration.TotalSeconds > 0 &&
                           duration.TotalSeconds < ShortEventThresholdSeconds;
                });

            return Math.Min(
                ShortActivityPenaltyCap,
                shortEvents * ShortActivityPenaltyPerEvent);
        }

        private int ClampPercent(
            double value)
        {
            if (double.IsNaN(value) ||
                double.IsInfinity(value))
            {
                return 0;
            }

            if (value < 0)
                return 0;

            if (value > 100)
                return 100;

            return (int)Math.Round(value);
        }
    }
}