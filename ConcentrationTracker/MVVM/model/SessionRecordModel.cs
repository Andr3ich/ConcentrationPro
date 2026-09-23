using System;
using System.Xml.Serialization;

namespace ConcentrationTracker.MVVM.Model
{
    [Serializable]
    public class SessionRecordModel
    {
        public string SessionId { get; set; }
        public DateTime StartedAt { get; set; }
        public DateTime EndedAt { get; set; }
        public string Status { get; set; }

        public int ConcentrationScore { get; set; }
        public int FocusQuality { get; set; }
        public int FocusStability { get; set; }

        public int TotalSwitches { get; set; }
        public int DisruptiveSwitches { get; set; }
        public int Interruptions { get; set; }
        public int IdleBreaks { get; set; }

        public double SessionSeconds { get; set; }
        public double TrackedSeconds { get; set; }
        public double AfkSeconds { get; set; }
        public double BestFocusBlockSeconds { get; set; }

        public double ProductiveSeconds { get; set; }
        public double NeutralSeconds { get; set; }
        public double CommunicationSeconds { get; set; }
        public double DistractionSeconds { get; set; }

        public string MostUsedApp { get; set; }
        public string MainWorkContext { get; set; }
        public string TopDistraction { get; set; }
        public string TopInterrupter { get; set; }
        public string Insight { get; set; }

        public SessionRecordModel()
        {
            SessionId = Guid.NewGuid().ToString();
            StartedAt = DateTime.Now;
            EndedAt = DateTime.Now;
            Status = "Saved";
            MostUsedApp = "None";
            MainWorkContext = "None";
            TopDistraction = "None";
            TopInterrupter = "None";
            Insight = string.Empty;
        }

        [XmlIgnore]
        public string StartedAtText
        {
            get { return StartedAt.ToString("dd.MM.yyyy HH:mm"); }
        }

        [XmlIgnore]
        public string EndedAtText
        {
            get { return EndedAt.ToString("HH:mm"); }
        }

        [XmlIgnore]
        public string SessionDurationText
        {
            get { return FormatSeconds(SessionSeconds); }
        }

        [XmlIgnore]
        public string TrackedDurationText
        {
            get { return FormatSeconds(TrackedSeconds); }
        }

        [XmlIgnore]
        public string AfkDurationText
        {
            get { return FormatSeconds(AfkSeconds); }
        }

        [XmlIgnore]
        public string BestFocusBlockText
        {
            get { return FormatSeconds(BestFocusBlockSeconds); }
        }

        [XmlIgnore]
        public string ProductiveDurationText
        {
            get { return FormatSeconds(ProductiveSeconds); }
        }

        [XmlIgnore]
        public string NeutralDurationText
        {
            get { return FormatSeconds(NeutralSeconds); }
        }

        [XmlIgnore]
        public string CommunicationDurationText
        {
            get { return FormatSeconds(CommunicationSeconds); }
        }

        [XmlIgnore]
        public string DistractionDurationText
        {
            get { return FormatSeconds(DistractionSeconds); }
        }

        [XmlIgnore]
        public string ScoreText
        {
            get { return ConcentrationScore + "%"; }
        }

        [XmlIgnore]
        public string FocusQualityText
        {
            get { return FocusQuality + "%"; }
        }

        [XmlIgnore]
        public string FocusStabilityText
        {
            get { return FocusStability + "%"; }
        }

        [XmlIgnore]
        public string SwitchesText
        {
            get { return TotalSwitches + " total · " + DisruptiveSwitches + " disruptive"; }
        }

        private string FormatSeconds(double seconds)
        {
            if (seconds <= 0)
                return "0 sec";

            TimeSpan duration = TimeSpan.FromSeconds(seconds);

            int hours = (int)duration.TotalHours;
            int minutes = duration.Minutes;
            int secs = duration.Seconds;

            if (hours > 0)
            {
                if (minutes > 0)
                    return hours + "h " + minutes + " min";

                return hours + "h";
            }

            if (minutes > 0)
            {
                if (secs > 0)
                    return minutes + " min " + secs + " sec";

                return minutes + " min";
            }

            return secs + " sec";
        }
    }
}
