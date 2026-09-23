namespace ConcentrationTracker.MVVM.Model
{
    public class SessionDetailFocusBlockModel
    {
        public string MainApp { get; set; }
        public string LastWindowTitle { get; set; }
        public string Category { get; set; }
        public string StartText { get; set; }
        public string EndText { get; set; }
        public string DurationText { get; set; }
        public string BreakReason { get; set; }

        public SessionDetailFocusBlockModel()
        {
            MainApp = string.Empty;
            LastWindowTitle = string.Empty;
            Category = string.Empty;
            StartText = string.Empty;
            EndText = string.Empty;
            DurationText = string.Empty;
            BreakReason = string.Empty;
        }
    }
}
