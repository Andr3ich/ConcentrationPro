using ConcentrationTracker.Core.Services;
using ConcentrationTracker.MVVM.Model;
using ConcentrationTracker.MVVM.View.Windows;
using MahApps.Metro.IconPacks;
using System;
using System.Windows;
using System.Windows.Controls;

namespace ConcentrationTracker.MVVM.View.Controls
{
    public partial class HeaderActionsPanel : UserControl
    {
        public HeaderActionsPanel()
        {
            InitializeComponent();

            Loaded +=
                HeaderActionsPanel_Loaded;

            Unloaded +=
                HeaderActionsPanel_Unloaded;

            LocalizationService.LanguageChanged +=
                LocalizationService_LanguageChanged;
        }

        private void HeaderActionsPanel_Loaded(
            object sender,
            RoutedEventArgs e)
        {
            RefreshButtonStates();
        }

        private void HeaderActionsPanel_Unloaded(
            object sender,
            RoutedEventArgs e)
        {
            LocalizationService.LanguageChanged -=
                LocalizationService_LanguageChanged;
        }

        private void LocalizationService_LanguageChanged(
            object sender,
            EventArgs e)
        {
            RefreshButtonStates();
        }

        private void SettingsButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            try
            {
                SettingsWindow settingsWindow =
                    new SettingsWindow();

                Window owner =
                    Window.GetWindow(this);

                if (owner != null)
                {
                    settingsWindow.Owner =
                        owner;
                }

                settingsWindow.ShowDialog();

                RefreshButtonStates();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    LocalizationService.Format(
                        "Vm_OpenSettingsFailed",
                        ex.Message),
                    "ConcentrationPro",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
        }

        private void LanguageButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            LocalizationService.ToggleLanguage();

            RefreshButtonStates();
        }

        private void ThemeButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            ThemeService.ToggleTheme();

            RefreshButtonStates();
        }

        private void RefreshButtonStates()
        {
            if (LanguageText != null)
            {
                LanguageText.Text =
                    LocalizationService.GetNextLanguageShortName();
            }

            if (ThemeIcon != null)
            {
                ThemeIcon.Kind =
                    ThemeService.CurrentTheme == AppTheme.Dark
                        ? PackIconMaterialKind.WhiteBalanceSunny
                        : PackIconMaterialKind.WeatherNight;
            }
        }
    }
}
