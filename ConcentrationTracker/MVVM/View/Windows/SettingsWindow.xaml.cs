using ConcentrationTracker.MVVM.ViewModel;
using System;
using System.Windows;

namespace ConcentrationTracker.MVVM.View.Windows
{
    public partial class SettingsWindow : Window
    {
        private readonly SettingsViewModel _viewModel;

        public SettingsWindow()
        {
            InitializeComponent();

            _viewModel =
                new SettingsViewModel();

            DataContext =
                _viewModel;

            _viewModel.CloseRequested +=
                ViewModel_CloseRequested;
        }

        private void ViewModel_CloseRequested(
            object sender,
            bool? dialogResult)
        {
            DialogResult =
                dialogResult;

            Close();
        }

        protected override void OnClosed(
            EventArgs e)
        {
            _viewModel.CloseRequested -=
                ViewModel_CloseRequested;

            _viewModel.Detach();

            base.OnClosed(e);
        }
    }
}