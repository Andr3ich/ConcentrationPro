using MahApps.Metro.IconPacks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ConcentrationTracker.MVVM.View.Controls
{
    public partial class KpiCard : UserControl
    {
        public static readonly DependencyProperty TitleProperty =
            DependencyProperty.Register(
                nameof(Title),
                typeof(string),
                typeof(KpiCard),
                new PropertyMetadata(string.Empty));

        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register(
                nameof(Value),
                typeof(string),
                typeof(KpiCard),
                new PropertyMetadata(string.Empty));

        public static readonly DependencyProperty IconKindProperty =
            DependencyProperty.Register(
                nameof(IconKind),
                typeof(PackIconMaterialKind),
                typeof(KpiCard),
                new PropertyMetadata(PackIconMaterialKind.ChartBoxOutline));

        public static readonly DependencyProperty IconBrushProperty =
            DependencyProperty.Register(
                nameof(IconBrush),
                typeof(Brush),
                typeof(KpiCard),
                new PropertyMetadata(null));

        public static readonly DependencyProperty IconBackgroundProperty =
            DependencyProperty.Register(
                nameof(IconBackground),
                typeof(Brush),
                typeof(KpiCard),
                new PropertyMetadata(null));

        public string Title
        {
            get { return (string)GetValue(TitleProperty); }
            set { SetValue(TitleProperty, value); }
        }

        public string Value
        {
            get { return (string)GetValue(ValueProperty); }
            set { SetValue(ValueProperty, value); }
        }

        public PackIconMaterialKind IconKind
        {
            get { return (PackIconMaterialKind)GetValue(IconKindProperty); }
            set { SetValue(IconKindProperty, value); }
        }

        public Brush IconBrush
        {
            get { return (Brush)GetValue(IconBrushProperty); }
            set { SetValue(IconBrushProperty, value); }
        }

        public Brush IconBackground
        {
            get { return (Brush)GetValue(IconBackgroundProperty); }
            set { SetValue(IconBackgroundProperty, value); }
        }

        public KpiCard()
        {
            InitializeComponent();
        }
    }
}
