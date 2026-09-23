using ConcentrationTracker.Core.Services;
using ConcentrationTracker.MVVM.Model;
using System;
using System.Collections.ObjectModel;

namespace ConcentrationTracker.MVVM.ViewModel
{
    public class SessionDetailsViewModel : BaseViewModel
    {
        private const double BreakdownBarMaxWidth = 360;

        public SessionRecordModel Session { get; private set; }
        public SessionDetailsModel Details { get; private set; }

        public ObservableCollection<SessionDetailActivityModel> ActivityEvents
        {
            get { return Details.ActivityEvents; }
        }

        public ObservableCollection<SessionDetailSwitchModel> SwitchEvents
        {
            get { return Details.SwitchEvents; }
        }

        public ObservableCollection<SessionDetailFocusBlockModel> FocusBlocks
        {
            get { return Details.FocusBlocks; }
        }

        public string Title
        {
            get
            {
                if (Session == null)
                    return LocalizationService.GetString("Details_WindowTitle");

                return LocalizationService.Format(
                    "Details_TitleWithDate",
                    Session.StartedAtText);
            }
        }

        public string SubtitleText
        {
            get
            {
                if (Session == null)
                    return LocalizationService.GetString("Details_SubtitleBase");

                return LocalizationService.Format(
                    "Details_SubtitleWithStatus",
                    GetLocalizedStatus(Session.Status));
            }
        }

        public string ScoreText
        {
            get { return Session == null ? "0%" : Session.ConcentrationScore + "%"; }
        }

        public string DurationText
        {
            get { return FormatDuration(Session != null ? Session.SessionSeconds : 0); }
        }

        public string TrackedText
        {
            get { return FormatDuration(Session != null ? Session.TrackedSeconds : 0); }
        }

        public string AfkText
        {
            get { return FormatDuration(Session != null ? Session.AfkSeconds : 0); }
        }

        public string SwitchesText
        {
            get
            {
                if (Session == null)
                    return "0";

                return LocalizationService.Format(
                    "Vm_TotalDisruptive",
                    Session.TotalSwitches,
                    Session.DisruptiveSwitches);
            }
        }

        public string FocusText
        {
            get
            {
                if (Session == null)
                    return "0%";

                return LocalizationService.Format(
                    "Details_FocusQualityStability",
                    Session.FocusQuality,
                    Session.FocusStability);
            }
        }

        public string QualityText
        {
            get { return Session == null ? "0%" : Session.FocusQuality + "%"; }
        }

        public string StabilityText
        {
            get { return Session == null ? "0%" : Session.FocusStability + "%"; }
        }

        public string BestFocusBlockText
        {
            get { return FormatDuration(Session != null ? Session.BestFocusBlockSeconds : 0); }
        }

        public string ProductiveText
        {
            get { return FormatDuration(Session != null ? Session.ProductiveSeconds : 0); }
        }

        public string NeutralText
        {
            get { return FormatDuration(Session != null ? Session.NeutralSeconds : 0); }
        }

        public string CommunicationText
        {
            get { return FormatDuration(Session != null ? Session.CommunicationSeconds : 0); }
        }

        public string DistractionText
        {
            get { return FormatDuration(Session != null ? Session.DistractionSeconds : 0); }
        }

        public string MostUsedAppText
        {
            get
            {
                if (Session == null || string.IsNullOrWhiteSpace(Session.MostUsedApp))
                    return LocalizationService.GetString("Common_NoData");

                return Session.MostUsedApp;
            }
        }

        public string MainWorkContextText
        {
            get
            {
                if (Session == null || string.IsNullOrWhiteSpace(Session.MainWorkContext))
                    return LocalizationService.GetString("Common_NoData");

                return Session.MainWorkContext;
            }
        }

        public string TopDistractionText
        {
            get
            {
                if (Session == null || string.IsNullOrWhiteSpace(Session.TopDistraction))
                    return LocalizationService.GetString("Common_NoData");

                return Session.TopDistraction;
            }
        }

        public string TopInterrupterText
        {
            get
            {
                if (Session == null || string.IsNullOrWhiteSpace(Session.TopInterrupter))
                    return LocalizationService.GetString("Common_NoData");

                return Session.TopInterrupter;
            }
        }

        public string InsightText
        {
            get
            {
                if (Session == null ||
                    string.IsNullOrWhiteSpace(Session.Insight))
                {
                    return LocalizationService.GetString("Details_NoInsight");
                }

                return Session.Insight;
            }
        }

        public string ActivityCountText
        {
            get { return ActivityEvents.Count.ToString(); }
        }

        public string SwitchCountText
        {
            get { return SwitchEvents.Count.ToString(); }
        }

        public string FocusBlockCountText
        {
            get { return FocusBlocks.Count.ToString(); }
        }

        public double ProductiveBarWidth
        {
            get { return GetBreakdownWidth(Session != null ? Session.ProductiveSeconds : 0); }
        }

        public double NeutralBarWidth
        {
            get { return GetBreakdownWidth(Session != null ? Session.NeutralSeconds : 0); }
        }

        public double CommunicationBarWidth
        {
            get { return GetBreakdownWidth(Session != null ? Session.CommunicationSeconds : 0); }
        }

        public double DistractionBarWidth
        {
            get { return GetBreakdownWidth(Session != null ? Session.DistractionSeconds : 0); }
        }

        public double AfkBarWidth
        {
            get { return GetBreakdownWidth(Session != null ? Session.AfkSeconds : 0); }
        }

        public SessionDetailsViewModel(
            SessionRecordModel session,
            SessionDetailsModel details)
        {
            Session =
                session;

            Details =
                details ?? new SessionDetailsModel();

            LocalizationService.LanguageChanged +=
                LocalizationService_LanguageChanged;
        }

        public void Detach()
        {
            LocalizationService.LanguageChanged -=
                LocalizationService_LanguageChanged;
        }

        private void LocalizationService_LanguageChanged(
            object sender,
            EventArgs e)
        {
            OnPropertyChanged(nameof(Title));
            OnPropertyChanged(nameof(SubtitleText));
            OnPropertyChanged(nameof(DurationText));
            OnPropertyChanged(nameof(TrackedText));
            OnPropertyChanged(nameof(AfkText));
            OnPropertyChanged(nameof(SwitchesText));
            OnPropertyChanged(nameof(FocusText));
            OnPropertyChanged(nameof(BestFocusBlockText));
            OnPropertyChanged(nameof(ProductiveText));
            OnPropertyChanged(nameof(NeutralText));
            OnPropertyChanged(nameof(CommunicationText));
            OnPropertyChanged(nameof(DistractionText));
            OnPropertyChanged(nameof(MostUsedAppText));
            OnPropertyChanged(nameof(MainWorkContextText));
            OnPropertyChanged(nameof(TopDistractionText));
            OnPropertyChanged(nameof(TopInterrupterText));
            OnPropertyChanged(nameof(InsightText));
        }

        private double GetBreakdownWidth(
            double seconds)
        {
            double totalSeconds =
                0;

            if (Session != null)
            {
                totalSeconds =
                    Session.ProductiveSeconds +
                    Session.NeutralSeconds +
                    Session.CommunicationSeconds +
                    Session.DistractionSeconds +
                    Session.AfkSeconds;
            }

            if (totalSeconds <= 0 ||
                seconds <= 0)
            {
                return 0;
            }

            double ratio =
                seconds / totalSeconds;

            if (ratio < 0)
                ratio = 0;

            if (ratio > 1)
                ratio = 1;

            return ratio * BreakdownBarMaxWidth;
        }

        private string GetLocalizedStatus(
            string status)
        {
            if (string.IsNullOrWhiteSpace(status))
                return LocalizationService.GetString("Status_Saved");

            string normalized =
                status.Trim().ToLowerInvariant();

            if (normalized == "completed" || normalized == "complete" || normalized == "finished")
                return LocalizationService.GetString("Status_Completed");

            if (normalized == "manual save" || normalized == "manual saved" || normalized == "manual")
                return LocalizationService.GetString("Status_ManualSave");

            if (normalized == "auto save" || normalized == "autosave" || normalized == "auto saved")
                return LocalizationService.GetString("Status_AutoSave");

            if (normalized == "saved")
                return LocalizationService.GetString("Status_Saved");

            return status;
        }

        private string FormatDuration(
            double seconds)
        {
            if (seconds < 0)
                seconds = 0;

            TimeSpan duration =
                TimeSpan.FromSeconds(seconds);

            if (duration.TotalSeconds < 1)
                return LocalizationService.GetString("Duration_Zero");

            if (duration.TotalHours >= 1)
            {
                return LocalizationService.Format(
                    "Duration_Hours",
                    (int)duration.TotalHours,
                    duration.Minutes,
                    duration.Seconds);
            }

            if (duration.TotalMinutes >= 1)
            {
                return LocalizationService.Format(
                    "Duration_Minutes",
                    duration.Minutes,
                    duration.Seconds);
            }

            return LocalizationService.Format(
                "Duration_Seconds",
                Math.Round(duration.TotalSeconds));
        }
    }
}
