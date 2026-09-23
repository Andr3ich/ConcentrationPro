using System;

namespace ConcentrationTracker.MVVM.Model
{
    public class ActiveWindowInfo
    {
        public IntPtr WindowHandle { get; set; }

        public int ProcessId { get; set; }

        public string AppName { get; set; }

        public string ProcessPath { get; set; }

        public string AppUserModelId { get; set; }

        public string PackageFullName { get; set; }

        public string PackageInstallPath { get; set; }

        public string WindowTitle { get; set; }

        public DateTime DetectedAt { get; set; }
    }
}
