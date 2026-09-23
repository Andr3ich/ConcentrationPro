using ConcentrationTracker.MVVM.Model;
using System;
using System.Linq;
using System.Windows;

namespace ConcentrationTracker.Core.Services
{
    public static class LocalizationService
    {
        private const string EnglishDictionaryPath =
            "/ConcentrationTracker;component/Resources/Localization/StringsEn.xaml";

        private const string UkrainianDictionaryPath =
            "/ConcentrationTracker;component/Resources/Localization/StringsUk.xaml";

        private const string EnglishTrayDictionaryPath =
            "/ConcentrationTracker;component/Resources/Localization/TrayStringsEn.xaml";

        private const string UkrainianTrayDictionaryPath =
            "/ConcentrationTracker;component/Resources/Localization/TrayStringsUk.xaml";

        public static event EventHandler LanguageChanged;

        public static AppLanguage CurrentLanguage { get; private set; } =
            AppLanguage.English;

        public static void ApplyLanguage(
            AppLanguage language,
            bool persist = true)
        {
            if (Application.Current == null)
                return;

            RemoveLocalizationDictionaries();

            AddDictionary(
                language == AppLanguage.Ukrainian
                    ? UkrainianDictionaryPath
                    : EnglishDictionaryPath);

            AddDictionary(
                language == AppLanguage.Ukrainian
                    ? UkrainianTrayDictionaryPath
                    : EnglishTrayDictionaryPath);

            CurrentLanguage =
                language;

            if (persist)
            {
                PersistLanguagePreference(
                    language);
            }

            LanguageChanged?.Invoke(
                null,
                EventArgs.Empty);
        }

        private static void PersistLanguagePreference(
            AppLanguage language)
        {
            try
            {
                AppSettingsModel settings =
                    AppSettingsService.LoadSettings();

                settings.InterfaceLanguage =
                    language;

                AppSettingsService.SaveSettings(
                    settings);
            }
            catch
            {

            }
        }

        public static void ToggleLanguage()
        {
            ApplyLanguage(
                CurrentLanguage == AppLanguage.Ukrainian
                    ? AppLanguage.English
                    : AppLanguage.Ukrainian);
        }

        public static string GetCurrentLanguageShortName()
        {
            return CurrentLanguage == AppLanguage.Ukrainian
                ? "UA"
                : "EN";
        }

        public static string GetNextLanguageShortName()
        {
            return CurrentLanguage == AppLanguage.Ukrainian
                ? "EN"
                : "UA";
        }

        public static string GetString(
            string key)
        {
            if (Application.Current == null ||
                string.IsNullOrWhiteSpace(key))
            {
                return key;
            }

            object value =
                Application.Current.TryFindResource(
                    key);

            return value != null
                ? value.ToString()
                : key;
        }

        public static string Format(
            string key,
            params object[] args)
        {
            string template =
                GetString(
                    key);

            try
            {
                return string.Format(
                    template,
                    args);
            }
            catch
            {
                return template;
            }
        }

        private static void AddDictionary(
            string path)
        {
            ResourceDictionary dictionary =
                new ResourceDictionary
                {
                    Source =
                        new Uri(
                            path,
                            UriKind.RelativeOrAbsolute)
                };

            Application.Current.Resources.MergedDictionaries.Add(
                dictionary);
        }

        private static void RemoveLocalizationDictionaries()
        {
            var dictionariesToRemove =
                Application.Current.Resources.MergedDictionaries
                    .Where(x =>
                        x.Source != null &&
                        (
                            x.Source.OriginalString.IndexOf("StringsEn.xaml", StringComparison.OrdinalIgnoreCase) >= 0 ||
                            x.Source.OriginalString.IndexOf("StringsUk.xaml", StringComparison.OrdinalIgnoreCase) >= 0 ||
                            x.Source.OriginalString.IndexOf("TrayStringsEn.xaml", StringComparison.OrdinalIgnoreCase) >= 0 ||
                            x.Source.OriginalString.IndexOf("TrayStringsUk.xaml", StringComparison.OrdinalIgnoreCase) >= 0
                        ))
                    .ToList();

            foreach (ResourceDictionary dictionary in dictionariesToRemove)
            {
                Application.Current.Resources.MergedDictionaries.Remove(
                    dictionary);
            }
        }
    }
}
