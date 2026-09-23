using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ConcentrationTracker.MVVM.Model
{
    public class FocusBlockModel : INotifyPropertyChanged
    {
        private string _mainApp;
        public string MainApp
        {
            get => _mainApp;
            set
            {
                _mainApp = value;
                OnPropertyChanged();
            }
        }

        private string _lastWindowTitle;
        public string LastWindowTitle
        {
            get => _lastWindowTitle;
            set
            {
                _lastWindowTitle = value;
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
                OnPropertyChanged(nameof(StartTimeText));
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
                OnPropertyChanged(nameof(EndTimeText));
                OnPropertyChanged(nameof(DurationText));
            }
        }

        private TimeSpan _accumulatedDuration;
        public TimeSpan AccumulatedDuration
        {
            get => _accumulatedDuration;
            set
            {
                _accumulatedDuration = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CurrentDuration));
                OnPropertyChanged(nameof(DurationText));
            }
        }

        private DateTime? _currentSegmentStartTime;
        public DateTime? CurrentSegmentStartTime
        {
            get => _currentSegmentStartTime;
            set
            {
                _currentSegmentStartTime = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CurrentDuration));
                OnPropertyChanged(nameof(DurationText));
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
                OnPropertyChanged(nameof(CurrentDuration));
                OnPropertyChanged(nameof(DurationText));
            }
        }

        private bool _isSuspended;
        public bool IsSuspended
        {
            get => _isSuspended;
            set
            {
                _isSuspended = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(StatusText));
            }
        }

        private FocusBreakReason _breakReason;
        public FocusBreakReason BreakReason
        {
            get => _breakReason;
            set
            {
                _breakReason = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(BreakReasonText));
            }
        }

        public TimeSpan CurrentDuration
        {
            get
            {
                TimeSpan duration = AccumulatedDuration;

                if (IsActive && CurrentSegmentStartTime.HasValue)
                {
                    TimeSpan currentPart =
                        DateTime.Now - CurrentSegmentStartTime.Value;

                    if (currentPart.TotalSeconds > 0)
                    {
                        duration += currentPart;
                    }
                }

                return duration;
            }
        }

        public string DurationText => FormatDuration(CurrentDuration);

        public string StartTimeText => StartTime.ToString("HH:mm:ss");

        public string EndTimeText
        {
            get
            {
                if (!EndTime.HasValue)
                    return "";

                return EndTime.Value.ToString("HH:mm:ss");
            }
        }

        public string StatusText
        {
            get
            {
                if (IsActive)
                    return "Active now";

                if (IsSuspended)
                    return "Paused for dashboard";

                return "Completed";
            }
        }

        public string BreakReasonText => BreakReason.ToString();

        public void StartSegment(DateTime startTime)
        {
            IsSuspended = false;
            IsActive = true;
            CurrentSegmentStartTime = startTime;

            RefreshDuration();
        }

        public void PauseSegment(DateTime pauseTime)
        {
            if (IsActive && CurrentSegmentStartTime.HasValue)
            {
                TimeSpan segmentDuration =
                    pauseTime - CurrentSegmentStartTime.Value;

                if (segmentDuration.TotalSeconds > 0)
                {
                    AccumulatedDuration += segmentDuration;
                }
            }

            IsActive = false;
            IsSuspended = true;
            CurrentSegmentStartTime = null;

            RefreshDuration();
        }

        public void Complete(
            DateTime endTime,
            FocusBreakReason reason)
        {
            if (IsActive || CurrentSegmentStartTime.HasValue)
            {
                PauseSegment(endTime);
            }

            IsActive = false;
            IsSuspended = false;
            EndTime = endTime;
            BreakReason = reason;
            CurrentSegmentStartTime = null;

            RefreshDuration();
        }

        public void RefreshDuration()
        {
            OnPropertyChanged(nameof(CurrentDuration));
            OnPropertyChanged(nameof(DurationText));
        }

        private string FormatDuration(TimeSpan duration)
        {
            if (duration.TotalSeconds < 1)
                return "0 sec";

            int hours = (int)duration.TotalHours;
            int minutes = duration.Minutes;
            int seconds = duration.Seconds;

            List<string> parts = new List<string>();

            if (hours > 0)
                parts.Add($"{hours}h");

            if (minutes > 0)
                parts.Add($"{minutes} min");

            if (seconds > 0)
                parts.Add($"{seconds} sec");

            if (parts.Count == 0)
                return "0 sec";

            return string.Join(" ", parts);
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
