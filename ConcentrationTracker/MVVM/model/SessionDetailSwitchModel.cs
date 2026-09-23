namespace ConcentrationTracker.MVVM.Model
{
    public class SessionDetailSwitchModel
    {
        public string TimeText { get; set; }
        public string FromCategory { get; set; }
        public string ToCategory { get; set; }
        public string ToAppName { get; set; }
        public string ToWindowTitle { get; set; }
        public string IsDisruptiveText { get; set; }

        public SessionDetailSwitchModel()
        {
            TimeText = string.Empty;
            FromCategory = string.Empty;
            ToCategory = string.Empty;
            ToAppName = string.Empty;
            ToWindowTitle = string.Empty;
            IsDisruptiveText = string.Empty;
        }
    }
}
