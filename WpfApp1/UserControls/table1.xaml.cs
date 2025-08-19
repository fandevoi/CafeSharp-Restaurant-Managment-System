using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using WpfApp1;

namespace Sales_Dashboard.UserControls
{
    public partial class table1 : UserControl
    {
        public string Number
        {
            get { return (string)GetValue(NumberProperty); }
            set { SetValue(NumberProperty, value); }
        }
        public double CustomWidth
        {
            get { return (double)GetValue(CustomWidthProperty); }
            set { SetValue(CustomWidthProperty, value); }
        }
        public double BigBorderWidth
        {
            get { return (double)GetValue(BigBorderWidthProperty); }
            set { SetValue(BigBorderWidthProperty, value); }
        }
        public double SmallBorderWidth
        {
            get { return (double)GetValue(SmallBorderWidthProperty); }
            set { SetValue(SmallBorderWidthProperty, value); }
        }
       
        public string Status
        {
            get { return (string)GetValue(StatusProperty); }
            set { SetValue(StatusProperty, value); }
        }
       
        public static readonly DependencyProperty NumberProperty = DependencyProperty.Register("Number", typeof(string), typeof(table1));
        public static readonly DependencyProperty StatusProperty = DependencyProperty.Register("Status", typeof(string), typeof(table1));
        public static readonly DependencyProperty CustomWidthProperty = DependencyProperty.Register("CustomWidth", typeof(double), typeof(table1));
        public static readonly DependencyProperty BigBorderWidthProperty = DependencyProperty.Register("BigBorderWidth", typeof(double), typeof(table1));
        public static readonly DependencyProperty SmallBorderWidthProperty = DependencyProperty.Register("SmallBorderWidth", typeof(double), typeof(table1));
        public table1()
        {
            InitializeComponent();

            buttonTable.Click += ButtonTable_Click;
        }
        private void ButtonTable_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button)
            {
                Window window = Window.GetWindow(button);
                if (window is MainWindow)
                {
                    tablesEditTable secondWindow = new tablesEditTable(Number, Status);
                    secondWindow.ShowDialog();
                }
                else if (window is GarconWindow garconWindow) 
                {
                    garconWindow.ChangeTableState(Number, Status);
                }
            }
        }

    }
}
