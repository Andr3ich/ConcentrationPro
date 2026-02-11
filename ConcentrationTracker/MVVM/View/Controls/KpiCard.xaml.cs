using System.Windows;
using System.Windows.Controls;

namespace ConcentrationTracker.MVVM.View.Controls
{
    public partial class KpiCard : UserControl
    {
        public KpiCard()
        {
            InitializeComponent();
        }

        public string Title
        {
            get { return (string)GetValue(TitleProperty); }
            set { SetValue(TitleProperty, value); }
        }

        public static readonly DependencyProperty TitleProperty =
            DependencyProperty.Register("Title", typeof(string), typeof(KpiCard), new PropertyMetadata("Title"));

        public string Value
        {
            get { return (string)GetValue(ValueProperty); }
            set { SetValue(ValueProperty, value); }
        }

        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register("Value", typeof(string), typeof(KpiCard), new PropertyMetadata("0"));
    }
}
