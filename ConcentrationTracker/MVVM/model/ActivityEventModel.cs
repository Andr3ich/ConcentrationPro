using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ConcentrationTracker.MVVM.Model
{
    public class ActivityEventModel : INotifyPropertyChanged
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

        private string _processPath;
        public string ProcessPath
        {
            get => _processPath;
            set
            {
                _processPath = value;
                OnPropertyChanged();
            }
        }

        private string _appUserModelId;
        public string AppUserModelId
        {
            get => _appUserModelId;
            set
            {
                _appUserModelId = value;
                OnPropertyChanged();
            }
        }

        private string _packageFullName;
        public string PackageFullName
        {
            get => _packageFullName;
            set
            {
                _packageFullName = value;
                OnPropertyChanged();
            }
        }

        private string _packageInstallPath;
        public string PackageInstallPath
        {
            get => _packageInstallPath;
            set
            {
                _packageInstallPath = value;
                OnPropertyChanged();
            }
        }

        private string _windowTitle;
        public string WindowTitle
        {
            get => _windowTitle;
            set
            {
                _windowTitle = value;
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
            }
        }

        private DateTime _startTime;
        public DateTime StartTime
        {
            get => _startTime;
            set
            {
                _startTime = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(DurationText));
                OnPropertyChanged(nameof(TimeRangeText));
            }
        }

        private DateTime? _endTime;
        public DateTime? EndTime
        {
            get => _endTime;
            set
            {
                _endTime = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CurrentDuration));
                OnPropertyChanged(nameof(DurationText));
                OnPropertyChanged(nameof(TimeRangeText));
            }
        }

        private bool _isActive;
        public bool IsActive
        {
            get => _isActive;
            set
            {
                _isActive = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(StatusText));
                OnPropertyChanged(nameof(TimeRangeText));
            }
        }

        public TimeSpan CurrentDuration
        {
            get
            {
                DateTime effectiveEndTime = EndTime ?? DateTime.Now;
                TimeSpan duration = effectiveEndTime - StartTime;

                if (duration.TotalSeconds < 0)
                    return TimeSpan.Zero;

                return duration;
            }
        }

        public string DurationText
        {
            get
            {
                TimeSpan duration = CurrentDuration;

                if (duration.TotalSeconds < 1)
                    return "0 sec";

                int hours = (int)duration.TotalHours;
                int minutes = duration.Minutes;
                int seconds = duration.Seconds;

                if (hours > 0)
                    return $"{hours}h {minutes} min";

                if (minutes > 0 && seconds > 0)
                    return $"{minutes} min {seconds} sec";

                if (minutes > 0)
                    return $"{minutes} min";

                return $"{seconds} sec";
            }
        }

        public string StatusText
        {
            get
            {
                return IsActive
                    ? "Active now"
                    : "Completed";
            }
        }

        public string TimeRangeText
        {
            get
            {
                if (IsActive)
                    return $"{StartTime:HH:mm:ss} – now";

                if (EndTime.HasValue)
                    return $"{StartTime:HH:mm:ss} – {EndTime.Value:HH:mm:ss}";

                return $"{StartTime:HH:mm:ss}";
            }
        }

        public void RefreshDuration()
        {
            OnPropertyChanged(nameof(CurrentDuration));
            OnPropertyChanged(nameof(DurationText));
            OnPropertyChanged(nameof(TimeRangeText));
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
