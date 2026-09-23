using ConcentrationTracker.Core.Services;
using ConcentrationTracker.MVVM.Model;
using ConcentrationTracker.MVVM.View.Windows;
using ConcentrationTracker.MVVM.ViewModel;
using MahApps.Metro.Controls;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Resources;
using System.Windows.Threading;
using Drawing = System.Drawing;
using WinForms = System.Windows.Forms;
using WpfApplication = System.Windows.Application;

namespace ConcentrationTracker
{
    public partial class MainWindow : MetroWindow
    {
        private WinForms.NotifyIcon _trayIcon;
        private TrayFlyoutWindow _trayFlyout;

        private DispatcherTimer _trayUpdateTimer;

        private bool _allowRealClose;
        private bool _hasShownTrayBalloon;

        public MainWindow()
        {
            InitializeComponent();

            DataContext =
                new MainViewModel();

            LocalizationService.LanguageChanged +=
                LocalizationService_LanguageChanged;

            InitializeTrayIcon();

            Loaded +=
                MainWindow_Loaded;

            StateChanged +=
                MainWindow_StateChanged;

            Closing +=
                MainWindow_Closing;
        }

        private MainViewModel ViewModel
        {
            get
            {
                return DataContext as MainViewModel;
            }
        }

        private void LocalizationService_LanguageChanged(
            object sender,
            EventArgs e)
        {
            UpdateTrayStaticText();
            UpdateTrayState();
        }

        private void MainWindow_Loaded(
            object sender,
            RoutedEventArgs e)
        {
            if (ShouldStartMinimized())
            {
                Dispatcher.BeginInvoke(
                    new Action(HideToTray),
                    DispatcherPriority.ApplicationIdle);
            }
        }

        private bool ShouldStartMinimized()
        {
            try
            {
                bool startedWithArgument =
                    Environment
                        .GetCommandLineArgs()
                        .Any(x =>
                            x.Equals(
                                "--minimized",
                                StringComparison.OrdinalIgnoreCase));

                if (startedWithArgument)
                    return true;

                AppSettingsModel settings =
                    AppSettingsService.LoadSettings();

                return settings.StartMinimizedToTray;
            }
            catch
            {
                return false;
            }
        }

        private void InitializeTrayIcon()
        {
            _trayFlyout =
                new TrayFlyoutWindow();

            _trayFlyout.OpenDashboardRequested =
                ShowMainWindow;

            _trayFlyout.OpenSettingsRequested =
                OpenSettingsWindowFromTray;

            _trayFlyout.StartMonitoringRequested =
                StartMonitoringFromTray;

            _trayFlyout.PauseMonitoringRequested =
                PauseMonitoringFromTray;

            _trayFlyout.ResetSessionRequested =
                ResetSessionFromTray;

            _trayFlyout.ToggleStartWithWindowsRequested =
                ToggleStartWithWindowsFromTray;

            _trayFlyout.ExitRequested =
                ExitApplicationFromTray;

            _trayIcon =
                new WinForms.NotifyIcon();

            _trayIcon.Text =
                LocalizationService.GetString(
                    "App_Title");

            _trayIcon.Icon =
                GetApplicationIcon();

            _trayIcon.Visible =
                true;

            _trayIcon.MouseDoubleClick +=
                TrayIcon_MouseDoubleClick;

            _trayIcon.MouseUp +=
                TrayIcon_MouseUp;

            _trayUpdateTimer =
                new DispatcherTimer();

            _trayUpdateTimer.Interval =
                TimeSpan.FromSeconds(2);

            _trayUpdateTimer.Tick +=
                (sender, args) =>
                {
                    UpdateTrayState();
                };

            _trayUpdateTimer.Start();

            UpdateTrayStaticText();
            UpdateTrayState();
        }

        private void UpdateTrayStaticText()
        {
            if (_trayFlyout == null)
                return;

            _trayFlyout.SetOpenDashboardText(
                LocalizationService.GetString(
                    "Tray_OpenDashboard"));

            _trayFlyout.SetSettingsText(
                LocalizationService.GetString(
                    "Common_Settings"));

            _trayFlyout.SetStartMonitoringText(
                LocalizationService.GetString(
                    "Monitoring_Start"));

            _trayFlyout.SetPauseMonitoringText(
                LocalizationService.GetString(
                    "Monitoring_Pause"));

            _trayFlyout.SetResetSessionText(
                LocalizationService.GetString(
                    "Monitoring_Reset"));

            _trayFlyout.SetStartWithWindowsText(
                LocalizationService.GetString(
                    "Tray_StartWithWindows"));

            _trayFlyout.SetExitText(
                LocalizationService.GetString(
                    "Tray_Exit"));
        }

        private Drawing.Icon GetApplicationIcon()
        {
            try
            {
                Uri iconUri =
                    new Uri(
                        "pack://application:,,,/Resources/Brand/AppIcon.ico",
                        UriKind.Absolute);

                StreamResourceInfo resourceInfo =
                    WpfApplication.GetResourceStream(
                        iconUri);

                if (resourceInfo != null &&
                    resourceInfo.Stream != null)
                {
                    using (resourceInfo.Stream)
                    {
                        Drawing.Icon icon =
                            new Drawing.Icon(
                                resourceInfo.Stream);

                        return (Drawing.Icon)icon.Clone();
                    }
                }
            }
            catch
            {
            }

            try
            {
                Process currentProcess =
                    Process.GetCurrentProcess();

                if (currentProcess.MainModule != null &&
                    !string.IsNullOrWhiteSpace(currentProcess.MainModule.FileName))
                {
                    Drawing.Icon icon =
                        Drawing.Icon.ExtractAssociatedIcon(
                            currentProcess.MainModule.FileName);

                    if (icon != null)
                        return icon;
                }
            }
            catch
            {
            }

            return Drawing.SystemIcons.Application;
        }

        private void TrayIcon_MouseDoubleClick(
            object sender,
            WinForms.MouseEventArgs e)
        {
            if (e.Button == WinForms.MouseButtons.Left)
            {
                Dispatcher.Invoke(ShowMainWindow);
            }
        }

        private void TrayIcon_MouseUp(
            object sender,
            WinForms.MouseEventArgs e)
        {
            if (e.Button == WinForms.MouseButtons.Right)
            {
                Dispatcher.Invoke(ShowTrayFlyout);
            }
        }

        private Point GetTrayFlyoutAnchorPoint()
        {
            Drawing.Point cursor =
                WinForms.Cursor.Position;

            Point point =
                new Point(cursor.X, cursor.Y);

            PresentationSource source =
                PresentationSource.FromVisual(this);

            if (source != null &&
                source.CompositionTarget != null)
            {
                point =
                    source.CompositionTarget.TransformFromDevice.Transform(point);
            }

            return point;
        }

        private void ShowTrayFlyout()
        {
            if (_trayFlyout == null)
                return;

            UpdateTrayStaticText();
            UpdateTrayState();

            Point anchor =
                GetTrayFlyoutAnchorPoint();

            _trayFlyout.Opacity =
                0;

            _trayFlyout.Left =
                anchor.X;

            _trayFlyout.Top =
                anchor.Y;

            _trayFlyout.Show();
            _trayFlyout.UpdateLayout();

            double flyoutWidth =
                _trayFlyout.ActualWidth > 0
                    ? _trayFlyout.ActualWidth
                    : _trayFlyout.Width;

            double flyoutHeight =
                _trayFlyout.ActualHeight;

            Rect workArea =
                SystemParameters.WorkArea;

            double left =
                anchor.X - flyoutWidth;

            double top =
                anchor.Y - flyoutHeight;

            if (left < workArea.Left)
                left = workArea.Left + 8;

            if (left + flyoutWidth > workArea.Right)
                left = workArea.Right - flyoutWidth - 8;

            if (top < workArea.Top)
                top = anchor.Y;

            if (top + flyoutHeight > workArea.Bottom)
                top = workArea.Bottom - flyoutHeight - 8;

            _trayFlyout.Left =
                left;

            _trayFlyout.Top =
                top;

            _trayFlyout.Opacity =
                1;

            _trayFlyout.Activate();
        }

        private void MainWindow_StateChanged(
            object sender,
            EventArgs e)
        {
            if (WindowState == WindowState.Minimized &&
                ShouldMinimizeToTray())
            {
                HideToTray();
            }
        }

        private void MainWindow_Closing(
            object sender,
            CancelEventArgs e)
        {
            if (_allowRealClose)
            {
                DisposeTrayResources();
                return;
            }

            if (ShouldMinimizeToTray())
            {
                e.Cancel =
                    true;

                HideToTray();

                return;
            }

            PauseMonitoringBeforeRealClose();

            _allowRealClose =
                true;

            DisposeTrayResources();
        }

        private void HideToTray()
        {
            Hide();

            if (!_hasShownTrayBalloon &&
                ShouldShowTrayNotification())
            {
                _hasShownTrayBalloon =
                    true;

                try
                {
                    _trayIcon.BalloonTipTitle =
                        LocalizationService.GetString(
                            "Tray_BalloonStillRunningTitle");

                    _trayIcon.BalloonTipText =
                        LocalizationService.GetString(
                            "Tray_BalloonStillRunningText");

                    _trayIcon.BalloonTipIcon =
                        WinForms.ToolTipIcon.Info;

                    _trayIcon.ShowBalloonTip(2500);
                }
                catch
                {
                }
            }
        }

        private void ShowMainWindow()
        {
            Show();

            if (WindowState == WindowState.Minimized)
            {
                WindowState =
                    WindowState.Normal;
            }

            Activate();

            Topmost =
                true;

            Topmost =
                false;
        }

        private void StartMonitoringFromTray()
        {
            MainViewModel viewModel =
                ViewModel;

            if (viewModel == null ||
                viewModel.StartMonitoringCommand == null)
            {
                return;
            }

            if (viewModel.StartMonitoringCommand.CanExecute(null))
            {
                viewModel.StartMonitoringCommand.Execute(null);
            }

            UpdateTrayState();
        }

        private void PauseMonitoringFromTray()
        {
            MainViewModel viewModel =
                ViewModel;

            if (viewModel == null ||
                viewModel.PauseMonitoringCommand == null)
            {
                return;
            }

            if (viewModel.PauseMonitoringCommand.CanExecute(null))
            {
                viewModel.PauseMonitoringCommand.Execute(null);
            }

            UpdateTrayState();
        }

        private void ResetSessionFromTray()
        {
            MainViewModel viewModel =
                ViewModel;

            if (viewModel == null ||
                viewModel.ResetSessionCommand == null)
            {
                return;
            }

            if (viewModel.ResetSessionCommand.CanExecute(null))
            {
                viewModel.ResetSessionCommand.Execute(null);
            }

            UpdateTrayState();
        }

        private void ToggleStartWithWindowsFromTray()
        {
            bool currentlyEnabled =
                AppStartupService.IsStartWithWindowsEnabled();

            bool targetValue =
                !currentlyEnabled;

            bool success =
                AppStartupService.SetStartWithWindowsEnabled(
                    targetValue);

            AppSettingsModel settings =
                AppSettingsService.LoadSettings();

            settings.StartWithWindows =
                success
                    ? targetValue
                    : currentlyEnabled;

            AppSettingsService.SaveSettings(
                settings);

            UpdateTrayState();

            try
            {
                _trayIcon.BalloonTipTitle =
                    LocalizationService.GetString(
                        "App_Title");

                _trayIcon.BalloonTipText =
                    success
                        ? targetValue
                            ? LocalizationService.GetString(
                                "Tray_StartupEnabledText")
                            : LocalizationService.GetString(
                                "Tray_StartupDisabledText")
                        : LocalizationService.GetString(
                            "Tray_StartupChangeFailedText");

                _trayIcon.BalloonTipIcon =
                    success
                        ? WinForms.ToolTipIcon.Info
                        : WinForms.ToolTipIcon.Warning;

                _trayIcon.ShowBalloonTip(2200);
            }
            catch
            {
            }
        }

        private void OpenSettingsWindowFromTray()
        {
            SettingsWindow settingsWindow =
                new SettingsWindow();

            if (IsVisible)
            {
                settingsWindow.Owner =
                    this;
            }

            bool? result =
                settingsWindow.ShowDialog();

            UpdateTrayState();

            if (result == true)
            {
                AppSettingsModel settings =
                    AppSettingsService.LoadSettings();

                if (settings.StartMinimizedToTray &&
                    settings.MinimizeToTray)
                {

                }
            }
        }

        private bool ShouldMinimizeToTray()
        {
            try
            {
                AppSettingsModel settings =
                    AppSettingsService.LoadSettings();

                return settings.MinimizeToTray;
            }
            catch
            {
                return true;
            }
        }

        private bool ShouldShowTrayNotification()
        {
            try
            {
                AppSettingsModel settings =
                    AppSettingsService.LoadSettings();

                return settings.ShowTrayNotification;
            }
            catch
            {
                return true;
            }
        }

        private void PauseMonitoringBeforeRealClose()
        {
            MainViewModel viewModel =
                ViewModel;

            if (viewModel != null &&
                viewModel.IsMonitoring &&
                viewModel.PauseMonitoringCommand != null &&
                viewModel.PauseMonitoringCommand.CanExecute(null))
            {
                viewModel.PauseMonitoringCommand.Execute(null);
            }
        }

        private void ExitApplicationFromTray()
        {
            MainViewModel viewModel =
                ViewModel;

            if (viewModel != null &&
                viewModel.IsMonitoring &&
                viewModel.PauseMonitoringCommand != null &&
                viewModel.PauseMonitoringCommand.CanExecute(null))
            {
                viewModel.PauseMonitoringCommand.Execute(null);
            }

            _allowRealClose =
                true;

            DisposeTrayResources();

            Close();

            WpfApplication.Current.Shutdown();
        }

        private void UpdateTrayState()
        {
            MainViewModel viewModel =
                ViewModel;

            if (viewModel == null)
            {
                SetTrayText(
                    LocalizationService.GetString(
                        "App_Title"));

                return;
            }

            string status;

            if (viewModel.IsMonitoring && viewModel.IsViewingOwnApp)
            {
                status =
                    LocalizationService.GetString(
                        "Monitoring_StatusViewingDashboard");
            }
            else if (viewModel.IsMonitoring)
            {
                status =
                    LocalizationService.GetString(
                        "Monitoring_StatusActive");
            }
            else
            {
                status =
                    LocalizationService.GetString(
                        "Monitoring_StatusPaused");
            }

            if (_trayFlyout != null)
            {
                _trayFlyout.SetStatusText(
                    LocalizationService.Format(
                        "Tray_StatusFormat",
                        status));

                _trayFlyout.SetSessionTimeText(
                    LocalizationService.Format(
                        "Tray_SessionTimeFormat",
                        viewModel.SessionTime));

                _trayFlyout.SetScoreText(
                    LocalizationService.Format(
                        "Tray_ScoreFormat",
                        viewModel.ConcentrationScore));

                _trayFlyout.SetTrackedTimeText(
                    LocalizationService.Format(
                        "Tray_TrackedTimeFormat",
                        viewModel.TrackedTime));

                _trayFlyout.SetFocusStreakText(
                    LocalizationService.Format(
                        "Tray_FocusStreakFormat",
                        viewModel.FocusStreak));

                _trayFlyout.SetProductiveTimeText(
                    LocalizationService.Format(
                        "Tray_ProductiveTimeFormat",
                        viewModel.ProductiveTime));

                _trayFlyout.SetStartMonitoringEnabled(
                    !viewModel.IsMonitoring);

                _trayFlyout.SetPauseMonitoringEnabled(
                    viewModel.IsMonitoring);

                _trayFlyout.SetStartWithWindowsChecked(
                    AppStartupService.IsStartWithWindowsEnabled());
            }

            string compactStatus =
                viewModel.IsMonitoring
                    ? LocalizationService.GetString(
                        "Tray_ActiveShort")
                    : LocalizationService.GetString(
                        "Tray_PausedShort");

            string trayText =
                LocalizationService.Format(
                    "Tray_TooltipFormat",
                    compactStatus,
                    viewModel.ConcentrationScore);

            SetTrayText(trayText);
        }

        private void SetTrayText(
            string text)
        {
            if (_trayIcon == null)
                return;

            if (string.IsNullOrWhiteSpace(text))
            {
                _trayIcon.Text =
                    LocalizationService.GetString(
                        "App_Title");

                return;
            }

            if (text.Length > 63)
            {
                text =
                    text.Substring(0, 60) + "...";
            }

            try
            {
                _trayIcon.Text =
                    text;
            }
            catch
            {
                _trayIcon.Text =
                    LocalizationService.GetString(
                        "App_Title");
            }
        }

        private void DisposeTrayResources()
        {
            LocalizationService.LanguageChanged -=
                LocalizationService_LanguageChanged;

            if (_trayUpdateTimer != null)
            {
                _trayUpdateTimer.Stop();
                _trayUpdateTimer = null;
            }

            if (_trayIcon != null)
            {
                _trayIcon.Visible =
                    false;

                _trayIcon.MouseDoubleClick -=
                    TrayIcon_MouseDoubleClick;

                _trayIcon.MouseUp -=
                    TrayIcon_MouseUp;

                _trayIcon.Dispose();
                _trayIcon = null;
            }

            if (_trayFlyout != null)
            {
                _trayFlyout.Close();
                _trayFlyout = null;
            }
        }
    }
}