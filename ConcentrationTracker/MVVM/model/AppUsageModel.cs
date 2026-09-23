using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ConcentrationTracker.MVVM.Model
{
    public class AppUsageModel : INotifyPropertyChanged
    {
        private string _appName;
        public string AppName
        {
            get => _appName;
            set
            {
                _appName = value;
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

        private AppCategory _category;
        public AppCategory Category
        {
            get => _category;
            set
            {
                _category = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CategoryText));
            }
        }

        public string CategoryText
        {
            get
            {
                return Category.ToString();
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
