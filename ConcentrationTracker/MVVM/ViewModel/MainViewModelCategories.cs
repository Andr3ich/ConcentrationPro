using ConcentrationTracker.Core.Services;
using ConcentrationTracker.MVVM.Model;
using ConcentrationTracker.MVVM.View.Windows;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Threading;

namespace ConcentrationTracker.MVVM.ViewModel
{
    public partial class MainViewModel
    {
        private void LoadCategoryRules()
        {
            CategoryRules.Clear();

            foreach (AppCategoryRuleModel rule in _categoryRuleService.GetRules())
            {
                CategoryRules.Add(rule);
            }

            CategoryRulesStatus = LocalizationService.Format("Categories_StatusLoaded", CategoryRules.Count);
        }

        private void AddCategoryRule()
        {
            string appPattern = NewRuleAppPattern ?? string.Empty;
            string titlePattern = NewRuleTitlePattern ?? string.Empty;

            if (string.IsNullOrWhiteSpace(appPattern))
            {
                CategoryRulesStatus = LocalizationService.GetString("Categories_StatusNeedApp");
                return;
            }

            if (string.IsNullOrWhiteSpace(titlePattern))
            {
                titlePattern = "*";
            }

            AppCategoryRuleModel rule =
                new AppCategoryRuleModel
                {
                    AppPattern = appPattern.Trim(),
                    TitlePattern = titlePattern.Trim(),
                    Category = NewRuleCategory,
                    IsEnabled = true
                };

            CategoryRules.Add(rule);
            SelectedCategoryRule = rule;

            SaveCategoryRules();

            NewRuleAppPattern = string.Empty;
            NewRuleTitlePattern = "*";
            NewRuleCategory = AppCategory.Neutral;
        }

        private void DeleteCategoryRule(object parameter)
        {
            AppCategoryRuleModel rule =
                parameter as AppCategoryRuleModel ?? SelectedCategoryRule;

            if (rule == null)
            {
                CategoryRulesStatus = LocalizationService.GetString("Categories_StatusSelectDelete");
                return;
            }

            CategoryRules.Remove(rule);
            SelectedCategoryRule = null;

            SaveCategoryRules();
        }

        private void SaveCategoryRules()
        {
            _categoryRuleService.SaveRules(
                CategoryRules.ToList());

            CategoryRulesStatus = LocalizationService.Format("Categories_StatusSaved", CategoryRules.Count);
        }

        private void ReloadCategoryRules()
        {
            _categoryRuleService.Reload();
            LoadCategoryRules();
        }

        private void UseCurrentActivityForRule()
        {
            ActiveWindowInfo source =
                GetQuickClassifySource();

            if (source == null)
            {
                CategoryRulesStatus = LocalizationService.GetString("Categories_StatusNeedExternal");
                return;
            }

            NewRuleAppPattern = source.AppName;
            NewRuleTitlePattern = GetSuggestedTitlePatternForRule(source);
            NewRuleCategory = _lastClassifiableActivityCategory;

            CategoryRulesStatus =
                LocalizationService.Format(
                    "Categories_StatusCopied",
                    GetQuickClassifyDisplayName(source));
        }

        private void SetCurrentActivityCategory(object parameter)
        {
            ActiveWindowInfo source =
                GetQuickClassifySource();

            if (source == null)
            {
                CategoryRulesStatus = LocalizationService.GetString("Categories_StatusNeedExternal");
                return;
            }

            AppCategory category =
                ParseCategoryParameter(
                    parameter,
                    _lastClassifiableActivityCategory);

            AppCategoryRuleModel rule =
                new AppCategoryRuleModel
                {
                    AppPattern = source.AppName,
                    TitlePattern = GetSuggestedTitlePatternForRule(source),
                    Category = category,
                    IsEnabled = true
                };

            CategoryRules.Add(rule);
            SelectedCategoryRule = rule;
            SaveCategoryRules();

            CategoryRulesStatus =
                LocalizationService.Format(
                    "Categories_StatusRuleAdded",
                    GetQuickClassifyDisplayName(source),
                    GetCategoryDisplayName(category));
        }

        private void RememberClassifiableActivity(
            ActiveWindowInfo activeWindow,
            AppCategory category)
        {
            if (activeWindow == null)
                return;

            if (IsOwnApplication(activeWindow))
                return;

            if (string.IsNullOrWhiteSpace(activeWindow.AppName))
                return;

            if (activeWindow.AppName.Equals("Dashboard", StringComparison.OrdinalIgnoreCase) ||
                activeWindow.AppName.Equals("Idle", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            _lastClassifiableActivity =
                new ActiveWindowInfo
                {
                    WindowHandle = activeWindow.WindowHandle,
                    ProcessId = activeWindow.ProcessId,
                    AppName = activeWindow.AppName,
                    ProcessPath = activeWindow.ProcessPath,
                    AppUserModelId = activeWindow.AppUserModelId,
                    PackageFullName = activeWindow.PackageFullName,
                    PackageInstallPath = activeWindow.PackageInstallPath,
                    WindowTitle = activeWindow.WindowTitle,
                    DetectedAt = activeWindow.DetectedAt
                };

            _lastClassifiableActivityCategory =
                category;

            OnPropertyChanged(nameof(QuickClassifySourceText));
        }

        private ActiveWindowInfo GetQuickClassifySource()
        {
            if (_lastClassifiableActivity != null)
                return _lastClassifiableActivity;

            if (!string.IsNullOrWhiteSpace(CurrentApp) &&
                !IsNoActiveAppPlaceholder(CurrentApp) &&
                CurrentApp != "Dashboard" &&
                CurrentApp != "Idle")
            {
                return new ActiveWindowInfo
                {
                    AppName = CurrentApp,
                    WindowTitle = CurrentWindowTitle,
                    ProcessPath = CurrentProcessPath,
                    AppUserModelId = CurrentAppUserModelId,
                    PackageFullName = CurrentPackageFullName,
                    PackageInstallPath = CurrentPackageInstallPath,
                    DetectedAt = DateTime.Now
                };
            }

            return null;
        }

        private string GetSuggestedTitlePatternForRule(
            ActiveWindowInfo source)
        {
            if (source == null)
                return "*";

            if (IsBrowserAppName(source.AppName))
            {
                string context =
                    ExtractBrowserContextName(
                        source.WindowTitle,
                        source.AppName);

                if (!string.IsNullOrWhiteSpace(context) &&
                    !context.Equals(source.AppName, StringComparison.OrdinalIgnoreCase))
                {
                    return context.ToLowerInvariant();
                }
            }

            return "*";
        }

        private string GetCategoryDisplayName(
            AppCategory category)
        {
            switch (category)
            {
                case AppCategory.Productive:
                    return LocalizationService.GetString("Category_Productive");

                case AppCategory.Neutral:
                    return LocalizationService.GetString("Category_Neutral");

                case AppCategory.Communication:
                    return LocalizationService.GetString("Category_Communication");

                case AppCategory.Distraction:
                    return LocalizationService.GetString("Category_Distraction");

                case AppCategory.System:
                    return LocalizationService.GetString("Category_System");

                default:
                    return category.ToString();
            }
        }

        private bool IsNoActiveAppPlaceholder(
            string appName)
        {
            if (string.IsNullOrWhiteSpace(appName))
                return true;

            return string.Equals(appName, "No active app yet", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(appName, "Активної програми ще немає", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(appName, LocalizationService.GetString("Focus_NoActiveApp"), StringComparison.OrdinalIgnoreCase);
        }

        private string GetQuickClassifyDisplayName(
            ActiveWindowInfo source)
        {
            if (source == null)
                return "No external app captured yet";

            if (IsBrowserAppName(source.AppName))
            {
                string context =
                    ExtractBrowserContextName(
                        source.WindowTitle,
                        source.AppName);

                if (!string.IsNullOrWhiteSpace(context) &&
                    !context.Equals(source.AppName, StringComparison.OrdinalIgnoreCase))
                {
                    return context + " in " + source.AppName;
                }
            }

            return source.AppName;
        }

        private AppCategory ParseCategoryParameter(
            object parameter,
            AppCategory fallback)
        {
            if (parameter == null)
                return fallback;

            AppCategory parsedCategory;

            if (Enum.TryParse(
                    parameter.ToString(),
                    true,
                    out parsedCategory))
            {
                return parsedCategory;
            }

            return fallback;
        }
    }
}