using ConcentrationTracker.Core.Database;
using ConcentrationTracker.Core.Services;
using ConcentrationTracker.MVVM.Model;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows.Input;

namespace ConcentrationTracker.MVVM.ViewModel
{
    public class SettingsViewModel : BaseViewModel
    {
        private AppSettingsModel _settings;

        private bool _startWithWindows;
        private bool _startMinimizedToTray;
        private bool _minimizeToTray;
        private bool _showTrayNotification;
        private string _idleThresholdText;
        private string _pendingIdleGraceText;
        private List<DashboardTabOption> _dashboardTabOptions;
        private DashboardTabOption _selectedDashboardTabOption;
        private string _databasePath;
        private string _statusText;

        public event EventHandler<bool?> CloseRequested;

        public SettingsViewModel()
        {
            SaveCommand =
                new RelayCommand(_ => Save());

            CancelCommand =
                new RelayCommand(_ => Cancel());

            ResetDefaultsCommand =
                new RelayCommand(_ => ResetDefaults());

            OpenDataFolderCommand =
                new RelayCommand(_ => OpenDataFolder());

            LocalizationService.LanguageChanged +=
                LocalizationService_LanguageChanged;

            LoadSettingsIntoViewModel();
            LoadDatabaseInfo();
        }

        public bool StartWithWindows
        {
            get { return _startWithWindows; }
            set
            {
                if (_startWithWindows != value)
                {
                    _startWithWindows = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool StartMinimizedToTray
        {
            get { return _startMinimizedToTray; }
            set
            {
                if (_startMinimizedToTray != value)
                {
                    _startMinimizedToTray = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool MinimizeToTray
        {
            get { return _minimizeToTray; }
            set
            {
                if (_minimizeToTray != value)
                {
                    _minimizeToTray = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool ShowTrayNotification
        {
            get { return _showTrayNotification; }
            set
            {
                if (_showTrayNotification != value)
                {
                    _showTrayNotification = value;
                    OnPropertyChanged();
                }
            }
        }

        public string IdleThresholdText
        {
            get { return _idleThresholdText; }
            set
            {
                if (_idleThresholdText != value)
                {
                    _idleThresholdText = value;
                    OnPropertyChanged();
                }
            }
        }

        public string PendingIdleGraceText
        {
            get { return _pendingIdleGraceText; }
            set
            {
                if (_pendingIdleGraceText != value)
                {
                    _pendingIdleGraceText = value;
                    OnPropertyChanged();
                }
            }
        }

        public List<DashboardTabOption> DashboardTabOptions
        {
            get { return _dashboardTabOptions; }
            private set
            {
                _dashboardTabOptions = value;
                OnPropertyChanged();
            }
        }

        public DashboardTabOption SelectedDashboardTabOption
        {
            get { return _selectedDashboardTabOption; }
            set
            {
                if (_selectedDashboardTabOption != value)
                {
                    _selectedDashboardTabOption = value;
                    OnPropertyChanged();
                }
            }
        }

        public string DatabasePath
        {
            get { return _databasePath; }
            private set
            {
                _databasePath = value;
                OnPropertyChanged();
            }
        }

        public string StatusText
        {
            get { return _statusText; }
            set
            {
                _statusText = value;
                OnPropertyChanged();
            }
        }

        public ICommand SaveCommand { get; private set; }
        public ICommand CancelCommand { get; private set; }
        public ICommand ResetDefaultsCommand { get; private set; }
        public ICommand OpenDataFolderCommand { get; private set; }

        // Call this from the View's OnClosed override so the ViewModel
        // does not keep a static event subscription alive after the
        // window is gone (avoids a memory leak / leaked handler).
        public void Detach()
        {
            LocalizationService.LanguageChanged -=
                LocalizationService_LanguageChanged;
        }

        private void LocalizationService_LanguageChanged(
            object sender,
            EventArgs e)
        {
            DashboardMode selectedMode =
                GetSelectedDashboardMode();

            LoadDashboardTabOptions(selectedMode);

            StatusText =
                LocalizationService.GetString("Settings_StatusStored");
        }

        private void LoadSettingsIntoViewModel()
        {
            _settings =
                AppSettingsService.LoadSettings();

            _settings.StartWithWindows =
                AppStartupService.IsStartWithWindowsEnabled();

            ApplySettingsToProperties(
                _settings);

            StatusText =
                LocalizationService.GetString("Settings_StatusStored");
        }

        private void ApplySettingsToProperties(
            AppSettingsModel settings)
        {
            if (settings == null)
                settings = new AppSettingsModel();

            StartWithWindows =
                settings.StartWithWindows;

            StartMinimizedToTray =
                settings.StartMinimizedToTray;

            MinimizeToTray =
                settings.MinimizeToTray;

            ShowTrayNotification =
                settings.ShowTrayNotification;

            IdleThresholdText =
                settings.IdleThresholdMinutes.ToString(
                    "0.##",
                    CultureInfo.InvariantCulture);

            PendingIdleGraceText =
                settings.PendingIdleGraceSeconds.ToString(
                    CultureInfo.InvariantCulture);

            LoadDashboardTabOptions(
                settings.DefaultDashboardTab);
        }

        private void LoadDashboardTabOptions(
            DashboardMode selectedMode)
        {
            List<DashboardTabOption> options =
                new List<DashboardTabOption>
                {
                    new DashboardTabOption(DashboardMode.Charts, LocalizationService.GetString("Tab_Charts")),
                    new DashboardTabOption(DashboardMode.Timeline, LocalizationService.GetString("Tab_Timeline")),
                    new DashboardTabOption(DashboardMode.Summary, LocalizationService.GetString("Tab_Summary")),
                    new DashboardTabOption(DashboardMode.Categories, LocalizationService.GetString("Tab_Categories")),
                    new DashboardTabOption(DashboardMode.History, LocalizationService.GetString("Tab_History"))
                };

            DashboardTabOptions =
                options;

            SelectedDashboardTabOption =
                options.FirstOrDefault(x => x.Mode == selectedMode) ?? options.First();
        }

        private DashboardMode GetSelectedDashboardMode()
        {
            if (SelectedDashboardTabOption != null)
                return SelectedDashboardTabOption.Mode;

            if (_settings != null)
                return _settings.DefaultDashboardTab;

            return DashboardMode.Charts;
        }

        private void LoadDatabaseInfo()
        {
            try
            {
                DatabasePath =
                    DatabaseConnectionFactory.GetDatabasePath();
            }
            catch
            {
                DatabasePath =
                    LocalizationService.GetString("Settings_DatabaseUnavailable");
            }
        }

        private void Save()
        {
            if (_settings == null)
            {
                _settings =
                    new AppSettingsModel();
            }

            double idleThresholdMinutes;

            if (!TryParseDouble(
                    IdleThresholdText,
                    out idleThresholdMinutes))
            {
                StatusText =
                    LocalizationService.GetString("Settings_InvalidIdleThreshold");

                return;
            }

            if (idleThresholdMinutes < 0.25 ||
                idleThresholdMinutes > 60)
            {
                StatusText =
                    LocalizationService.GetString("Settings_IdleThresholdRange");

                return;
            }

            int pendingIdleGraceSeconds;

            if (!int.TryParse(
                    PendingIdleGraceText,
                    NumberStyles.Integer,
                    CultureInfo.InvariantCulture,
                    out pendingIdleGraceSeconds))
            {
                StatusText =
                    LocalizationService.GetString("Settings_InvalidIdleGrace");

                return;
            }

            if (pendingIdleGraceSeconds < 0 ||
                pendingIdleGraceSeconds > 300)
            {
                StatusText =
                    LocalizationService.GetString("Settings_IdleGraceRange");

                return;
            }

            _settings.StartWithWindows =
                StartWithWindows;

            _settings.StartMinimizedToTray =
                StartMinimizedToTray;

            _settings.MinimizeToTray =
                MinimizeToTray;

            _settings.ShowTrayNotification =
                ShowTrayNotification;

            _settings.IdleThresholdMinutes =
                idleThresholdMinutes;

            _settings.PendingIdleGraceSeconds =
                pendingIdleGraceSeconds;

            _settings.DefaultDashboardTab =
                GetSelectedDashboardMode();

            bool startupUpdated =
                AppStartupService.SetStartWithWindowsEnabled(
                    _settings.StartWithWindows);

            AppSettingsService.SaveSettings(
                _settings);

            StatusText =
                startupUpdated
                    ? LocalizationService.GetString("Settings_Saved")
                    : LocalizationService.GetString("Settings_SavedStartupFailed");

            RaiseCloseRequested(true);
        }

        private void ResetDefaults()
        {
            _settings =
                new AppSettingsModel();

            ApplySettingsToProperties(
                _settings);

            StatusText =
                LocalizationService.GetString("Settings_DefaultsRestored");
        }

        private void OpenDataFolder()
        {
            try
            {
                string directory =
                    DatabaseConnectionFactory.GetDatabaseDirectory();

                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(
                        directory);
                }

                ProcessStartInfo startInfo =
                    new ProcessStartInfo
                    {
                        FileName =
                            directory,

                        UseShellExecute =
                            true
                    };

                Process.Start(
                    startInfo);
            }
            catch (Exception ex)
            {
                StatusText =
                    LocalizationService.Format(
                        "Settings_OpenFolderFailed",
                        ex.Message);
            }
        }

        private void Cancel()
        {
            RaiseCloseRequested(false);
        }

        private void RaiseCloseRequested(bool? dialogResult)
        {
            EventHandler<bool?> handler =
                CloseRequested;

            if (handler != null)
                handler(this, dialogResult);
        }

        private bool TryParseDouble(
            string value,
            out double result)
        {
            string normalized =
                (value ?? string.Empty)
                    .Trim()
                    .Replace(',', '.');

            return double.TryParse(
                normalized,
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out result);
        }

        public class DashboardTabOption
        {
            public DashboardMode Mode { get; private set; }
            public string DisplayName { get; private set; }

            public DashboardTabOption(
                DashboardMode mode,
                string displayName)
            {
                Mode =
                    mode;

                DisplayName =
                    displayName;
            }
        }
    }
}