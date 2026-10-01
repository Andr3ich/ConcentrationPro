using ConcentrationTracker.Core.Services;
using ConcentrationTracker.MVVM.Model;
using ConcentrationTracker.MVVM.View.Windows;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Threading;

namespace ConcentrationTracker.MVVM.ViewModel
{
    public partial class MainViewModel : BaseViewModel
    {
        private readonly WindowTrackingService _trackingService;
        private readonly AppClassifierService _appClassifierService;
        private readonly AppCategoryRuleService _categoryRuleService;
        private readonly SessionHistoryStorageService _sessionHistoryStorageService;
        private readonly SessionDetailStorageService _sessionDetailStorageService;
        private readonly ConcentrationMetricsService _metricsService;
        private readonly ActivityAnalyticsService _activityAnalyticsService;
        private readonly IdleDetectionService _idleDetectionService;
        private readonly DispatcherTimer _sessionTimer;

        private readonly string _ownProcessName;
        private readonly string _ownProcessPath;
        private readonly TimeSpan _focusGracePeriod = TimeSpan.FromSeconds(60);
        private TimeSpan _idleThreshold;
        private TimeSpan _pendingIdleGracePeriod;

        private TimeSpan _lastObservedIdleTime;
        private DateTime _lastObservedIdleAt;

        private readonly TimeSpan _chartInterval = TimeSpan.FromMinutes(1);
        private readonly TimeSpan _scoreSampleInterval = TimeSpan.FromSeconds(15);

        private const int MaxChartItems = 12;
        private const int SwitchChartYAxisMax = 20;
        private const double SummaryBarMaxWidth = 560;
        private const double SummaryStackBarMaxWidth = 520;

        private readonly List<DateTime> _switchTimestamps;
        private readonly List<SwitchEventModel> _switchEvents;
        private readonly List<ScoreSampleModel> _scoreSamples;

        private DateTime _lastScoreSampleTime;
        private DateTime _sessionStartTime;
        private DateTime _currentRunStartTime;
        private DateTime _lastSwitchTime;
        private DateTime _pausedAt;

        private TimeSpan _elapsedBeforeCurrentRun;
        private bool _hasSessionStarted;

        private bool _isIgnoringOwnApp;
        private DateTime _ignoredOwnAppStartTime;
        private TimeSpan _ignoredOwnAppTotalTime;

        private bool _isIdle;
        private DateTime? _idleStartedAt;
        private TimeSpan _totalIdleTime;
        private int _idleBreakCount;

        private ActivityEventModel _currentActivityEvent;

        private ActiveWindowInfo _lastClassifiableActivity;
        private AppCategory _lastClassifiableActivityCategory;

        private FocusBlockModel _currentFocusBlock;
        private DateTime? _focusSuspendedAt;
        private FocusBreakReason _focusSuspensionReason;

        private bool _hasTrackedFirstExternalActivity;
        private string _lastTrackedExternalKey;
        private AppCategory _lastTrackedExternalCategory;
        private string _currentSessionId;

        public ObservableCollection<AppUsageModel> AppUsageList { get; set; }
        public ObservableCollection<ActivityEventModel> ActivityEvents { get; set; }
        public ObservableCollection<FocusBlockModel> FocusBlocks { get; set; }
        public ObservableCollection<AppCategoryRuleModel> CategoryRules { get; set; }
        public ObservableCollection<AppCategory> AvailableCategories { get; set; }
        public ObservableCollection<SessionRecordModel> SessionHistory { get; set; }

        public IEnumerable<ActivityEventModel> VisibleActivityEvents
        {
            get
            {
                return ActivityEvents
                    .Where(ShouldShowActivityEvent)
                    .Take(60)
                    .ToList();
            }
        }

        private AppCategoryRuleModel _selectedCategoryRule;
        public AppCategoryRuleModel SelectedCategoryRule
        {
            get => _selectedCategoryRule;
            set
            {
                _selectedCategoryRule = value;
                OnPropertyChanged();
            }
        }

        private string _newRuleAppPattern;
        public string NewRuleAppPattern
        {
            get => _newRuleAppPattern;
            set
            {
                _newRuleAppPattern = value;
                OnPropertyChanged();
            }
        }

        private string _newRuleTitlePattern;
        public string NewRuleTitlePattern
        {
            get => _newRuleTitlePattern;
            set
            {
                _newRuleTitlePattern = value;
                OnPropertyChanged();
            }
        }

        private AppCategory _newRuleCategory;
        public AppCategory NewRuleCategory
        {
            get => _newRuleCategory;
            set
            {
                _newRuleCategory = value;
                OnPropertyChanged();
            }
        }

        private string _categoryRulesStatus;
        public string CategoryRulesStatus
        {
            get => _categoryRulesStatus;
            set
            {
                _categoryRulesStatus = value;
                OnPropertyChanged();
            }
        }

        private SessionRecordModel _selectedSessionRecord;
        public SessionRecordModel SelectedSessionRecord
        {
            get => _selectedSessionRecord;
            set
            {
                _selectedSessionRecord = value;
                OnPropertyChanged();
            }
        }

        private string _sessionHistoryStatus;
        public string SessionHistoryStatus
        {
            get => _sessionHistoryStatus;
            set
            {
                _sessionHistoryStatus = value;
                OnPropertyChanged();
            }
        }

        public string SavedSessionsCount
        {
            get
            {
                if (SessionHistory == null)
                    return "0";

                return SessionHistory.Count.ToString();
            }
        }

        public string AverageSavedScore
        {
            get
            {
                if (SessionHistory == null || SessionHistory.Count == 0)
                    return "0%";

                double average = SessionHistory.Average(x => x.ConcentrationScore);
                return Math.Round(average).ToString("0") + "%";
            }
        }

        public string LastSavedSessionText
        {
            get
            {
                if (SessionHistory == null || SessionHistory.Count == 0)
                    return LocalizationService.GetString("History_NoSaved");

                SessionRecordModel lastSession =
                    SessionHistory.OrderByDescending(x => x.StartedAt).First();

                return LocalizationService.Format("History_LastSavedFormat", lastSession.StartedAtText, lastSession.ConcentrationScore);
            }
        }

        public string BestSavedScore
        {
            get
            {
                if (SessionHistory == null || SessionHistory.Count == 0)
                    return "0%";

                int bestScore =
                    SessionHistory.Max(x => x.ConcentrationScore);

                return bestScore + "%";
            }
        }

        public string BestSavedSessionText
        {
            get
            {
                if (SessionHistory == null || SessionHistory.Count == 0)
                    return LocalizationService.GetString("History_NoBest");

                SessionRecordModel bestSession =
                    SessionHistory
                        .OrderByDescending(x => x.ConcentrationScore)
                        .ThenByDescending(x => x.TrackedSeconds)
                        .First();

                return LocalizationService.Format("History_BestSavedFormat", bestSession.StartedAtText, bestSession.ConcentrationScore);
            }
        }

        public string TotalSavedTrackedTime
        {
            get
            {
                if (SessionHistory == null || SessionHistory.Count == 0)
                    return LocalizationService.GetString("Duration_Zero");

                double totalSeconds =
                    SessionHistory.Sum(x => x.TrackedSeconds);

                return FormatHistoryDuration(totalSeconds);
            }
        }

        public string TotalSavedProductiveTime
        {
            get
            {
                if (SessionHistory == null || SessionHistory.Count == 0)
                    return LocalizationService.GetString("Duration_Zero");

                double totalSeconds =
                    SessionHistory.Sum(x => x.ProductiveSeconds);

                return FormatHistoryDuration(totalSeconds);
            }
        }

        public string TotalSavedDistractionTime
        {
            get
            {
                if (SessionHistory == null || SessionHistory.Count == 0)
                    return LocalizationService.GetString("Duration_Zero");

                double totalSeconds =
                    SessionHistory.Sum(x => x.DistractionSeconds);

                return FormatHistoryDuration(totalSeconds);
            }
        }

        public string TotalSavedAfkTime
        {
            get
            {
                if (SessionHistory == null || SessionHistory.Count == 0)
                    return LocalizationService.GetString("Duration_Zero");

                double totalSeconds =
                    SessionHistory.Sum(x => x.AfkSeconds);

                return FormatHistoryDuration(totalSeconds);
            }
        }

        public string MostCommonDistraction
        {
            get
            {
                if (SessionHistory == null || SessionHistory.Count == 0)
                    return LocalizationService.GetString("Common_NoData");

                var grouped =
                    SessionHistory
                        .Where(x =>
                            !string.IsNullOrWhiteSpace(x.TopDistraction) &&
                            !x.TopDistraction.Equals("None", StringComparison.OrdinalIgnoreCase) &&
                            !x.TopDistraction.Equals("No data", StringComparison.OrdinalIgnoreCase))
                        .GroupBy(x => x.TopDistraction.Trim())
                        .Select(g => new
                        {
                            Name = g.Key,
                            Count = g.Count(),
                            Seconds = g.Sum(x => x.DistractionSeconds)
                        })
                        .OrderByDescending(x => x.Count)
                        .ThenByDescending(x => x.Seconds)
                        .FirstOrDefault();

                if (grouped == null)
                    return LocalizationService.GetString("Common_NoData");

                return grouped.Name;
            }
        }

        public string AverageDisruptiveSwitches
        {
            get
            {
                if (SessionHistory == null || SessionHistory.Count == 0)
                    return "0";

                double average =
                    SessionHistory.Average(x => x.DisruptiveSwitches);

                return Math.Round(average, 1).ToString("0.0");
            }
        }

        public string HistoryProductiveRatio
        {
            get
            {
                if (SessionHistory == null || SessionHistory.Count == 0)
                    return "0%";

                double tracked =
                    SessionHistory.Sum(x => x.TrackedSeconds);

                if (tracked <= 0)
                    return "0%";

                double productive =
                    SessionHistory.Sum(x => x.ProductiveSeconds);

                return Math.Round(productive / tracked * 100).ToString("0") + "%";
            }
        }

        public string HistoryDistractionRatio
        {
            get
            {
                if (SessionHistory == null || SessionHistory.Count == 0)
                    return "0%";

                double tracked =
                    SessionHistory.Sum(x => x.TrackedSeconds);

                if (tracked <= 0)
                    return "0%";

                double distraction =
                    SessionHistory.Sum(x => x.DistractionSeconds);

                return Math.Round(distraction / tracked * 100).ToString("0") + "%";
            }
        }

        private PlotModel _switchesPlotModel;
        public PlotModel SwitchesPlotModel
        {
            get => _switchesPlotModel;
            set
            {
                _switchesPlotModel = value;
                OnPropertyChanged();
            }
        }

        private PlotModel _scorePlotModel;
        public PlotModel ScorePlotModel
        {
            get => _scorePlotModel;
            set
            {
                _scorePlotModel = value;
                OnPropertyChanged();
            }
        }

        private PlotModel _historyScoreTrendPlotModel;
        public PlotModel HistoryScoreTrendPlotModel
        {
            get => _historyScoreTrendPlotModel;
            set
            {
                _historyScoreTrendPlotModel = value;
                OnPropertyChanged();
            }
        }

        private bool _isMonitoring;
        public bool IsMonitoring
        {
            get => _isMonitoring;
            set
            {
                _isMonitoring = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsMonitoringPaused));
            }
        }

        public bool IsMonitoringPaused => !IsMonitoring;

        public bool IsIdle
        {
            get => _isIdle;
            set
            {
                _isIdle = value;
                OnPropertyChanged();
            }
        }

        public bool IsViewingOwnApp => _isIgnoringOwnApp;

        private string _monitoringStatus;
        public string MonitoringStatus
        {
            get => _monitoringStatus;
            set
            {
                _monitoringStatus = value;
                OnPropertyChanged();
            }
        }

        private DashboardMode _currentMode;
        public DashboardMode CurrentMode
        {
            get => _currentMode;
            set
            {
                _currentMode = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsChartsMode));
                OnPropertyChanged(nameof(IsTimelineMode));
                OnPropertyChanged(nameof(IsSummaryMode));
                OnPropertyChanged(nameof(IsCategoriesMode));
                OnPropertyChanged(nameof(IsHistoryMode));
            }
        }

        public bool IsChartsMode => CurrentMode == DashboardMode.Charts;
        public bool IsTimelineMode => CurrentMode == DashboardMode.Timeline;
        public bool IsSummaryMode => CurrentMode == DashboardMode.Summary;
        public bool IsCategoriesMode => CurrentMode == DashboardMode.Categories;
        public bool IsHistoryMode => CurrentMode == DashboardMode.History;

        private int _selectedTabIndex;
        public int SelectedTabIndex
        {
            get => _selectedTabIndex;
            set
            {
                _selectedTabIndex = value;
                OnPropertyChanged();
            }
        }

        private int _totalSwitches;
        public int TotalSwitches
        {
            get => _totalSwitches;
            set
            {
                _totalSwitches = value;
                OnPropertyChanged();
                RefreshCalculatedProperties();
            }
        }

        private int _interruptionCount;
        public int InterruptionCount
        {
            get => _interruptionCount;
            set
            {
                _interruptionCount = value;
                OnPropertyChanged();
            }
        }

        private string _sessionTime;
        public string SessionTime
        {
            get => _sessionTime;
            set
            {
                _sessionTime = value;
                OnPropertyChanged();
            }
        }

        private string _trackedTime;
        public string TrackedTime
        {
            get => _trackedTime;
            set
            {
                _trackedTime = value;
                OnPropertyChanged();
            }
        }

        public string AvgSwitchesPerHour
        {
            get
            {
                TimeSpan trackedElapsed = GetTrackedElapsedTime();

                if (TotalSwitches <= 0 || trackedElapsed.TotalSeconds <= 0)
                    return "0.0";

                double stableMinutes = Math.Max(trackedElapsed.TotalMinutes, 5.0);
                double switchesPerFiveMinutes = TotalSwitches / stableMinutes * 5.0;

                return switchesPerFiveMinutes.ToString("0.0");
            }
        }

        public string FocusStreak
        {
            get
            {
                if (!_hasSessionStarted)
                    return LocalizationService.GetString("Duration_Zero");

                TimeSpan streak = GetCurrentFocusStreak();
                TimeSpan trackedElapsed = GetTrackedElapsedTime();

                if (streak > trackedElapsed)
                    streak = trackedElapsed;

                return FormatDurationShort(streak);
            }
        }

        public string LastFocusBlock
        {
            get
            {
                FocusBlockModel lastCompletedBlock = FocusBlocks.FirstOrDefault(x =>
                    !x.IsActive &&
                    !x.IsSuspended &&
                    x.CurrentDuration.TotalSeconds > 0);

                if (lastCompletedBlock == null)
                    return LocalizationService.GetString("Duration_Zero");

                return FormatDurationShort(lastCompletedBlock.CurrentDuration);
            }
        }

        public string BestFocusBlock
        {
            get
            {
                FocusBlockModel bestBlock = FocusBlocks
                    .OrderByDescending(x => GetFocusBlockDurationForDisplay(x))
                    .FirstOrDefault();

                if (bestBlock == null)
                    return LocalizationService.GetString("Duration_Zero");

                return FormatDurationShort(GetFocusBlockDurationForDisplay(bestBlock));
            }
        }

        public string ProductiveTime
        {
            get
            {
                TimeSpan trackedElapsed = GetTrackedElapsedTime();

                if (trackedElapsed.TotalSeconds <= 0)
                    return "0%";

                int value = _metricsService.CalculateProductiveTimePercent(
                    ActivityEvents,
                    trackedElapsed,
                    GetMetricsNow());

                return value + "%";
            }
        }

        public string ProductiveDuration
        {
            get
            {
                return _activityAnalyticsService.GetCategoryDurationText(
                    ActivityEvents,
                    AppCategory.Productive,
                    GetMetricsNow());
            }
        }

        public string NeutralDuration
        {
            get
            {
                return _activityAnalyticsService.GetCategoryDurationText(
                    ActivityEvents,
                    AppCategory.Neutral,
                    GetMetricsNow());
            }
        }

        public string CommunicationDuration
        {
            get
            {
                return _activityAnalyticsService.GetCategoryDurationText(
                    ActivityEvents,
                    AppCategory.Communication,
                    GetMetricsNow());
            }
        }

        public string DistractionDuration
        {
            get
            {
                return _activityAnalyticsService.GetCategoryDurationText(
                    ActivityEvents,
                    AppCategory.Distraction,
                    GetMetricsNow());
            }
        }

        public string MostUsedApp
        {
            get
            {
                return _activityAnalyticsService.GetTopAppByDuration(
                    ActivityEvents,
                    GetMetricsNow());
            }
        }

        public string TopDistraction
        {
            get
            {
                return _activityAnalyticsService.GetTopAppByCategoryDuration(
                    ActivityEvents,
                    AppCategory.Distraction,
                    GetMetricsNow());
            }
        }

        public string MainWorkContext
        {
            get
            {
                return _activityAnalyticsService.GetMainWorkContext(
                    ActivityEvents,
                    GetMetricsNow());
            }
        }

        public SummaryAppMetricModel MostUsedAppMetric
        {
            get
            {
                return GetTopAppMetricByDuration(
                    "Most Used App",
                    null);
            }
        }

        public SummaryAppMetricModel MainWorkContextMetric
        {
            get
            {
                return GetTopAppMetricByDuration(
                    "Main Work Context",
                    new[]
                    {
                        AppCategory.Productive,
                        AppCategory.Neutral
                    });
            }
        }

        public SummaryAppMetricModel TopDistractionMetric
        {
            get
            {
                return GetTopAppMetricByDuration(
                    "Top Distraction",
                    new[]
                    {
                        AppCategory.Distraction
                    });
            }
        }

        public SummaryAppMetricModel TopInterrupterMetric
        {
            get
            {
                return GetTopInterrupterMetric();
            }
        }

        public double SummaryStackBarWidth => SummaryStackBarMaxWidth;
        public double ProductiveStackWidth => GetActivityStackSegmentWidth(AppCategory.Productive);
        public double NeutralStackWidth => GetActivityStackSegmentWidth(AppCategory.Neutral);
        public double CommunicationStackWidth => GetActivityStackSegmentWidth(AppCategory.Communication);
        public double DistractionStackWidth => GetActivityStackSegmentWidth(AppCategory.Distraction);
        public double AfkStackWidth => GetAfkStackSegmentWidth();

        public string DisruptiveSwitches
        {
            get
            {
                return GetDisruptiveSwitchCount().ToString();
            }
        }

        public string TopInterrupter
        {
            get
            {
                return GetTopInterrupterName();
            }
        }

        public string SessionInsight
        {
            get
            {
                return BuildSessionInsight();
            }
        }

        public string AwayTime
        {
            get
            {
                return FormatDurationShort(GetAfkDuration());
            }
        }

        public string AfkSessions
        {
            get
            {
                return _idleBreakCount.ToString();
            }
        }

        public string AwaySummary
        {
            get
            {
                return LocalizationService.Format(
                    "AwaySummary_Format",
                    AwayTime,
                    AfkSessions);
            }
        }

        public string AfkTime
        {
            get
            {
                return AwayTime;
            }
        }

        public string IdleBreaks
        {
            get
            {
                return AfkSessions;
            }
        }

        public double ProductiveBarWidth => GetCategoryBarWidth(AppCategory.Productive);
        public double NeutralBarWidth => GetCategoryBarWidth(AppCategory.Neutral);
        public double CommunicationBarWidth => GetCategoryBarWidth(AppCategory.Communication);
        public double DistractionBarWidthSummary => GetCategoryBarWidth(AppCategory.Distraction);

        private string _currentApp;
        public string CurrentApp
        {
            get => _currentApp;
            set
            {
                _currentApp = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CurrentAppDisplay));
            }
        }

        public string CurrentAppDisplay
        {
            get
            {
                if (string.IsNullOrWhiteSpace(CurrentApp) || IsNoActiveAppPlaceholder(CurrentApp))
                    return LocalizationService.GetString("Focus_NoActiveApp");

                if (string.Equals(CurrentApp, "Dashboard", StringComparison.OrdinalIgnoreCase))
                    return LocalizationService.GetString("Focus_DashboardAppName");

                if (string.Equals(CurrentApp, "Idle", StringComparison.OrdinalIgnoreCase))
                    return LocalizationService.GetString("Focus_IdleAppName");

                return CurrentApp;
            }
        }

        private string _currentWindowTitle;
        public string CurrentWindowTitle
        {
            get => _currentWindowTitle;
            set
            {
                _currentWindowTitle = value;
                OnPropertyChanged();
            }
        }

        private string _currentProcessPath;
        public string CurrentProcessPath
        {
            get => _currentProcessPath;
            set
            {
                _currentProcessPath = value;
                OnPropertyChanged();
            }
        }

        private AppCategory _currentCategory;
        public AppCategory CurrentCategory
        {
            get => _currentCategory;
            set
            {
                _currentCategory = value;
                OnPropertyChanged();
            }
        }

        private string _currentAppUserModelId;
        public string CurrentAppUserModelId
        {
            get => _currentAppUserModelId;
            set
            {
                _currentAppUserModelId = value;
                OnPropertyChanged();
            }
        }

        private string _currentPackageFullName;
        public string CurrentPackageFullName
        {
            get => _currentPackageFullName;
            set
            {
                _currentPackageFullName = value;
                OnPropertyChanged();
            }
        }

        private string _currentPackageInstallPath;
        public string CurrentPackageInstallPath
        {
            get => _currentPackageInstallPath;
            set
            {
                _currentPackageInstallPath = value;
                OnPropertyChanged();
            }
        }

        public string QuickClassifySourceText
        {
            get
            {
                if (_lastClassifiableActivity == null)
                    return LocalizationService.GetString("Categories_NoExternalCaptured");

                string displayName =
                    GetQuickClassifyDisplayName(_lastClassifiableActivity);

                return LocalizationService.Format("Categories_LastCaptured", displayName);
            }
        }

        public string ConcentrationScore
        {
            get
            {
                TimeSpan trackedElapsed = GetTrackedElapsedTime();

                if (!_hasSessionStarted || trackedElapsed.TotalSeconds <= 0)
                    return "0";

                int score = _metricsService.CalculateConcentrationScore(
                    ActivityEvents,
                    FocusBlocks,
                    trackedElapsed,
                    InterruptionCount,
                    GetMetricsNow());

                return score.ToString();
            }
        }

        public string DeepFocusPercent => GetFocusQualityValue() + "%";
        public string DistractionControlPercent => GetFocusStabilityValue() + "%";
        public double DeepFocusBarWidth => GetFocusQualityValue() * 3;
        public double DistractionBarWidth => GetFocusStabilityValue() * 3;

        public ICommand ShowChartsCommand { get; set; }
        public ICommand ShowTimelineCommand { get; set; }
        public ICommand ShowSummaryCommand { get; set; }
        public ICommand ShowCategoriesCommand { get; set; }
        public ICommand ShowHistoryCommand { get; set; }
        public ICommand StartMonitoringCommand { get; set; }
        public ICommand PauseMonitoringCommand { get; set; }
        public ICommand ResetSessionCommand { get; set; }
        public ICommand AddCategoryRuleCommand { get; set; }
        public ICommand DeleteCategoryRuleCommand { get; set; }
        public ICommand SaveCategoryRulesCommand { get; set; }
        public ICommand ReloadCategoryRulesCommand { get; set; }
        public ICommand UseCurrentActivityForRuleCommand { get; set; }
        public ICommand SetCurrentActivityCategoryCommand { get; set; }
        public ICommand SaveCurrentSessionCommand { get; set; }
        public ICommand ReloadSessionHistoryCommand { get; set; }
        public ICommand DeleteSessionRecordCommand { get; set; }
        public ICommand OpenSessionDetailsCommand { get; set; }
        public ICommand ClearSessionHistoryCommand { get; set; }

        public MainViewModel()
        {
            AppUsageList = new ObservableCollection<AppUsageModel>();
            ActivityEvents = new ObservableCollection<ActivityEventModel>();
            FocusBlocks = new ObservableCollection<FocusBlockModel>();
            CategoryRules = new ObservableCollection<AppCategoryRuleModel>();
            AvailableCategories = new ObservableCollection<AppCategory>
            {
                AppCategory.Productive,
                AppCategory.Neutral,
                AppCategory.Communication,
                AppCategory.Distraction,
                AppCategory.System
            };
            SessionHistory = new ObservableCollection<SessionRecordModel>();

            _switchTimestamps = new List<DateTime>();
            _switchEvents = new List<SwitchEventModel>();
            _scoreSamples = new List<ScoreSampleModel>();

            _categoryRuleService = new AppCategoryRuleService();
            _sessionHistoryStorageService = new SessionHistoryStorageService();
            _sessionDetailStorageService = new SessionDetailStorageService();
            _appClassifierService = new AppClassifierService(_categoryRuleService);
            _metricsService = new ConcentrationMetricsService();
            _activityAnalyticsService = new ActivityAnalyticsService();
            _idleDetectionService = new IdleDetectionService();
            _trackingService = new WindowTrackingService();
            _sessionTimer = new DispatcherTimer();

            AppSettingsModel appSettings =
                AppSettingsService.LoadSettings();

            ApplyAppSettings(
                appSettings);

            AppSettingsService.SettingsChanged +=
                ApplyAppSettings;

            _ownProcessName = Process.GetCurrentProcess().ProcessName;
            _ownProcessPath = GetOwnProcessPath();

            DateTime now = DateTime.Now;

            _sessionStartTime = now;
            _currentRunStartTime = now;
            _lastSwitchTime = now;
            _pausedAt = now;
            _ignoredOwnAppStartTime = now;

            _elapsedBeforeCurrentRun = TimeSpan.Zero;
            _ignoredOwnAppTotalTime = TimeSpan.Zero;
            _idleStartedAt = null;
            _totalIdleTime = TimeSpan.Zero;
            _idleBreakCount = 0;
            _lastObservedIdleTime = TimeSpan.Zero;
            _lastObservedIdleAt = now;
            _hasSessionStarted = false;
            _isIgnoringOwnApp = false;
            IsIdle = false;

            _currentFocusBlock = null;
            _focusSuspendedAt = null;
            _focusSuspensionReason = FocusBreakReason.None;

            _hasTrackedFirstExternalActivity = false;
            _lastTrackedExternalKey = string.Empty;
            _lastTrackedExternalCategory = AppCategory.Neutral;
            _currentSessionId = Guid.NewGuid().ToString();
            _lastScoreSampleTime = DateTime.MinValue;

            CurrentApp = LocalizationService.GetString("Focus_NoActiveApp");
            CurrentWindowTitle = LocalizationService.GetString("Focus_NoActiveWindow");
            CurrentProcessPath = string.Empty;
            CurrentAppUserModelId = string.Empty;
            CurrentPackageFullName = string.Empty;
            CurrentPackageInstallPath = string.Empty;
            CurrentCategory = AppCategory.Neutral;
            NewRuleAppPattern = string.Empty;
            NewRuleTitlePattern = "*";
            NewRuleCategory = AppCategory.Neutral;
            CategoryRulesStatus = LocalizationService.GetString("Categories_InitialStatus");
            SessionHistoryStatus = LocalizationService.GetString("History_InitialStatus");
            CurrentMode = appSettings.DefaultDashboardTab;
            SelectedTabIndex = GetTabIndexFromDashboardMode(CurrentMode);
            SessionTime = "00:00:00";
            TrackedTime = "00:00:00";
            TotalSwitches = 0;
            InterruptionCount = 0;
            MonitoringStatus = LocalizationService.GetString("Monitoring_StatusPaused");
            IsMonitoring = false;

            LoadCategoryRules();
            LoadSessionHistory();
            UpdateChartData();

            ShowChartsCommand = new RelayCommand(o =>
            {
                CurrentMode = DashboardMode.Charts;
                SelectedTabIndex = 0;
            });

            ShowTimelineCommand = new RelayCommand(o =>
            {
                CurrentMode = DashboardMode.Timeline;
                SelectedTabIndex = 1;
            });

            ShowSummaryCommand = new RelayCommand(o =>
            {
                CurrentMode = DashboardMode.Summary;
                SelectedTabIndex = 2;
            });

            ShowCategoriesCommand = new RelayCommand(o =>
            {
                CurrentMode = DashboardMode.Categories;
                SelectedTabIndex = 3;
            });

            ShowHistoryCommand = new RelayCommand(o =>
            {
                CurrentMode = DashboardMode.History;
                SelectedTabIndex = 4;
            });

            StartMonitoringCommand = new RelayCommand(o => StartMonitoring());
            PauseMonitoringCommand = new RelayCommand(o => PauseMonitoring());
            ResetSessionCommand = new RelayCommand(o => ResetSession());
            AddCategoryRuleCommand = new RelayCommand(o => AddCategoryRule());
            DeleteCategoryRuleCommand = new RelayCommand(o => DeleteCategoryRule(o));
            SaveCategoryRulesCommand = new RelayCommand(o => SaveCategoryRules());
            ReloadCategoryRulesCommand = new RelayCommand(o => ReloadCategoryRules());
            UseCurrentActivityForRuleCommand = new RelayCommand(o => UseCurrentActivityForRule());
            SetCurrentActivityCategoryCommand = new RelayCommand(o => SetCurrentActivityCategory(o));
            SaveCurrentSessionCommand = new RelayCommand(o => SaveCurrentSessionSnapshot(DateTime.Now, "Manual Save"));
            ReloadSessionHistoryCommand = new RelayCommand(o => LoadSessionHistory());
            DeleteSessionRecordCommand = new RelayCommand(o => DeleteSessionRecord(o));
            OpenSessionDetailsCommand = new RelayCommand(o => OpenSessionDetails(o));
            ClearSessionHistoryCommand = new RelayCommand(o => ClearSessionHistory());

            _sessionTimer.Interval = TimeSpan.FromSeconds(1);
            _sessionTimer.Tick += (s, e) =>
            {
                CheckIdleState();
                UpdateTimeDisplays();
                RefreshCurrentActivityDuration();
                RefreshCurrentFocusBlockDuration();
                UpdateChartData();
                RefreshCalculatedProperties();
            };

            LocalizationService.LanguageChanged +=
                LocalizationService_LanguageChanged;

            _trackingService.OnWindowSwitched += activeWindow =>
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    if (IsMonitoring && !IsIdle)
                    {
                        RegisterWindowSwitch(activeWindow, true);
                    }
                });
            };
        }

        private void LocalizationService_LanguageChanged(
            object sender,
            EventArgs e)
        {
            if (!_hasSessionStarted && !IsMonitoring)
            {
                CurrentApp = LocalizationService.GetString("Focus_NoActiveApp");
                CurrentWindowTitle = LocalizationService.GetString("Focus_NoActiveWindow");
            }
            else if (string.Equals(CurrentApp, "Dashboard", StringComparison.OrdinalIgnoreCase))
            {
                CurrentWindowTitle = LocalizationService.GetString("Focus_DashboardExcluded");
            }
            else if (string.Equals(CurrentApp, "Idle", StringComparison.OrdinalIgnoreCase))
            {
                CurrentWindowTitle = LocalizationService.GetString("Focus_AwaySavedSeparately");
            }

            MonitoringStatus = IsMonitoring
                ? (IsIdle
                    ? LocalizationService.GetString("Monitoring_StatusAway")
                    : LocalizationService.GetString("Monitoring_StatusActive"))
                : LocalizationService.GetString("Monitoring_StatusPaused");

            if (SessionHistory != null && SessionHistory.Count > 0)
            {
                SessionHistoryStatus = LocalizationService.Format(
                    "History_LoadedStatus",
                    SessionHistory.Count);
            }
            else
            {
                SessionHistoryStatus = LocalizationService.GetString("History_InitialStatus");
            }

            if (CategoryRules != null && CategoryRules.Count > 0)
            {
                CategoryRulesStatus = LocalizationService.Format(
                    "Categories_StatusLoaded",
                    CategoryRules.Count);
            }
            else
            {
                CategoryRulesStatus = LocalizationService.GetString("Categories_InitialStatus");
            }

            UpdateChartData();
            UpdateHistoryCharts();
            RefreshCalculatedProperties();
            RefreshSessionHistoryProperties();

            if (ActivityEvents != null)
                CollectionViewSource.GetDefaultView(ActivityEvents).Refresh();

            if (SessionHistory != null)
                CollectionViewSource.GetDefaultView(SessionHistory).Refresh();

            if (CategoryRules != null)
                CollectionViewSource.GetDefaultView(CategoryRules).Refresh();

            OnPropertyChanged(nameof(CurrentAppDisplay));
            OnPropertyChanged(nameof(CurrentWindowTitle));
            OnPropertyChanged(nameof(MonitoringStatus));
            OnPropertyChanged(nameof(QuickClassifySourceText));
            OnPropertyChanged(nameof(CategoryRulesStatus));
            OnPropertyChanged(nameof(SessionHistoryStatus));
        }

        private void ApplyAppSettings(
            AppSettingsModel settings)
        {
            if (settings == null)
            {
                settings =
                    new AppSettingsModel();
            }

            double idleThresholdMinutes =
                settings.IdleThresholdMinutes;

            if (double.IsNaN(idleThresholdMinutes) ||
                double.IsInfinity(idleThresholdMinutes) ||
                idleThresholdMinutes < 0.25)
            {
                idleThresholdMinutes =
                    0.25;
            }

            if (idleThresholdMinutes > 60)
            {
                idleThresholdMinutes =
                    60;
            }

            int pendingIdleGraceSeconds =
                settings.PendingIdleGraceSeconds;

            if (pendingIdleGraceSeconds < 0)
            {
                pendingIdleGraceSeconds =
                    0;
            }

            if (pendingIdleGraceSeconds > 300)
            {
                pendingIdleGraceSeconds =
                    300;
            }

            _idleThreshold =
                TimeSpan.FromMinutes(
                    idleThresholdMinutes);

            _pendingIdleGracePeriod =
                TimeSpan.FromSeconds(
                    pendingIdleGraceSeconds);
        }

        private int GetTabIndexFromDashboardMode(
            DashboardMode mode)
        {
            switch (mode)
            {
                case DashboardMode.Charts:
                    return 0;

                case DashboardMode.Timeline:
                    return 1;

                case DashboardMode.Summary:
                    return 2;

                case DashboardMode.Categories:
                    return 3;

                case DashboardMode.History:
                    return 4;

                default:
                    return 0;
            }
        }

        private int ParseIntSafe(string value)
        {
            int result;

            if (int.TryParse((value ?? string.Empty).Replace("%", string.Empty).Trim(), out result))
                return result;

            return 0;
        }

        private void StartMonitoring()
        {
            if (IsMonitoring)
                return;

            DateTime now = DateTime.Now;

            if (!_hasSessionStarted)
            {
                _sessionStartTime = now;
                _elapsedBeforeCurrentRun = TimeSpan.Zero;
                _ignoredOwnAppTotalTime = TimeSpan.Zero;
                _totalIdleTime = TimeSpan.Zero;
                _idleBreakCount = 0;
                _lastObservedIdleTime = TimeSpan.Zero;
                _lastObservedIdleAt = now;
                _switchTimestamps.Clear();
                _switchEvents.Clear();
                _scoreSamples.Clear();
                _lastScoreSampleTime = DateTime.MinValue;
                SessionTime = "00:00:00";
                TrackedTime = "00:00:00";
                _hasSessionStarted = true;
                _currentSessionId = Guid.NewGuid().ToString();
            }

            _currentRunStartTime = now;
            _lastSwitchTime = now;

            BeginIgnoringOwnApp(now);

            IsIdle = false;
            _idleStartedAt = null;
            IsMonitoring = true;
            MonitoringStatus = LocalizationService.GetString("Monitoring_StatusActive");

            _sessionTimer.Start();
            _trackingService.Start();

            UpdateChartData();
            RefreshCalculatedProperties();
        }

        private void PauseMonitoring()
        {
            if (!IsMonitoring)
                return;

            DateTime now = DateTime.Now;

            EndIdlePeriod(now, false);
            EndIgnoringOwnApp(now);

            _pausedAt = now;
            _elapsedBeforeCurrentRun += now - _currentRunStartTime;

            IsMonitoring = false;
            MonitoringStatus = LocalizationService.GetString("Monitoring_StatusPaused");

            _trackingService.Stop();
            _sessionTimer.Stop();

            FinishCurrentActivity(now);

            CompleteCurrentFocusBlock(
                now,
                FocusBreakReason.SessionPaused,
                false);

            SaveCurrentSessionSnapshot(now, "Paused");

            UpdateTimeDisplays();
            RefreshCurrentActivityDuration();
            RefreshCurrentFocusBlockDuration();
            UpdateChartData();
            RefreshCalculatedProperties();
        }

        private void ResetSession()
        {
            DateTime resetTime = DateTime.Now;

            if (_hasSessionStarted)
            {
                SaveCurrentSessionSnapshot(resetTime, "Completed");
            }

            _trackingService.Stop();
            _sessionTimer.Stop();

            AppUsageList.Clear();
            ActivityEvents.Clear();
            FocusBlocks.Clear();

            _switchTimestamps.Clear();
            _switchEvents.Clear();
            _scoreSamples.Clear();
            _lastScoreSampleTime = DateTime.MinValue;

            _currentActivityEvent = null;
            _currentFocusBlock = null;
            _focusSuspendedAt = null;
            _focusSuspensionReason = FocusBreakReason.None;
            _hasTrackedFirstExternalActivity = false;
            _lastTrackedExternalKey = string.Empty;
            _lastTrackedExternalCategory = AppCategory.Neutral;
            _currentSessionId = Guid.NewGuid().ToString();

            DateTime now = resetTime;

            _sessionStartTime = now;
            _currentRunStartTime = now;
            _lastSwitchTime = now;
            _pausedAt = now;
            _ignoredOwnAppStartTime = now;

            _elapsedBeforeCurrentRun = TimeSpan.Zero;
            _ignoredOwnAppTotalTime = TimeSpan.Zero;
            _idleStartedAt = null;
            _totalIdleTime = TimeSpan.Zero;
            _idleBreakCount = 0;
            _lastObservedIdleTime = TimeSpan.Zero;
            _lastObservedIdleAt = now;
            _hasSessionStarted = false;
            _isIgnoringOwnApp = false;
            IsIdle = false;
            TotalSwitches = 0;
            InterruptionCount = 0;
            SessionTime = "00:00:00";
            TrackedTime = "00:00:00";
            CurrentApp = LocalizationService.GetString("Focus_NoActiveApp");
            CurrentWindowTitle = LocalizationService.GetString("Focus_NoActiveWindow");
            CurrentProcessPath = string.Empty;
            CurrentAppUserModelId = string.Empty;
            CurrentPackageFullName = string.Empty;
            CurrentPackageInstallPath = string.Empty;
            CurrentCategory = AppCategory.Neutral;
            NewRuleAppPattern = string.Empty;
            NewRuleTitlePattern = "*";
            NewRuleCategory = AppCategory.Neutral;
            CategoryRulesStatus = LocalizationService.GetString("Categories_InitialStatus");
            SessionHistoryStatus = LocalizationService.GetString("History_InitialStatus");
            AppSettingsModel appSettings =
                AppSettingsService.LoadSettings();

            CurrentMode = appSettings.DefaultDashboardTab;
            SelectedTabIndex = GetTabIndexFromDashboardMode(CurrentMode);
            IsMonitoring = false;
            MonitoringStatus = LocalizationService.GetString("Monitoring_StatusPaused");

            UpdateChartData();
            RefreshCalculatedProperties();
        }

        private void CheckIdleState()
        {
            if (!IsMonitoring || !_hasSessionStarted)
                return;

            DateTime now = DateTime.Now;
            TimeSpan idleTime = _idleDetectionService.GetIdleTime();
            TimeSpan awayThreshold = GetAwayDetectionThreshold();

            RememberObservedIdleTime(idleTime, now);

            if (idleTime >= awayThreshold && !IsIdle)
            {
                DateTime idleStartTime = now - idleTime;
                idleStartTime = ClampIdleStartTime(idleStartTime);

                if (_isIgnoringOwnApp)
                {
                    EndIgnoringOwnApp(idleStartTime);
                }

                StartIdlePeriod(idleStartTime);
                return;
            }

            if (idleTime < awayThreshold && IsIdle)
            {
                EndIdlePeriod(now, true);
            }
        }

        private void RememberObservedIdleTime(TimeSpan idleTime, DateTime observedAt)
        {
            if (idleTime.TotalSeconds < 0)
                idleTime = TimeSpan.Zero;

            _lastObservedIdleTime = idleTime;
            _lastObservedIdleAt = observedAt;
        }

        private TimeSpan GetAwayDetectionThreshold()
        {
            if (_idleThreshold > TimeSpan.Zero)
                return _idleThreshold;

            return TimeSpan.FromSeconds(10);
        }

        private DateTime GetCurrentLastInputTime()
        {
            TimeSpan idleTime = _idleDetectionService.GetIdleTime();
            return DateTime.Now - idleTime;
        }

        private DateTime ClampIdleStartTime(DateTime idleStartTime)
        {
            if (idleStartTime < _currentRunStartTime)
                idleStartTime = _currentRunStartTime;

            if (_currentActivityEvent != null && idleStartTime < _currentActivityEvent.StartTime)
                idleStartTime = _currentActivityEvent.StartTime;

            if (_currentFocusBlock != null &&
                _currentFocusBlock.CurrentSegmentStartTime.HasValue &&
                idleStartTime < _currentFocusBlock.CurrentSegmentStartTime.Value)
            {
                idleStartTime = _currentFocusBlock.CurrentSegmentStartTime.Value;
            }

            return idleStartTime;
        }

        private void StartIdlePeriod(DateTime idleStartTime)
        {
            if (IsIdle)
                return;

            IsIdle = true;
            _idleStartedAt = idleStartTime;
            _idleBreakCount++;

            FinishCurrentActivity(idleStartTime);
            _currentActivityEvent = null;

            CompleteCurrentFocusBlock(
                idleStartTime,
                FocusBreakReason.Idle,
                false);

            CurrentApp = "Idle";
            CurrentWindowTitle = LocalizationService.GetString("Focus_AwaySavedSeparately");
            CurrentProcessPath = string.Empty;
            CurrentAppUserModelId = string.Empty;
            CurrentPackageFullName = string.Empty;
            CurrentPackageInstallPath = string.Empty;
            CurrentCategory = AppCategory.System;
            MonitoringStatus = LocalizationService.GetString("Monitoring_StatusAway");

            UpdateChartData();
            RefreshCalculatedProperties();
        }

        private void EndIdlePeriod(DateTime endTime, bool resumeCurrentWindow)
        {
            if (!IsIdle)
                return;

            if (_idleStartedAt.HasValue)
            {
                TimeSpan idleDuration = endTime - _idleStartedAt.Value;

                if (idleDuration.TotalSeconds > 0)
                    _totalIdleTime += idleDuration;
            }

            IsIdle = false;
            _idleStartedAt = null;
            MonitoringStatus = LocalizationService.GetString("Monitoring_StatusActive");

            if (resumeCurrentWindow)
            {
                ActiveWindowInfo activeWindow = _trackingService.CaptureActiveWindow();

                if (activeWindow != null)
                {
                    RegisterWindowSwitch(activeWindow, false);
                }
            }

            UpdateChartData();
            RefreshCalculatedProperties();
        }

        private void CommitPendingIdleBeforeContextChange(DateTime endTime)
        {
            if (!IsMonitoring || IsIdle || !_hasSessionStarted)
                return;

            TimeSpan awayThreshold = GetAwayDetectionThreshold();
            TimeSpan idleTime = _idleDetectionService.GetIdleTime();
            DateTime idleObservedAt = endTime;

            if (idleTime < awayThreshold || idleTime.TotalSeconds <= 0)
            {
                TimeSpan observationAge = endTime - _lastObservedIdleAt;

                if (_lastObservedIdleTime >= awayThreshold &&
                    observationAge >= TimeSpan.Zero &&
                    observationAge <= _pendingIdleGracePeriod)
                {
                    idleTime = _lastObservedIdleTime;
                    idleObservedAt = _lastObservedIdleAt;
                }
                else
                {
                    return;
                }
            }

            DateTime pendingIdleStartTime = idleObservedAt - idleTime;
            pendingIdleStartTime = ClampIdleStartTime(pendingIdleStartTime);

            TimeSpan pendingIdleDuration = endTime - pendingIdleStartTime;

            if (pendingIdleDuration < awayThreshold ||
                pendingIdleDuration.TotalSeconds <= 0)
            {
                return;
            }

            if (_isIgnoringOwnApp)
            {
                EndIgnoringOwnApp(pendingIdleStartTime);
            }

            _totalIdleTime += pendingIdleDuration;
            _idleBreakCount++;

            FinishCurrentActivity(pendingIdleStartTime);
            _currentActivityEvent = null;

            CompleteCurrentFocusBlock(
                pendingIdleStartTime,
                FocusBreakReason.Idle,
                false);

            RememberObservedIdleTime(TimeSpan.Zero, endTime);
            MonitoringStatus = LocalizationService.GetString("Monitoring_StatusActive");
        }

        private void RegisterWindowSwitch(ActiveWindowInfo activeWindow, bool countSwitchStatistics)
        {
            if (activeWindow == null)
                return;

            if (string.IsNullOrWhiteSpace(activeWindow.AppName))
                activeWindow.AppName = LocalizationService.GetString("Common_UnknownApp");

            if (string.IsNullOrWhiteSpace(activeWindow.WindowTitle))
                activeWindow.WindowTitle = LocalizationService.GetString("Common_UntitledWindow");

            CommitPendingIdleBeforeContextChange(activeWindow.DetectedAt);

            if (IsOwnApplication(activeWindow))
            {
                HandleOwnApplicationActivated(activeWindow.DetectedAt);
                return;
            }

            EndIgnoringOwnApp(activeWindow.DetectedAt);

            MonitoringStatus = LocalizationService.GetString("Monitoring_StatusActive");

            CurrentApp = activeWindow.AppName;
            CurrentWindowTitle = activeWindow.WindowTitle;

            AppCategory category = _appClassifierService.Classify(
                activeWindow.AppName,
                activeWindow.WindowTitle);

            category = RefineBrowserCategory(
                activeWindow,
                category);

            CurrentProcessPath = activeWindow.ProcessPath;
            CurrentAppUserModelId = activeWindow.AppUserModelId;
            CurrentPackageFullName = activeWindow.PackageFullName;
            CurrentPackageInstallPath = activeWindow.PackageInstallPath;
            CurrentCategory = category;

            RememberClassifiableActivity(activeWindow, category);

            FinishCurrentActivity(activeWindow.DetectedAt);
            StartNewActivity(activeWindow, category);

            if (countSwitchStatistics)
            {
                UpdateAppUsage(activeWindow.AppName, category);
                UpdateSwitchStatistics(activeWindow, category);
            }

            UpdateFocusBlockState(activeWindow, category);

            _lastSwitchTime = activeWindow.DetectedAt;

            TryAddScoreSample();
            UpdateChartData();
            RefreshCalculatedProperties();
        }

        private void UpdateSwitchStatistics(
            ActiveWindowInfo activeWindow,
            AppCategory currentCategory)
        {
            string currentKey = GetExternalActivityKey(activeWindow);

            if (!_hasTrackedFirstExternalActivity)
            {
                _hasTrackedFirstExternalActivity = true;
                _lastTrackedExternalKey = currentKey;
                _lastTrackedExternalCategory = currentCategory;
                return;
            }

            if (_lastTrackedExternalKey != currentKey)
            {
                bool isDisruptive =
                    IsInterruptiveCategory(currentCategory) &&
                    !IsSameCategoryContext(_lastTrackedExternalCategory, currentCategory);

                TotalSwitches++;
                _switchTimestamps.Add(activeWindow.DetectedAt);
                _switchEvents.Add(
                    new SwitchEventModel
                    {
                        Timestamp = activeWindow.DetectedAt,
                        FromCategory = _lastTrackedExternalCategory,
                        ToCategory = currentCategory,
                        ToAppName = activeWindow.AppName,
                        ToWindowTitle = activeWindow.WindowTitle,
                        IsDisruptive = isDisruptive
                    });
            }

            _lastTrackedExternalKey = currentKey;
            _lastTrackedExternalCategory = currentCategory;
        }

        private bool IsSameCategoryContext(
            AppCategory previousCategory,
            AppCategory currentCategory)
        {
            return previousCategory == currentCategory;
        }

        private string GetExternalActivityKey(ActiveWindowInfo activeWindow)
        {
            string app = activeWindow.AppName ?? string.Empty;
            string title = activeWindow.WindowTitle ?? string.Empty;

            return (app + "|" + title).ToLowerInvariant();
        }

        private string GetOwnProcessPath()
        {
            try
            {
                Process currentProcess = Process.GetCurrentProcess();

                if (currentProcess.MainModule != null &&
                    !string.IsNullOrWhiteSpace(currentProcess.MainModule.FileName))
                {
                    return currentProcess.MainModule.FileName;
                }
            }
            catch
            {
            }

            return string.Empty;
        }

        private bool IsOwnApplication(ActiveWindowInfo activeWindow)
        {
            if (activeWindow == null)
                return false;

            string appName = activeWindow.AppName ?? string.Empty;

            if (appName.Equals(_ownProcessName, StringComparison.OrdinalIgnoreCase))
                return true;

            if (appName.IndexOf("ConcentrationTracker", StringComparison.OrdinalIgnoreCase) >= 0)
                return true;

            return false;
        }

        private void HandleOwnApplicationActivated(DateTime detectedAt)
        {
            FinishCurrentActivity(detectedAt);
            _currentActivityEvent = null;

            SuspendCurrentFocusBlock(detectedAt, FocusBreakReason.None);
            BeginIgnoringOwnApp(detectedAt);

            MonitoringStatus = LocalizationService.GetString("Monitoring_StatusViewingDashboard");

            CurrentApp = "Dashboard";
            CurrentWindowTitle = LocalizationService.GetString("Focus_DashboardExcluded");
            CurrentProcessPath = _ownProcessPath;
            CurrentAppUserModelId = string.Empty;
            CurrentPackageFullName = string.Empty;
            CurrentPackageInstallPath = string.Empty;
            CurrentCategory = AppCategory.System;

            UpdateChartData();
            RefreshCalculatedProperties();
        }

        private void BeginIgnoringOwnApp(DateTime startTime)
        {
            if (_isIgnoringOwnApp)
                return;

            _isIgnoringOwnApp = true;
            _ignoredOwnAppStartTime = startTime;
            OnPropertyChanged(nameof(IsViewingOwnApp));
        }

        private void EndIgnoringOwnApp(DateTime endTime)
        {
            if (!_isIgnoringOwnApp)
                return;

            TimeSpan ignoredDuration = endTime - _ignoredOwnAppStartTime;

            if (ignoredDuration.TotalSeconds > 0)
                _ignoredOwnAppTotalTime += ignoredDuration;

            _isIgnoringOwnApp = false;
            OnPropertyChanged(nameof(IsViewingOwnApp));
        }

        private void StartNewActivity(ActiveWindowInfo activeWindow, AppCategory category)
        {
            _currentActivityEvent = new ActivityEventModel
            {
                AppName = activeWindow.AppName,
                ProcessPath = activeWindow.ProcessPath,
                AppUserModelId = activeWindow.AppUserModelId,
                PackageFullName = activeWindow.PackageFullName,
                PackageInstallPath = activeWindow.PackageInstallPath,
                WindowTitle = activeWindow.WindowTitle,
                Category = category,
                StartTime = activeWindow.DetectedAt,
                EndTime = null,
                IsActive = true
            };

            ActivityEvents.Insert(0, _currentActivityEvent);
            OnPropertyChanged(nameof(VisibleActivityEvents));
        }

        private void FinishCurrentActivity(DateTime endTime)
        {
            if (_currentActivityEvent == null)
                return;

            _currentActivityEvent.EndTime = endTime;
            _currentActivityEvent.IsActive = false;
            _currentActivityEvent.RefreshDuration();
            OnPropertyChanged(nameof(VisibleActivityEvents));
        }

        private void RefreshCurrentActivityDuration()
        {
            if (_currentActivityEvent == null)
                return;

            _currentActivityEvent.RefreshDuration();
            OnPropertyChanged(nameof(VisibleActivityEvents));
        }

        private void UpdateFocusBlockState(ActiveWindowInfo activeWindow, AppCategory category)
        {
            if (category == AppCategory.Productive || category == AppCategory.Neutral)
            {
                StartOrResumeFocusBlock(activeWindow, category);
                return;
            }

            if (category == AppCategory.Distraction)
            {
                CompleteCurrentFocusBlock(
                    activeWindow.DetectedAt,
                    FocusBreakReason.Distraction,
                    true);

                return;
            }

            if (category == AppCategory.Communication)
            {
                CompleteCurrentFocusBlock(
                    activeWindow.DetectedAt,
                    FocusBreakReason.Communication,
                    true);

                return;
            }

            if (category == AppCategory.System)
            {
                SuspendCurrentFocusBlock(
                    activeWindow.DetectedAt,
                    FocusBreakReason.ContextChanged);
            }
        }

        private void StartOrResumeFocusBlock(ActiveWindowInfo activeWindow, AppCategory category)
        {
            DateTime now = activeWindow.DetectedAt;

            if (_currentFocusBlock == null)
            {
                CreateNewFocusBlock(activeWindow, category);
                return;
            }

            if (_currentFocusBlock.IsSuspended)
            {
                bool canResume =
                    !_focusSuspendedAt.HasValue ||
                    now - _focusSuspendedAt.Value <= _focusGracePeriod ||
                    _focusSuspensionReason == FocusBreakReason.None;

                if (canResume)
                {
                    _currentFocusBlock.LastWindowTitle = activeWindow.WindowTitle;
                    _currentFocusBlock.StartSegment(now);
                    _focusSuspendedAt = null;
                    _focusSuspensionReason = FocusBreakReason.None;
                    RefreshCalculatedProperties();
                    return;
                }

                CompleteCurrentFocusBlock(
                    _focusSuspendedAt.Value,
                    _focusSuspensionReason,
                    false);

                CreateNewFocusBlock(activeWindow, category);
                return;
            }

            if (_currentFocusBlock.IsActive)
            {
                _currentFocusBlock.LastWindowTitle = activeWindow.WindowTitle;
                _currentFocusBlock.RefreshDuration();
                RefreshCalculatedProperties();
                return;
            }

            CreateNewFocusBlock(activeWindow, category);
        }

        private void CreateNewFocusBlock(ActiveWindowInfo activeWindow, AppCategory category)
        {
            _currentFocusBlock = new FocusBlockModel
            {
                MainApp = activeWindow.AppName,
                LastWindowTitle = activeWindow.WindowTitle,
                Category = category,
                StartTime = activeWindow.DetectedAt,
                AccumulatedDuration = TimeSpan.Zero,
                EndTime = null,
                BreakReason = FocusBreakReason.None,
                IsActive = false,
                IsSuspended = false
            };

            _currentFocusBlock.StartSegment(activeWindow.DetectedAt);
            FocusBlocks.Insert(0, _currentFocusBlock);
            RefreshCalculatedProperties();
        }

        private void SuspendCurrentFocusBlock(DateTime suspendTime, FocusBreakReason reason)
        {
            if (_currentFocusBlock == null)
                return;

            if (!_currentFocusBlock.IsActive && !_currentFocusBlock.IsSuspended)
                return;

            if (_currentFocusBlock.IsActive)
                _currentFocusBlock.PauseSegment(suspendTime);

            _focusSuspendedAt = suspendTime;
            _focusSuspensionReason = reason;
            RefreshCalculatedProperties();
        }

        private void CompleteCurrentFocusBlock(DateTime endTime, FocusBreakReason reason, bool countAsInterruption)
        {
            if (_currentFocusBlock == null)
                return;

            TimeSpan durationBeforeComplete = GetFocusBlockDurationForMetrics();

            _currentFocusBlock.Complete(endTime, reason);

            if (countAsInterruption && durationBeforeComplete.TotalSeconds >= 5)
                InterruptionCount++;

            _currentFocusBlock = null;
            _focusSuspendedAt = null;
            _focusSuspensionReason = FocusBreakReason.None;
            RefreshCalculatedProperties();
        }

        private void RefreshCurrentFocusBlockDuration()
        {
            if (_currentFocusBlock == null)
                return;

            _currentFocusBlock.RefreshDuration();
        }

        private TimeSpan GetCurrentFocusStreak()
        {
            if (_currentFocusBlock == null)
                return TimeSpan.Zero;

            if (_currentFocusBlock.IsActive || _currentFocusBlock.IsSuspended)
                return GetFocusBlockDurationForMetrics();

            return TimeSpan.Zero;
        }

        private TimeSpan GetFocusBlockDurationForMetrics()
        {
            if (_currentFocusBlock == null)
                return TimeSpan.Zero;

            return _currentFocusBlock.CurrentDuration;
        }

        private TimeSpan GetFocusBlockDurationForDisplay(FocusBlockModel focusBlock)
        {
            if (focusBlock == null)
                return TimeSpan.Zero;

            if (focusBlock == _currentFocusBlock)
                return GetFocusBlockDurationForMetrics();

            return focusBlock.CurrentDuration;
        }

        private void UpdateAppUsage(string appName, AppCategory category)
        {
            AppUsageModel existingApp = null;

            foreach (AppUsageModel app in AppUsageList)
            {
                if (app.AppName == appName)
                {
                    existingApp = app;
                    break;
                }
            }

            if (existingApp != null)
            {
                existingApp.SwitchCount++;
                existingApp.Category = category;
            }
            else
            {
                AppUsageList.Add(
                    new AppUsageModel
                    {
                        AppName = appName,
                        SwitchCount = 1,
                        Category = category
                    });
            }
        }

        private bool ShouldShowActivityEvent(ActivityEventModel activityEvent)
        {
            if (activityEvent == null)
                return false;

            if (activityEvent.IsActive)
                return true;

            if (activityEvent.CurrentDuration.TotalSeconds < 0.5)
                return false;

            return true;
        }

        private AppCategory RefineBrowserCategory(
            ActiveWindowInfo activeWindow,
            AppCategory fallbackCategory)
        {
            if (activeWindow == null ||
                !IsBrowserAppName(activeWindow.AppName))
            {
                return fallbackCategory;
            }

            AppCategory userRuleCategory;

            if (_categoryRuleService.TryClassify(
                    activeWindow.AppName,
                    activeWindow.WindowTitle,
                    out userRuleCategory))
            {
                return userRuleCategory;
            }

            string title =
                (activeWindow.WindowTitle ?? string.Empty).ToLowerInvariant();

            if (string.IsNullOrWhiteSpace(title))
                return fallbackCategory;

            if (title.Contains("github") ||
                title.Contains("stackoverflow") ||
                title.Contains("stack overflow") ||
                title.Contains("microsoft learn") ||
                title.Contains("documentation") ||
                title.Contains("docs.microsoft") ||
                title.Contains("google docs") ||
                title.Contains("figma"))
            {
                return AppCategory.Productive;
            }

            if (title.Contains("gmail") ||
                title.Contains("outlook") ||
                title.Contains("mail") ||
                title.Contains("slack") ||
                title.Contains("discord") ||
                title.Contains("telegram"))
            {
                return AppCategory.Communication;
            }

            if (title.Contains("youtube") ||
                title.Contains("tiktok") ||
                title.Contains("instagram") ||
                title.Contains("facebook") ||
                title.Contains("netflix") ||
                title.Contains("twitch") ||
                title.Contains("reddit"))
            {
                return AppCategory.Distraction;
            }

            return fallbackCategory;
        }

        private TimeSpan GetAfkDuration()
        {
            if (!_hasSessionStarted)
                return TimeSpan.Zero;

            TimeSpan afkDuration =
                _totalIdleTime;

            if (IsIdle && _idleStartedAt.HasValue && IsMonitoring)
            {
                TimeSpan currentIdleDuration =
                    DateTime.Now - _idleStartedAt.Value;

                if (currentIdleDuration.TotalSeconds > 0)
                    afkDuration += currentIdleDuration;
            }

            if (afkDuration.TotalSeconds < 0)
                return TimeSpan.Zero;

            return afkDuration;
        }

        private void UpdateTimeDisplays()
        {
            SessionTime = GetSessionElapsedTime().ToString(@"hh\:mm\:ss");
            TrackedTime = GetTrackedElapsedTime().ToString(@"hh\:mm\:ss");
        }

        private TimeSpan GetSessionElapsedTime()
        {
            if (!_hasSessionStarted)
                return TimeSpan.Zero;

            if (IsMonitoring)
            {
                return _elapsedBeforeCurrentRun +
                       (DateTime.Now - _currentRunStartTime);
            }

            return _elapsedBeforeCurrentRun;
        }

        private TimeSpan GetTrackedElapsedTime()
        {
            if (!_hasSessionStarted)
                return TimeSpan.Zero;

            TimeSpan rawElapsed = GetSessionElapsedTime();
            TimeSpan ignoredTime = _ignoredOwnAppTotalTime;

            if (_isIgnoringOwnApp && IsMonitoring)
                ignoredTime += DateTime.Now - _ignoredOwnAppStartTime;

            TimeSpan idleTime = _totalIdleTime;

            if (IsIdle && _idleStartedAt.HasValue && IsMonitoring)
                idleTime += DateTime.Now - _idleStartedAt.Value;

            TimeSpan result = rawElapsed - ignoredTime - idleTime;

            if (result.TotalSeconds < 0)
                return TimeSpan.Zero;

            return result;
        }

        private DateTime GetMetricsNow()
        {
            if (IsIdle && _idleStartedAt.HasValue)
                return _idleStartedAt.Value;

            if (IsMonitoring)
                return DateTime.Now;

            return _pausedAt;
        }

        private string FormatDurationShort(TimeSpan duration)
        {
            if (duration.TotalSeconds < 1)
                return LocalizationService.GetString("Duration_Zero");

            int hours =
                (int)duration.TotalHours;

            int minutes =
                duration.Minutes;

            int seconds =
                duration.Seconds;

            if (hours > 0)
            {
                return LocalizationService.Format(
                    "Duration_Hours",
                    hours,
                    minutes,
                    seconds);
            }

            if (minutes > 0)
            {
                return LocalizationService.Format(
                    "Duration_Minutes",
                    minutes,
                    seconds);
            }

            return LocalizationService.Format(
                "Duration_Seconds",
                seconds);
        }

        private void RefreshCalculatedProperties()
        {
            OnPropertyChanged(nameof(AvgSwitchesPerHour));
            OnPropertyChanged(nameof(FocusStreak));
            OnPropertyChanged(nameof(LastFocusBlock));
            OnPropertyChanged(nameof(BestFocusBlock));
            OnPropertyChanged(nameof(IsViewingOwnApp));
            OnPropertyChanged(nameof(ProductiveTime));

            OnPropertyChanged(nameof(ProductiveDuration));
            OnPropertyChanged(nameof(NeutralDuration));
            OnPropertyChanged(nameof(CommunicationDuration));
            OnPropertyChanged(nameof(DistractionDuration));
            OnPropertyChanged(nameof(MostUsedApp));
            OnPropertyChanged(nameof(TopDistraction));
            OnPropertyChanged(nameof(MainWorkContext));
            OnPropertyChanged(nameof(MostUsedAppMetric));
            OnPropertyChanged(nameof(MainWorkContextMetric));
            OnPropertyChanged(nameof(TopDistractionMetric));
            OnPropertyChanged(nameof(TopInterrupterMetric));

            OnPropertyChanged(nameof(DisruptiveSwitches));
            OnPropertyChanged(nameof(TopInterrupter));
            OnPropertyChanged(nameof(SessionInsight));
            OnPropertyChanged(nameof(AwayTime));
            OnPropertyChanged(nameof(AfkSessions));
            OnPropertyChanged(nameof(AwaySummary));
            OnPropertyChanged(nameof(AfkTime));
            OnPropertyChanged(nameof(IdleBreaks));

            OnPropertyChanged(nameof(ProductiveBarWidth));
            OnPropertyChanged(nameof(NeutralBarWidth));
            OnPropertyChanged(nameof(CommunicationBarWidth));
            OnPropertyChanged(nameof(DistractionBarWidthSummary));
            OnPropertyChanged(nameof(SummaryStackBarWidth));
            OnPropertyChanged(nameof(ProductiveStackWidth));
            OnPropertyChanged(nameof(NeutralStackWidth));
            OnPropertyChanged(nameof(CommunicationStackWidth));
            OnPropertyChanged(nameof(DistractionStackWidth));
            OnPropertyChanged(nameof(AfkStackWidth));

            OnPropertyChanged(nameof(ConcentrationScore));
            OnPropertyChanged(nameof(DeepFocusPercent));
            OnPropertyChanged(nameof(DistractionControlPercent));
            OnPropertyChanged(nameof(DeepFocusBarWidth));
            OnPropertyChanged(nameof(DistractionBarWidth));

            OnPropertyChanged(nameof(CurrentProcessPath));
            OnPropertyChanged(nameof(CurrentAppUserModelId));
            OnPropertyChanged(nameof(CurrentPackageFullName));
            OnPropertyChanged(nameof(CurrentPackageInstallPath));
            OnPropertyChanged(nameof(CurrentCategory));
            OnPropertyChanged(nameof(CurrentAppDisplay));
            OnPropertyChanged(nameof(VisibleActivityEvents));
            OnPropertyChanged(nameof(SwitchesPlotModel));
            OnPropertyChanged(nameof(ScorePlotModel));
            OnPropertyChanged(nameof(CategoryRules));
            OnPropertyChanged(nameof(AvailableCategories));
            OnPropertyChanged(nameof(CategoryRulesStatus));
            OnPropertyChanged(nameof(SessionHistory));
            OnPropertyChanged(nameof(SelectedSessionRecord));
            OnPropertyChanged(nameof(SessionHistoryStatus));
            OnPropertyChanged(nameof(SavedSessionsCount));
            OnPropertyChanged(nameof(AverageSavedScore));
            OnPropertyChanged(nameof(LastSavedSessionText));
        }

        private class SwitchEventModel
        {
            public DateTime Timestamp { get; set; }
            public AppCategory FromCategory { get; set; }
            public AppCategory ToCategory { get; set; }
            public string ToAppName { get; set; }
            public string ToWindowTitle { get; set; }
            public bool IsDisruptive { get; set; }
        }

        private class ScoreSampleModel
        {
            public DateTime CapturedAt { get; set; }
            public int Score { get; set; }
        }
    }
}