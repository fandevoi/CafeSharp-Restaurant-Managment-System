using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
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
    public partial class tablesEditTable : Window
    {
        private SqlConnection connection = new SqlConnection(ConfigurationManager.ConnectionStrings["constr"].ConnectionString);
        public string Number { get; set; }
        public string Status { get; set; }
       
        public string Customer { get; set; }
        public string Phone { get; set; }
        public string StartTime { get; set; }
        public string EndTime { get; set; }

        DateTime now = DateTime.Now;
        private void Border_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                this.DragMove();
            }
        }
        public tablesEditTable(string NumberValue, string StatusValue)
        {
            InitializeComponent();
            DataContext = this;
            Number = NumberValue;
            Status = StatusValue;
            if (Status == "Забронирован")
            {
                getTable();
            }
            else if (Status == "Свободен")
            {
                this.Height = 280;
                reservationStackPanel.Visibility = Visibility.Collapsed;
                statusTextBlock.Text = "Свободен";
                busyButton.Visibility = Visibility.Visible;
                statusTextBlock.Visibility = Visibility.Visible;
            }
            else if (Status == "Занят")
            {
                this.Height = 280;
                reservationStackPanel.Visibility = Visibility.Collapsed;
                statusTextBlock.Text = "Занят";
                unBusyButton.Visibility = Visibility.Visible;
                statusTextBlock.Visibility = Visibility.Visible;
            }
        }
        private void getTable ()
        {
            DataContext = this;
            try
            {
                connection.Open();
                SqlCommand cmd = new SqlCommand($"SELECT * FROM Reservation WHERE Reservation_Table = '{Number}' AND CONVERT(DATE, Reservation_Date, 104) = '{now.Year}-{now.Month}-{now.Day}' AND Reservation_Status = 'Активна' AND CONVERT(TIME, GETDATE()) >= Reservation_Start AND CONVERT(TIME, GETDATE()) <= Reservation_End;", connection);
                SqlDataReader rdr = cmd.ExecuteReader();
                while (rdr.Read())
                {
                    Customer = rdr["Reservation_Customer"].ToString();
                    Phone = rdr["Reservation_Phone"].ToString();
                    StartTime = rdr["Reservation_Start"].ToString();
                    EndTime = rdr["Reservation_End"].ToString();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message);
            }
            finally
            {
                connection.Close();
            }
        }

        private void clearButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                connection.Open();
                    DateTime currentDateTime = DateTime.Now;
                    string query = $"UPDATE Reservation SET Reservation_Status='Закрыта'" +
                                   $"WHERE Reservation_Table = {Number} " +
                                   $"AND Reservation_Date = '{currentDateTime.Date:yyyy-MM-dd}' " +
                                   $"AND '{currentDateTime:HH:mm:ss}' BETWEEN Reservation_Start AND Reservation_End AND Reservation_Status='Активна'";
                    SqlCommand command = new SqlCommand(query, connection);
                    command.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message);
            }
            finally
            {
                connection.Close();
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                connection.Open();
                string query = $"UPDATE Tables SET Tables_Status='Занят' WHERE Tables_ID = '{Number}';";
                SqlCommand command = new SqlCommand(query, connection);
                command.ExecuteNonQuery();
                MessageBox.Show("Стол успешно занят!", "Стол занят", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message);
            }
            finally
            {

                connection.Close();
                this.Close();
            }
        }

        private void unBusyButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                connection.Open();
                DateTime currentDateTime = DateTime.Now;
                string query = $"UPDATE Tables SET Tables_Status='Свободен'" +
                               $"WHERE Tables_ID = {Number};";
                SqlCommand command = new SqlCommand(query, connection);
                command.ExecuteNonQuery();
                MessageBox.Show("Стол успешно освобождён!", "Стол освобождён", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message);
            }
            finally
            {
                connection.Close();
                this.Close();
            }
        }
    }
}
