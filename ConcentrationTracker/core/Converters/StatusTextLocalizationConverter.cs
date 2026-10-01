using ConcentrationTracker.Core.Services;
using System;
using System.Globalization;
using System.Windows.Data;

namespace ConcentrationTracker.Core.Converters
{
    public class StatusTextLocalizationConverter : IValueConverter
    {
        public object Convert(
            object value,
            Type targetType,
            object parameter,
            CultureInfo culture)
        {
            string text =
                value == null
                    ? string.Empty
                    : value.ToString().Trim();

            if (string.IsNullOrWhiteSpace(text))
                return text;

            string normalized =
                text.ToLowerInvariant();

            if (normalized == "active now" ||
                normalized == "now active" ||
                normalized == "current")
            {
                return LocalizationService.GetString(
                    "Timeline_StatusActiveNow");
            }

            if (normalized == "completed" ||
                normalized == "complete" ||
                normalized == "finished")
            {
                return LocalizationService.GetString(
                    "Status_Completed");
            }

            if (normalized == "manual save" ||
                normalized == "manual saved" ||
                normalized == "manual")
            {
                return LocalizationService.GetString(
                    "Status_ManualSave");
            }

            if (normalized == "auto save" ||
                normalized == "autosave" ||
                normalized == "auto saved")
            {
                return LocalizationService.GetString(
                    "Status_AutoSave");
            }

            if (normalized == "saved")
            {
                return LocalizationService.GetString(
                    "Status_Saved");
            }

            if (normalized == "active" ||
                normalized == "running" ||
                normalized == "in progress")
            {
                return LocalizationService.GetString(
                    "Timeline_StatusActive");
            }

            if (normalized == "paused")
            {
                return LocalizationService.GetString(
                    "Timeline_StatusPaused");
            }

            if (normalized == "yes" ||
                normalized == "true")
            {
                return LocalizationService.GetString(
                    "Common_Yes");
            }

            if (normalized == "no" ||
                normalized == "false")
            {
                return LocalizationService.GetString(
                    "Common_No");
            }

            if (normalized == "window switch" ||
                normalized == "switch" ||
                normalized == "context changed")
            {
                return LocalizationService.GetString(
                    "Details_BreakReasonWindowSwitch");
            }

            if (normalized == "idle" ||
                normalized == "afk")
            {
                return LocalizationService.GetString(
                    "Details_BreakReasonAway");
            }

            if (normalized == "distraction")
            {
                return LocalizationService.GetString(
                    "Category_Distraction");
            }

            if (normalized == "communication")
            {
                return LocalizationService.GetString(
                    "Category_Communication");
            }

            if (normalized == "session ended" ||
                normalized == "session end" ||
                normalized == "saved session end")
            {
                return LocalizationService.GetString(
                    "Details_BreakReasonSessionEnded");
            }

            return text;
        }

        public object ConvertBack(
            object value,
            Type targetType,
            object parameter,
            CultureInfo culture)
        {
            return Binding.DoNothing;
        }
    }
}
