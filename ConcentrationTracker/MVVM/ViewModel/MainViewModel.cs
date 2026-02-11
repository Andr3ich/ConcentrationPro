using System.Windows.Input;

namespace ConcentrationTracker.MVVM.ViewModel
{
    public class MainViewModel : BaseViewModel
    {
        private int _totalSwitches;
        public int TotalSwitches
        {
            get => _totalSwitches;
            set
            {
                _totalSwitches = value;
                OnPropertyChanged();
            }
        }

        private double _avgSwitchesPerHour;
        public double AvgSwitchesPerHour
        {
            get => _avgSwitchesPerHour;
            set
            {
                _avgSwitchesPerHour = value;
                OnPropertyChanged();
            }
        }

        private string _focusStreak;
        public string FocusStreak
        {
            get => _focusStreak;
            set
            {
                _focusStreak = value;
                OnPropertyChanged();
            }
        }

        private string _productiveTime;
        public string ProductiveTime
        {
            get => _productiveTime;
            set
            {
                _productiveTime = value;
                OnPropertyChanged();
            }
        }

        public ICommand IncreaseSwitchesCommand { get; set; }

        public MainViewModel()
        {
            TotalSwitches = 144;
            AvgSwitchesPerHour = 27.4;
            FocusStreak = "46 min";
            ProductiveTime = "81%";

            IncreaseSwitchesCommand = new RelayCommand(o =>
            {
                TotalSwitches += 1;
            });
        }

    }
}
