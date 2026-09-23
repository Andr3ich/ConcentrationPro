using ConcentrationTracker.Core.Services;
using ConcentrationTracker.MVVM.Model;
using ConcentrationTracker.MVVM.ViewModel;
using System;
using System.Windows;

namespace ConcentrationTracker.MVVM.View.Windows
{
    public partial class SessionDetailsWindow : Window
    {
        private SessionDetailsViewModel _viewModel;

        public SessionDetailsWindow()
        {
            InitializeComponent();
        }

        public SessionDetailsWindow(
            SessionRecordModel session)
        {
            InitializeComponent();

            SessionDetailsReadService detailsReadService =
                new SessionDetailsReadService();

            SessionDetailsModel details =
                detailsReadService.LoadDetails(
                    session != null ? session.SessionId : string.Empty);

            _viewModel =
                new SessionDetailsViewModel(
                    session,
                    details);

            DataContext =
                _viewModel;
        }

        public SessionDetailsWindow(
            SessionDetailsViewModel viewModel)
        {
            InitializeComponent();

            _viewModel =
                viewModel;

            DataContext =
                viewModel;
        }

        protected override void OnClosed(
            EventArgs e)
        {
            if (_viewModel != null)
                _viewModel.Detach();

            base.OnClosed(e);
        }
    }
}
