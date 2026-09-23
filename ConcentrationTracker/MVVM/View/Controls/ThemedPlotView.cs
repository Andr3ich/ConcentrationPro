using OxyPlot;
using OxyPlot.Wpf;
using System;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;

namespace ConcentrationTracker.MVVM.View.Controls
{
    public class ThemedPlotView : PlotView
    {
        private readonly DispatcherTimer _themeSignatureTimer;
        private readonly DispatcherTimer _restoreVisibilityTimer;

        private string _lastThemeSignature;
        private EventInfo _themeChangingEvent;
        private EventInfo _themeChangedEvent;
        private Delegate _themeChangingHandler;
        private Delegate _themeChangedHandler;

        public ThemedPlotView()
        {
            Background =
                Brushes.Transparent;

            Opacity =
                1.0;

            Loaded +=
                ThemedPlotView_Loaded;

            Unloaded +=
                ThemedPlotView_Unloaded;

            DataContextChanged +=
                ThemedPlotView_DataContextChanged;

            _themeSignatureTimer =
                new DispatcherTimer
                {
                    Interval =
                        TimeSpan.FromMilliseconds(350)
                };

            _themeSignatureTimer.Tick +=
                ThemeSignatureTimer_Tick;

            _restoreVisibilityTimer =
                new DispatcherTimer
                {
                    Interval =
                        TimeSpan.FromMilliseconds(180)
                };

            _restoreVisibilityTimer.Tick +=
                RestoreVisibilityTimer_Tick;
        }

        protected override void OnPropertyChanged(
            DependencyPropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(
                e);

            if (e.Property != null &&
                e.Property.Name == "Model")
            {
                ApplyThemeToPlot();
            }
        }

        private void ThemedPlotView_Loaded(
            object sender,
            RoutedEventArgs e)
        {
            SubscribeToThemeServiceEvents();

            ApplyThemeToPlot();

            _lastThemeSignature =
                GetCurrentThemeSignature();

            _themeSignatureTimer.Start();
        }

        private void ThemedPlotView_Unloaded(
            object sender,
            RoutedEventArgs e)
        {
            _themeSignatureTimer.Stop();
            _restoreVisibilityTimer.Stop();

            UnsubscribeFromThemeServiceEvents();
        }

        private void ThemedPlotView_DataContextChanged(
            object sender,
            DependencyPropertyChangedEventArgs e)
        {
            ApplyThemeToPlot();
        }

        private void ThemeSignatureTimer_Tick(
            object sender,
            EventArgs e)
        {
            string currentSignature =
                GetCurrentThemeSignature();

            if (string.Equals(
                    _lastThemeSignature,
                    currentSignature,
                    StringComparison.Ordinal))
            {
                return;
            }

            BeginThemeTransition();
            EndThemeTransition();
        }

        private void ThemeService_ThemeChanging(
            object sender,
            EventArgs e)
        {
            BeginThemeTransition();
        }

        private void ThemeService_ThemeChanged(
            object sender,
            EventArgs e)
        {
            EndThemeTransition();
        }

        private void BeginThemeTransition()
        {
            _restoreVisibilityTimer.Stop();

            Opacity =
                0.0;

            IsHitTestVisible =
                false;

            ApplyNeutralTransparentSurface();
        }

        private void EndThemeTransition()
        {
            Dispatcher.BeginInvoke(
                new Action(ApplyThemeToPlot),
                DispatcherPriority.Send);

            Dispatcher.BeginInvoke(
                new Action(ApplyThemeToPlot),
                DispatcherPriority.Render);

            _restoreVisibilityTimer.Stop();
            _restoreVisibilityTimer.Start();
        }

        private void RestoreVisibilityTimer_Tick(
            object sender,
            EventArgs e)
        {
            _restoreVisibilityTimer.Stop();

            ApplyThemeToPlot();

            Opacity =
                1.0;

            IsHitTestVisible =
                true;
        }

        private void ApplyNeutralTransparentSurface()
        {
            if (Model == null)
                return;

            Model.Background =
                OxyColors.Transparent;

            Model.PlotAreaBackground =
                OxyColors.Transparent;

            Model.InvalidatePlot(
                false);
        }

        private void ApplyThemeToPlot()
        {
            if (Model == null)
                return;

            OxyColor primaryText =
                GetOxyColor(
                    "TextPrimaryBrush",
                    OxyColors.White);

            OxyColor secondaryText =
                GetOxyColor(
                    "TextSecondaryBrush",
                    OxyColor.FromRgb(220, 228, 242));

            OxyColor mutedText =
                GetOxyColor(
                    "TextMutedBrush",
                    OxyColor.FromRgb(174, 188, 208));

            OxyColor border =
                GetOxyColor(
                    "BorderBrush",
                    OxyColor.FromRgb(47, 62, 86));

            OxyColor divider =
                GetOxyColor(
                    "DividerBrush",
                    OxyColor.FromRgb(51, 65, 85));

            Model.TextColor =
                secondaryText;

            Model.TitleColor =
                primaryText;

            Model.SubtitleColor =
                secondaryText;

            Model.PlotAreaBorderColor =
                border;

            Model.Background =
                OxyColors.Transparent;

            Model.PlotAreaBackground =
                OxyColors.Transparent;

            SetOxyColorProperty(
                Model,
                "LegendTextColor",
                secondaryText);

            SetOxyColorProperty(
                Model,
                "LegendTitleColor",
                primaryText);

            foreach (object axis in Model.Axes)
            {
                SetOxyColorProperty(
                    axis,
                    "TextColor",
                    secondaryText);

                SetOxyColorProperty(
                    axis,
                    "TitleColor",
                    secondaryText);

                SetOxyColorProperty(
                    axis,
                    "TicklineColor",
                    mutedText);

                SetOxyColorProperty(
                    axis,
                    "AxislineColor",
                    border);

                SetOxyColorProperty(
                    axis,
                    "MajorGridlineColor",
                    divider);

                SetOxyColorProperty(
                    axis,
                    "MinorGridlineColor",
                    divider);
            }

            foreach (object series in Model.Series)
            {
                SetOxyColorProperty(
                    series,
                    "TextColor",
                    primaryText);

                SetOxyColorProperty(
                    series,
                    "LabelColor",
                    primaryText);

                SetOxyColorProperty(
                    series,
                    "TrackerTextColor",
                    primaryText);
            }

            foreach (object annotation in Model.Annotations)
            {
                SetOxyColorProperty(
                    annotation,
                    "TextColor",
                    primaryText);

                SetOxyColorProperty(
                    annotation,
                    "Color",
                    primaryText);

                SetOxyColorProperty(
                    annotation,
                    "Stroke",
                    mutedText);
            }

            _lastThemeSignature =
                GetCurrentThemeSignature();

            Model.InvalidatePlot(
                false);
        }

        private string GetCurrentThemeSignature()
        {
            Color background =
                GetWpfColor(
                    "SurfaceSoftBrush",
                    Colors.Transparent);

            Color text =
                GetWpfColor(
                    "TextPrimaryBrush",
                    Colors.Transparent);

            Color border =
                GetWpfColor(
                    "BorderBrush",
                    Colors.Transparent);

            return
                background.ToString() +
                "|" +
                text.ToString() +
                "|" +
                border.ToString();
        }

        private OxyColor GetOxyColor(
            string resourceKey,
            OxyColor fallback)
        {
            Color fallbackColor =
                Color.FromArgb(
                    fallback.A,
                    fallback.R,
                    fallback.G,
                    fallback.B);

            Color color =
                GetWpfColor(
                    resourceKey,
                    fallbackColor);

            return OxyColor.FromArgb(
                color.A,
                color.R,
                color.G,
                color.B);
        }

        private Color GetWpfColor(
            string resourceKey,
            Color fallback)
        {
            object resource =
                TryFindResource(
                    resourceKey);

            SolidColorBrush brush =
                resource as SolidColorBrush;

            if (brush == null)
                return fallback;

            return brush.Color;
        }

        private void SetOxyColorProperty(
            object target,
            string propertyName,
            OxyColor value)
        {
            if (target == null)
                return;

            PropertyInfo propertyInfo =
                target
                    .GetType()
                    .GetProperty(
                        propertyName,
                        BindingFlags.Public | BindingFlags.Instance);

            if (propertyInfo == null ||
                !propertyInfo.CanWrite ||
                propertyInfo.PropertyType != typeof(OxyColor))
            {
                return;
            }

            propertyInfo.SetValue(
                target,
                value,
                null);
        }

        private void SubscribeToThemeServiceEvents()
        {
            Type themeServiceType =
                GetThemeServiceType();

            if (themeServiceType == null)
                return;

            _themeChangingEvent =
                themeServiceType.GetEvent(
                    "ThemeChanging",
                    BindingFlags.Public | BindingFlags.Static);

            _themeChangedEvent =
                themeServiceType.GetEvent(
                    "ThemeChanged",
                    BindingFlags.Public | BindingFlags.Static);

            if (_themeChangingEvent != null)
            {
                _themeChangingHandler =
                    Delegate.CreateDelegate(
                        _themeChangingEvent.EventHandlerType,
                        this,
                        "ThemeService_ThemeChanging",
                        false);

                if (_themeChangingHandler != null)
                {
                    _themeChangingEvent.AddEventHandler(
                        null,
                        _themeChangingHandler);
                }
            }

            if (_themeChangedEvent != null)
            {
                _themeChangedHandler =
                    Delegate.CreateDelegate(
                        _themeChangedEvent.EventHandlerType,
                        this,
                        "ThemeService_ThemeChanged",
                        false);

                if (_themeChangedHandler != null)
                {
                    _themeChangedEvent.AddEventHandler(
                        null,
                        _themeChangedHandler);
                }
            }
        }

        private void UnsubscribeFromThemeServiceEvents()
        {
            if (_themeChangingEvent != null &&
                _themeChangingHandler != null)
            {
                _themeChangingEvent.RemoveEventHandler(
                    null,
                    _themeChangingHandler);
            }

            if (_themeChangedEvent != null &&
                _themeChangedHandler != null)
            {
                _themeChangedEvent.RemoveEventHandler(
                    null,
                    _themeChangedHandler);
            }

            _themeChangingEvent =
                null;

            _themeChangedEvent =
                null;

            _themeChangingHandler =
                null;

            _themeChangedHandler =
                null;
        }

        private Type GetThemeServiceType()
        {
            return AppDomain
                .CurrentDomain
                .GetAssemblies()
                .Select(
                    assembly =>
                        assembly.GetType(
                            "ConcentrationTracker.Core.Services.ThemeService",
                            false))
                .FirstOrDefault(
                    type =>
                        type != null);
        }
    }
}
