using MahApps.Metro.IconPacks;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace ConcentrationTracker.MVVM.View.Controls
{
    public partial class WindowCaptionBar : UserControl
    {
        public WindowCaptionBar()
        {
            InitializeComponent();

            Loaded +=
                WindowCaptionBar_Loaded;
        }

        private void WindowCaptionBar_Loaded(
            object sender,
            RoutedEventArgs e)
        {
            Window window =
                Window.GetWindow(this);

            if (window != null)
            {
                window.StateChanged +=
                    Window_StateChanged;
            }

            UpdateMaximizeRestoreIcon();
        }

        private void Window_StateChanged(
            object sender,
            EventArgs e)
        {
            UpdateMaximizeRestoreIcon();
        }

        private void CaptionRoot_MouseLeftButtonDown(
            object sender,
            MouseButtonEventArgs e)
        {
            if (IsInsideButton(e.OriginalSource as DependencyObject))
                return;

            Window window =
                Window.GetWindow(this);

            if (window == null)
                return;

            if (e.ClickCount == 2)
            {
                ToggleMaximizeRestore();
                return;
            }

            try
            {
                window.DragMove();
            }
            catch
            {

            }
        }

        private void MinimizeButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            Window window =
                Window.GetWindow(this);

            if (window != null)
            {
                window.WindowState =
                    WindowState.Minimized;
            }
        }

        private void MaximizeRestoreButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            ToggleMaximizeRestore();
        }

        private void CloseButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            Window window =
                Window.GetWindow(this);

            if (window != null)
            {
                window.Close();
            }
        }

        private void ToggleMaximizeRestore()
        {
            Window window =
                Window.GetWindow(this);

            if (window == null)
                return;

            window.WindowState =
                window.WindowState == WindowState.Maximized
                    ? WindowState.Normal
                    : WindowState.Maximized;

            UpdateMaximizeRestoreIcon();
        }

        private void UpdateMaximizeRestoreIcon()
        {
            Window window =
                Window.GetWindow(this);

            if (window == null ||
                MaximizeRestoreIcon == null)
            {
                return;
            }

            MaximizeRestoreIcon.Kind =
                window.WindowState == WindowState.Maximized
                    ? PackIconMaterialKind.CheckboxMultipleBlankOutline
                    : PackIconMaterialKind.CheckboxBlankOutline;
        }

        private bool IsInsideButton(
            DependencyObject source)
        {
            DependencyObject current =
                source;

            while (current != null)
            {
                if (current is Button)
                    return true;

                current =
                    VisualTreeHelper.GetParent(current);
            }

            return false;
        }
    }
}
