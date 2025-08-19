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
using System.Windows.Shapes;

namespace WpfApp1
{
    /// <summary>
    /// Логика взаимодействия для MainLoginWindow.xaml
    /// </summary>
    public partial class MainLoginWindow : Window
    {
        public MainLoginWindow()
        {
            InitializeComponent();
        }

        private void AdminButton_Click(object sender, RoutedEventArgs e)
        {
            loginPage loginWindow = new loginPage();
            Application.Current.MainWindow = loginWindow;
            loginWindow.Show();
            this.Close();
        }

        private void GarconButton_Click(object sender, RoutedEventArgs e)
        {
            GarconWindow loginWindow = new GarconWindow();
            Application.Current.MainWindow = loginWindow;
            loginWindow.Show();
            this.Close();
        }

        private void CookButton_Click(object sender, RoutedEventArgs e)
        {
            KitchenWindow loginWindow = new KitchenWindow(false);
            Application.Current.MainWindow = loginWindow;
            loginWindow.Show();
            this.Close();
        }

        private void BarButton_Click(object sender, RoutedEventArgs e)
        {
            KitchenWindow loginWindow = new KitchenWindow(false,true);
            Application.Current.MainWindow = loginWindow;
            loginWindow.Show();
            this.Close();
        }
    }
}
