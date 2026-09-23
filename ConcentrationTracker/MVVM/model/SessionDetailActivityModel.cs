namespace ConcentrationTracker.MVVM.Model
{
    public class SessionDetailActivityModel
    {
        public string AppName { get; set; }
        public string WindowTitle { get; set; }
        public string Category { get; set; }
        public string StartText { get; set; }
        public string EndText { get; set; }
        public string DurationText { get; set; }

        public SessionDetailActivityModel()
        {
            AppName = string.Empty;
            WindowTitle = string.Empty;
            Category = string.Empty;
            StartText = string.Empty;
            EndText = string.Empty;
            DurationText = string.Empty;
        }
    }
}
