using ConcentrationTracker.MVVM.Model;
using ControlzEx.Theming;
using MahApps.Metro.Theming;
using System;
using System.Linq;
using System.Windows;

namespace ConcentrationTracker.Core.Services
{
    public static class ThemeService
    {
        private const string LightThemePath =
            "/ConcentrationTracker;component/Resources/Themes/ThemeLight.xaml";

        private const string DarkThemePath =
            "/ConcentrationTracker;component/Resources/Themes/ThemeDark.xaml";

        public static event EventHandler ThemeChanging;
        public static event EventHandler ThemeChanged;

        public static AppTheme CurrentTheme { get; private set; }

        public static void ApplyTheme(
            AppTheme theme)
        {
            if (Application.Current == null)
                return;

            ThemeChanging?.Invoke(
                null,
                EventArgs.Empty);

            string mahAppsTheme =
                theme == AppTheme.Dark
                    ? "Dark.Blue"
                    : "Light.Blue";

            try
            {
                ThemeManager.Current.ChangeTheme(
                    Application.Current,
                    mahAppsTheme);
            }
            catch
            {

            }

            string themePath =
                theme == AppTheme.Dark
                    ? DarkThemePath
                    : LightThemePath;

            RemoveThemeDictionaries();

            ResourceDictionary dictionary =
                new ResourceDictionary
                {
                    Source =
                        new Uri(
                            themePath,
                            UriKind.RelativeOrAbsolute)
                };

            Application.Current.Resources.MergedDictionaries.Add(
                dictionary);

            CurrentTheme =
                theme;

            ThemeChanged?.Invoke(
                null,
                EventArgs.Empty);
        }

        public static void ToggleTheme()
        {
            ApplyTheme(
                CurrentTheme == AppTheme.Dark
                    ? AppTheme.Light
                    : AppTheme.Dark);
        }

        private static void RemoveThemeDictionaries()
        {
            var dictionariesToRemove =
                Application.Current.Resources.MergedDictionaries
                    .Where(x =>
                        x.Source != null &&
                        (
                            x.Source.OriginalString.IndexOf("ThemeLight.xaml", StringComparison.OrdinalIgnoreCase) >= 0 ||
                            x.Source.OriginalString.IndexOf("ThemeDark.xaml", StringComparison.OrdinalIgnoreCase) >= 0
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
