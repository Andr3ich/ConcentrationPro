using System;

namespace ConcentrationTracker.MVVM.Model
{
    [Serializable]
    public class AppCategoryRuleModel
    {
        public string AppPattern { get; set; }

        public string TitlePattern { get; set; }

        public AppCategory Category { get; set; }

        public bool IsEnabled { get; set; }

        public AppCategoryRuleModel()
        {
            AppPattern = string.Empty;
            TitlePattern = string.Empty;
            Category = AppCategory.Neutral;
            IsEnabled = true;
        }
    }
}
