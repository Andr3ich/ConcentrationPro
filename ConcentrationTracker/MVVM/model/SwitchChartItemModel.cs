using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ConcentrationTracker.MVVM.Model
{
    public class SwitchChartItemModel : INotifyPropertyChanged
    {
        private string _label;
        public string Label
        {
            get => _label;
            set
            {
                _label = value;
                OnPropertyChanged();
            }
        }

        private int _switchCount;
        public int SwitchCount
        {
            get => _switchCount;
            set
            {
                _switchCount = value;
                OnPropertyChanged();
            }
        }

        private double _barHeight;
        public double BarHeight
        {
            get => _barHeight;
            set
            {
                _barHeight = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(
            [CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(
                this,
                new PropertyChangedEventArgs(propertyName));
        }
    }
}
