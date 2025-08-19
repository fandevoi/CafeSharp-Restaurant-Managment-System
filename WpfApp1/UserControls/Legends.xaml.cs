using System.Windows;
using System.Windows.Controls;

namespace Sales_Dashboard.UserControls
{
    public partial class Legends : UserControl
    {
        public Legends()
        {
            InitializeComponent();
        }
        public string FirstMonth
        {
            get { return (string)GetValue(FirstMonthProperty); }
            set { SetValue(FirstMonthProperty, value); }
        }

        public static readonly DependencyProperty FirstMonthProperty = DependencyProperty.Register("FirstMonth", typeof(string), typeof(Legends));
        public static readonly DependencyProperty SecondMonthProperty = DependencyProperty.Register("SecondMonth", typeof(string), typeof(Legends));
        public static readonly DependencyProperty ThirdMonthProperty = DependencyProperty.Register("ThirdMonth", typeof(string), typeof(Legends));

        public string SecondMonth
        {
            get { return (string)GetValue(SecondMonthProperty); }
            set { SetValue(SecondMonthProperty, value); }
        }

        public string ThirdMonth
        {
            get { return (string)GetValue(ThirdMonthProperty); }
            set { SetValue(ThirdMonthProperty, value); }
        }
    }
}