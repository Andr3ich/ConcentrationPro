using System;
using System.Windows;
using System.Windows.Input;

namespace ConcentrationTracker.MVVM.View.Windows
{
    public partial class TrayFlyoutWindow : Window
    {
        public Action OpenDashboardRequested;
        public Action OpenSettingsRequested;
        public Action StartMonitoringRequested;
        public Action PauseMonitoringRequested;
        public Action ResetSessionRequested;
        public Action ToggleStartWithWindowsRequested;
        public Action ExitRequested;

        public TrayFlyoutWindow()
        {
            InitializeComponent();
        }

        public void SetStatusText(string text)
        {
            StatusText.Text = text;
        }

        public void SetSessionTimeText(string text)
        {
            SessionTimeText.Text = text;
        }

        public void SetScoreText(string text)
        {
            ScoreText.Text = text;
        }

        public void SetTrackedTimeText(string text)
        {
            TrackedTimeText.Text = text;
        }

        public void SetFocusStreakText(string text)
        {
            FocusStreakText.Text = text;
        }

        public void SetProductiveTimeText(string text)
        {
            ProductiveTimeText.Text = text;
        }

        public void SetOpenDashboardText(string text)
        {
            OpenDashboardButtonText.Text = text;
        }

        public void SetSettingsText(string text)
        {
            SettingsButtonText.Text = text;
        }

        public void SetStartMonitoringText(string text)
        {
            StartMonitoringButtonText.Text = text;
        }

        public void SetPauseMonitoringText(string text)
        {
            PauseMonitoringButtonText.Text = text;
        }

        public void SetResetSessionText(string text)
        {
            ResetSessionButtonText.Text = text;
        }

        public void SetStartWithWindowsText(string text)
        {
            StartWithWindowsButtonText.Text = text;
        }

        public void SetExitText(string text)
        {
            ExitButtonText.Text = text;
        }

        public void SetStartMonitoringEnabled(bool isEnabled)
        {
            StartMonitoringButton.IsEnabled = isEnabled;
        }

        public void SetPauseMonitoringEnabled(bool isEnabled)
        {
            PauseMonitoringButton.IsEnabled = isEnabled;
        }

        public void SetStartWithWindowsChecked(bool isChecked)
        {
            StartWithWindowsCheckIcon.Visibility =
                isChecked
                    ? Visibility.Visible
                    : Visibility.Collapsed;
        }

        public void ShowNear(double left, double top)
        {
            Left = left;
            Top = top;

            Show();
            Activate();
        }

        private void TrayFlyoutWindow_Deactivated(
            object sender,
            EventArgs e)
        {
            Hide();
        }

        private void TrayFlyoutWindow_KeyDown(
            object sender,
            KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                Hide();
            }
        }

        private void OpenDashboardButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            Hide();

            OpenDashboardRequested?.Invoke();
        }

        private void SettingsButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            Hide();

            OpenSettingsRequested?.Invoke();
        }

        private void StartMonitoringButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            Hide();

            StartMonitoringRequested?.Invoke();
        }

        private void PauseMonitoringButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            Hide();

            PauseMonitoringRequested?.Invoke();
        }

        private void ResetSessionButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            Hide();

            ResetSessionRequested?.Invoke();
        }

        private void StartWithWindowsButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            Hide();

            ToggleStartWithWindowsRequested?.Invoke();
        }

        private void ExitButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            Hide();

            ExitRequested?.Invoke();
        }
    }
}