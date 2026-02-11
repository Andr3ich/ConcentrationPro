using System.Windows;
using ConcentrationTracker.MVVM.ViewModel;

namespace ConcentrationTracker
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            DataContext = new MainViewModel();
        }
    }
}
