using System;

namespace ConcentrationTracker.MVVM.Model
{
    [Serializable]
    public class AppSettingsModel
    {
        public bool StartWithWindows { get; set; }

        public bool StartMinimizedToTray { get; set; }

        public bool MinimizeToTray { get; set; }

        public bool ShowTrayNotification { get; set; }

        public double IdleThresholdMinutes { get; set; }

        public int PendingIdleGraceSeconds { get; set; }

        public DashboardMode DefaultDashboardTab { get; set; }

        public AppLanguage InterfaceLanguage { get; set; }

        public AppSettingsModel()
        {
            StartWithWindows = false;
            StartMinimizedToTray = false;
            MinimizeToTray = true;
            ShowTrayNotification = true;
            IdleThresholdMinutes = 2.0;
            PendingIdleGraceSeconds = 15;
            DefaultDashboardTab = DashboardMode.Charts;
            InterfaceLanguage = AppLanguage.English;
        }
    }
}
