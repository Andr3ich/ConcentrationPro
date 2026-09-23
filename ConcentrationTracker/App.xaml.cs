using ConcentrationTracker.Core.Database;
using ConcentrationTracker.Core.Services;
using ConcentrationTracker.MVVM.Model;
using System.Windows;

namespace ConcentrationTracker
{
    public partial class App : Application
    {
        protected override void OnStartup(
            StartupEventArgs e)
        {
            DatabaseInitializer.Initialize();

            AppSettingsModel settings =
                AppSettingsService.LoadSettings();

            LocalizationService.ApplyLanguage(
                settings.InterfaceLanguage,
                false);

            base.OnStartup(
                e);
        }
    }
}
