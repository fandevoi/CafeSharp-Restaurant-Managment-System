using MySqlX.XDevAPI.Common;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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
    public class Reservations
    {
        public int Reservation_ID { get; set; }
        public int Reservation_Table { get; set; }
        public string Reservation_Customer { get; set; }
        public string Reservation_Phone { get; set; }
        public string Reservation_Status { get; set; }
        public string Reservation_Time { get; set; }
    }
    
    public partial class tablesReservations : Window
    {
        private int tmpID { get; set; }
        DateTime selectedDate { get; set; }
        string formattedDate { get; set; }

        private SqlConnection connection = new SqlConnection(ConfigurationManager.ConnectionStrings["constr"].ConnectionString);

        DateTime now = DateTime.Now;
        public tablesReservations()
        {
            InitializeComponent();
            getReservations($"{now.Year}-{now.Month}-{now.Day}");
            formattedDate = $"{now.Year}-{now.Month}-{now.Day}";
            fillStartBox();
            updateReservations();
        }
        private void getReservations (string formattedDate)
        {
            DataContext = this;
            List<Reservations> Reservations = new List<Reservations>();
            try
            {
                connection.Open();
                SqlCommand cmd = new SqlCommand($"SELECT * FROM Reservation WHERE CONVERT(DATE, Reservation_Date, 104) = '{formattedDate}' ORDER BY Reservation_Start DESC;", connection);
                SqlDataReader rdr = cmd.ExecuteReader();
                while (rdr.Read())
                {
                    string time = $"{rdr["Reservation_Start"].ToString().Substring(0, 5)} - {rdr["Reservation_End"].ToString().Substring(0, 5)}";
                    Reservations tableFiller = new Reservations
                    {
                        Reservation_ID = Convert.ToInt32(rdr["Reservation_ID"]),
                        Reservation_Table = Convert.ToInt32(rdr["Tables_ID"]),
                        Reservation_Customer = rdr["Customer_Name"].ToString(),
                        Reservation_Phone = rdr["Customer_Phone"].ToString(),
                        Reservation_Status = rdr["Reservation_Status"].ToString(),
                        Reservation_Time = time
                    };
                    Reservations.Add(tableFiller);
                }
                reservationsGrid.ItemsSource = Reservations;
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
        private void updateReservations ()
        {
            try
            {
                connection.Open();
                SqlCommand cmd = new SqlCommand($"UPDATE Reservation SET Reservation_Status='Закрыта' WHERE Reservation_Date < '{now.Year}-{now.Month}-{now.Day}' OR (Reservation_Date = '{now.Year}-{now.Month}-{now.Day}' AND Reservation_End <= CONVERT(TIME, GETDATE()));", connection);
                cmd.ExecuteScalar();
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
       
        private void fillStartBox ()
        {
            List<string> times = new List<string>();
            TimeSpan startTime = new TimeSpan(10, 0, 0);
            TimeSpan endTime = new TimeSpan(21, 0, 0);
            TimeSpan step = new TimeSpan(0, 30, 0);
            for (TimeSpan currentTime = startTime; currentTime <= endTime; currentTime += step)
            {
                times.Add(currentTime.ToString(@"hh\:mm")); 
            }
            timeStartComboBox.ItemsSource = times;
            addTimeStartComboBox.ItemsSource = times;
        }
        private void datePicker_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            updateReservations();
            selectedDate = datePicker.SelectedDate.GetValueOrDefault();
            formattedDate = selectedDate.ToString("yyyy-MM-dd");
            getReservations(formattedDate);

        }
        private void ComboBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            string currentText = ((ComboBox)sender).Text;

            string newText = currentText + e.Text;

            if (!int.TryParse(newText, out int result))
            {
                e.Handled = true; 
            }
            else
            {
              
                if (result <= 0 || result > 20 || newText.Length > 2)
                {
                    e.Handled = true; 
                }
            }
        }

        private void timeStartComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            timeEndComboBox.ItemsSource = null;
            addTimeEndComboBox.ItemsSource = null;
            if (timeStartComboBox.SelectedItem != null)
            {
                string selectedTime = timeStartComboBox.SelectedItem.ToString();
                List<string> endTimes = new List<string>();
                TimeSpan selectedStartTime = TimeSpan.Parse(selectedTime);
                for (TimeSpan currentTime = selectedStartTime.Add(new TimeSpan(0, 30, 0)); currentTime <= new TimeSpan(22, 0, 0); currentTime += new TimeSpan(0, 30, 0))
                {
                    endTimes.Add(currentTime.ToString(@"hh\:mm"));
                }
                timeEndComboBox.ItemsSource = endTimes;
            }
            if (addTimeStartComboBox.SelectedItem != null)
            {
                string selectedTime = addTimeStartComboBox.SelectedItem.ToString();
                List<string> endTimes = new List<string>();
                TimeSpan selectedStartTime = TimeSpan.Parse(selectedTime);
                for (TimeSpan currentTime = selectedStartTime.Add(new TimeSpan(0, 30, 0)); currentTime <= new TimeSpan(22, 0, 0); currentTime += new TimeSpan(0, 30, 0))
                {
                    endTimes.Add(currentTime.ToString(@"hh\:mm"));
                }
                addTimeEndComboBox.ItemsSource = endTimes;
            }
        }

        private void reservationsGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (reservationsGrid.SelectedItem != null)
            {
                Reservations selectedReservation = (Reservations)reservationsGrid.SelectedItem;
                string table = selectedReservation.Reservation_Table.ToString();
                tableComboBox.Text = table;
                string customer = selectedReservation.Reservation_Customer;
                customerTextBox.Text = customer;
                string phone = selectedReservation.Reservation_Phone;
                phoneTextBox.Text = phone;
                string time = selectedReservation.Reservation_Time;
                timeStartComboBox.Text = time.Substring(0, 5);
                timeEndComboBox.Text = time.Substring(8, 5);
                tmpID = selectedReservation.Reservation_ID;
                if (selectedReservation.Reservation_Status == "Активна")
                {
                    statusRadioButton1.IsChecked = true;
                    statusRadioButton2.IsChecked = false;
                }
                else
                {
                    statusRadioButton1.IsChecked = false;
                    statusRadioButton2.IsChecked = true;
                }
            }
        }
        private async void updateButton_Click(object sender, RoutedEventArgs e)
        {
            if (tableComboBox.SelectedItem == null)
            {
                MessageBox.Show("Введите номер стола", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            else if (timeStartComboBox.SelectedItem == null)
            {
                MessageBox.Show("Введите время начала брони", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            else if(timeEndComboBox.SelectedItem == null)
            {
                MessageBox.Show("Введите время конца брони", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            else if(string.IsNullOrEmpty(customerTextBox.Text))
            {
                customerTextBox.Background = new SolidColorBrush(Color.FromRgb(239, 142, 142));
                await Task.Delay(1000);
                customerTextBox.Background = Brushes.White;
            }
            else if(string.IsNullOrEmpty(phoneTextBox.Text))
            {
                phoneTextBox.Background = new SolidColorBrush(Color.FromRgb(239, 142, 142));
                await Task.Delay(1000);
                phoneTextBox.Background = Brushes.White;
            }
            else if(statusRadioButton1.IsChecked==false && statusRadioButton2.IsChecked == false)
            {
                statusRadioButton1.Background = new SolidColorBrush(Color.FromRgb(239, 142, 142));
                statusRadioButton2.Background = new SolidColorBrush(Color.FromRgb(239, 142, 142));
                await Task.Delay(1000);
                statusRadioButton2.Background = Brushes.White;
                statusRadioButton1.Background = Brushes.White;
            }
            if (tableComboBox.SelectedItem != null && timeStartComboBox.SelectedItem != null && timeEndComboBox.SelectedItem != null && !string.IsNullOrEmpty(customerTextBox.Text) && !string.IsNullOrEmpty(phoneTextBox.Text) && (statusRadioButton1.IsChecked == true || statusRadioButton2.IsChecked == true))
            {
                var Result = MessageBox.Show("Вы точно хотите применить изменения?", "Предупреждение о внесении изменений", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (Result == MessageBoxResult.Yes)
                {
                    Exception exeptionFlag = null;
                    try
                    {
                        connection.Open();
                        string status;
                        if (statusRadioButton1.IsChecked == false)
                        {
                            status = "Закрыта";
                        }
                        else
                        {
                            status = "Активна";
                        }
                        SqlCommand cmd = new SqlCommand($"UPDATE Reservation SET Reservation_Start= '{timeStartComboBox.Text}', Reservation_End='{timeEndComboBox.Text}', Tables_ID='{tableComboBox.Text}', Customer_Name='{customerTextBox.Text}',Customer_Phone='{phoneTextBox.Text}',Reservation_Status='{status}' WHERE Reservation_ID='{tmpID}';", connection);
                        cmd.ExecuteScalar();
                    }
                    catch (Exception ex)
                    {
                        exeptionFlag = ex;
                        MessageBox.Show("Error: " + ex.Message);
                    }
                    finally
                    {
                        if (exeptionFlag == null)
                        {
                            MessageBox.Show("Успешно!", "Изменения внесены", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                        connection.Close();
                        getReservations(formattedDate);
                    }
                }
                else if (Result == MessageBoxResult.No)
                {

                }
            }
                        
        }

        private void phoneTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (phoneTextBox.Text.Length > 12)
            {
                phoneTextBox.Text = phoneTextBox.Text.Substring(0, 12);
                phoneTextBox.SelectionStart = phoneTextBox.Text.Length; 
            }
            if (phoneTextBox.Text.Length > 0 && !char.IsDigit(phoneTextBox.Text[phoneTextBox.Text.Length - 1]))
            {
                phoneTextBox.Text = phoneTextBox.Text.Substring(0, phoneTextBox.Text.Length - 1);
                phoneTextBox.SelectionStart = phoneTextBox.Text.Length; 
            }
            if (phoneTextBox.Text.Length < 3)
            {
                phoneTextBox.Text = "+7";
                phoneTextBox.SelectionStart = phoneTextBox.Text.Length; 
            }
            else if (!phoneTextBox.Text.StartsWith("+7"))
            {
                phoneTextBox.Text = "+7" + phoneTextBox.Text;
                phoneTextBox.SelectionStart = phoneTextBox.Text.Length;
            }

            if (addPhoneTextBox.Text.Length > 12)
            {
                addPhoneTextBox.Text = addPhoneTextBox.Text.Substring(0, 12);
                addPhoneTextBox.SelectionStart = addPhoneTextBox.Text.Length;
            }
            if (addPhoneTextBox.Text.Length > 0 && !char.IsDigit(addPhoneTextBox.Text[addPhoneTextBox.Text.Length - 1]))
            {
                addPhoneTextBox.Text = addPhoneTextBox.Text.Substring(0, addPhoneTextBox.Text.Length - 1);
                addPhoneTextBox.SelectionStart = addPhoneTextBox.Text.Length;
            }
            if (addPhoneTextBox.Text.Length < 3)
            {
                addPhoneTextBox.Text = "+7";
                addPhoneTextBox.SelectionStart = addPhoneTextBox.Text.Length;
            }
            else if (!addPhoneTextBox.Text.StartsWith("+7"))
            {
                addPhoneTextBox.Text = "+7" + addPhoneTextBox.Text;
                addPhoneTextBox.SelectionStart = addPhoneTextBox.Text.Length;
            }
        }

        private void deleteButton_Click(object sender, RoutedEventArgs e)
        {
           
            if (tableComboBox.SelectedItem != null && timeStartComboBox.SelectedItem != null && timeEndComboBox.SelectedItem != null && !string.IsNullOrEmpty(customerTextBox.Text) && !string.IsNullOrEmpty(phoneTextBox.Text) && (statusRadioButton1.IsChecked == true || statusRadioButton2.IsChecked == true))
            {
                var Result = MessageBox.Show("Вы точно хотите удалить бронь?", "Предупреждение об удалении", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (Result == MessageBoxResult.Yes)
                {
                    Exception exeptionFlag = null;
                    try
                    {
                        connection.Open();
                        SqlCommand cmd = new SqlCommand($"DELETE FROM Reservation WHERE Reservation_ID='{tmpID}';", connection);
                        cmd.ExecuteScalar();
                    }
                    catch (Exception ex)
                    {
                        exeptionFlag = ex;
                        MessageBox.Show("Error: " + ex.Message);
                    }
                    finally
                    {
                        if (exeptionFlag == null)
                        {
                            MessageBox.Show("Бронь успешно удалена!", "Удалено", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                        connection.Close();
                        getReservations(formattedDate);
                    }
                }
                else if (Result == MessageBoxResult.No)
                {

                }
            }
            else
            {
                MessageBox.Show("Бронь не найдена.", "Невозможно удалить", MessageBoxButton.OK, MessageBoxImage.Error);
            }

        }

        private async void addButton_Click(object sender, RoutedEventArgs e)
        {
            if (addComboBox.SelectedItem == null)
            {
                MessageBox.Show("Введите номер стола", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            else if (addTimeStartComboBox.SelectedItem == null)
            {
                MessageBox.Show("Введите время начала брони", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            else if (addTimeEndComboBox.SelectedItem == null)
            {
                MessageBox.Show("Введите время конца брони", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            else if (string.IsNullOrEmpty(addCustomerTextBox.Text))
            {
                addCustomerTextBox.Background = new SolidColorBrush(Color.FromRgb(239, 142, 142));
                await Task.Delay(1000);
                addCustomerTextBox.Background = Brushes.White;
            }
            else if (string.IsNullOrEmpty(addPhoneTextBox.Text))
            {
                addPhoneTextBox.Background = new SolidColorBrush(Color.FromRgb(239, 142, 142));
                await Task.Delay(1000);
                addPhoneTextBox.Background = Brushes.White;
            }
            if (addComboBox.SelectedItem != null && addTimeStartComboBox.SelectedItem != null && addTimeEndComboBox.SelectedItem != null && !string.IsNullOrEmpty(addCustomerTextBox.Text) && !string.IsNullOrEmpty(addPhoneTextBox.Text))
            {
                if (formattedDate == null)
                {
                    MessageBox.Show($"Выберите дату!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                else 
                {
                    var Result = MessageBox.Show($"Бронь будет добавлена на {formattedDate}.", "Предупреждение о внесении изменений", MessageBoxButton.YesNo, MessageBoxImage.Question);
                    if (Result == MessageBoxResult.Yes)
                    {
                        Exception exeptionFlag = null;
                        try
                        {
                            connection.Open();
                            SqlCommand cmd = new SqlCommand($"INSERT INTO Reservation VALUES ('{addComboBox.Text}','{addCustomerTextBox.Text}','{addPhoneTextBox.Text}','{formattedDate}','{addTimeStartComboBox.Text}','{addTimeEndComboBox.Text}','Активна');", connection);
                            cmd.ExecuteScalar();
                        }
                        catch (Exception ex)
                        {
                            exeptionFlag = ex;
                            MessageBox.Show("Error: " + ex.Message);
                        }
                        finally
                        {
                            if (exeptionFlag == null)
                            {
                                MessageBox.Show("Бронь успешно добавлена!", "Добавлено", MessageBoxButton.OK, MessageBoxImage.Information);
                            }
                            connection.Close();
                            getReservations(formattedDate);
                        }
                    }
                    else if (Result == MessageBoxResult.No)
                    {
                    }
                }
            }
        }
    }
}
