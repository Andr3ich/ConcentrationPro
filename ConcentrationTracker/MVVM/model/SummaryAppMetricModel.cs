using ConcentrationTracker.Core.Services;
using ConcentrationTracker.MVVM.Model;

namespace ConcentrationTracker.MVVM.Model
{
    public class SummaryAppMetricModel
    {
        public string Title { get; set; }
        public string AppName { get; set; }
        public string WindowTitle { get; set; }
        public string DurationText { get; set; }
        public string ProcessPath { get; set; }
        public string AppUserModelId { get; set; }
        public string PackageFullName { get; set; }
        public string PackageInstallPath { get; set; }
        public AppCategory Category { get; set; }
        public bool HasValue { get; set; }

        public string DisplayText
        {
            get
            {
                if (!HasValue)
                    return LocalizationService.GetString("Common_None");

                if (string.IsNullOrWhiteSpace(DurationText))
                    return AppName;

                return AppName + " · " + DurationText;
            }
        }

        public static SummaryAppMetricModel Empty(string title)
        {
            return new SummaryAppMetricModel
            {
                Title = title,
                AppName = LocalizationService.GetString("Common_None"),
                WindowTitle = string.Empty,
                DurationText = string.Empty,
                ProcessPath = string.Empty,
                AppUserModelId = string.Empty,
                PackageFullName = string.Empty,
                PackageInstallPath = string.Empty,
                Category = AppCategory.Neutral,
                HasValue = false
            };
        }
    }
}
