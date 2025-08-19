using LiveCharts;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using static System.Net.Mime.MediaTypeNames;
using System.Windows.Media;
using System.Collections.ObjectModel;
using System.Windows.Media.Imaging;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Google.Protobuf.WellKnownTypes;
using MySqlX.XDevAPI.Common;
using Org.BouncyCastle.Asn1.X500;
using System.Timers;
using System.Windows.Threading;
using System.Diagnostics;
using System.Management;
using System.Security.Cryptography;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Data;
using LiveCharts.Wpf;
using System.Windows.Controls.Primitives;

namespace WpfApp1
{
    public class LastDishes
    {
        public string Orders_ID { get; set; }
        public int Orders_Bill { get; set; }
        public string Orders_Dish_List { get; set; }
        public string Orders_Time { get; set; }
        public string Orders_Status { get; set; }
        public string Orders_Serving_time { get; set; }
    }
    public class Garcon
    {
        public int ID { get; set; }
        public string GarconName { get; set; }
        public int CountOfOrders { get; set; }
    }
    public class Menu
    {
        public string ID { get; set; }
        public string Name { get; set; }
        public int Price { get; set; }
        public string Image { get; set; }
        public string Type { get; set; }
    }
    public class GarconSummary
    {
        public string GarconName { get; set; }
        public decimal TotalRevenue { get; set; }
        public int TotalOrdersServed { get; set; }
        public int TotalCustomersServed { get; set; }
    }
    public partial class MainWindow : Window
    {
        private SqlConnection connection = new SqlConnection(ConfigurationManager.ConnectionStrings["constr"].ConnectionString);

        public ChartValues<double> ChartValues { get; set; }
        public ChartValues<double> MonthOrdersChartValues { get; set; }

        public ChartValues<double> FirstMonthValues { get; set; }
        public ChartValues<double> SecondMonthValues { get; set; }
        public ChartValues<double> ThirdMonthValues { get; set; }

        public ChartValues<double> Occupancy { get; set; }

        public object FirstMonth { get; set; }
        public object SecondMonth { get; set; }
        public object ThirdMonth { get; set; }

        public string FirstMonthText { get; set; }
        public string SecondMonthText { get; set; }
        public string ThirdMonthText { get; set; }

        public List<string> Labels { get; set; }
        public List<string> Labels2 { get; set; }
        public List<string> Labels3 { get; set; }
        public List<string> Occupancy_Labels { get; set; }

        public List<double> Max2 { get; set; }
        public double Max2Value { get; set; }
        public int Max { get; set; }
        public int MonthOrdersMax { get; set; }
        public string MonthSells { get; set; }

        public string PicturePath { get; set; }
        public string Dish { get; set; }
        public string OrdersCount { get; set; }
        public string MonthOrdersCount { get; set; }

        public int Occupancy_Max { get; set; }
        DateTime now = DateTime.Now;

        private DispatcherTimer timer;
        public MainWindow()
        {
            InitializeComponent();
            showMainPage();
        }
        //Main
        private void getOrders()
        {
            mainButton.Tag = "Selected";
            DataContext = this;
            try
            {
                connection.Open();
                SqlCommand cmd = new SqlCommand($"SELECT SUM(Orders_Bill) FROM Orders WHERE MONTH(CONVERT(DATE, Orders_Date, 104)) = MONTH(GETDATE()) AND YEAR(CONVERT(DATE, Orders_Date, 104)) = YEAR(GETDATE())", connection);
                if (Convert.IsDBNull(cmd.ExecuteScalar()))
                {
                    MonthSells = "0 руб.";
                }
                else
                {
                    MonthSells = String.Format("{0:n0}", Convert.ToInt32(cmd.ExecuteScalar())) + " руб.";
                }
                ChartValues = new ChartValues<double> { };
                Labels = new List<string> { };
                int daysCout = DateTime.DaysInMonth(now.Year, now.Month);
                for (int i = 1; i < now.Day + 1; i++)
                {
                    cmd = new SqlCommand($"SELECT SUM(Orders_Bill) FROM Orders WHERE CONVERT(DATE, Orders_Date, 104) = '{now.Year}-{now.Month}-{i}'", connection);
                    Labels.Add(i.ToString());
                    if (Convert.IsDBNull(cmd.ExecuteScalar()))
                    {
                        ChartValues.Add(1);
                    }
                    else
                    {
                        ChartValues.Add(Convert.ToDouble(cmd.ExecuteScalar()));
                    }
                }
                cmd = new SqlCommand($"SELECT COUNT(*) FROM Orders WHERE CONVERT(DATE, Orders_Date, 104) = CONVERT(DATE, GETDATE(), 104) AND (Orders_Status != 'Отменён' AND Orders_Status != 'Завершен');", connection);
                if (Convert.IsDBNull(cmd.ExecuteScalar()))
                {
                    ActiveOrders.Text = "0";
                }
                else
                {
                    ActiveOrders.Text = cmd.ExecuteScalar().ToString();
                }

                cmd = new SqlCommand($"SELECT COUNT(*) FROM Orders WHERE CONVERT(DATE, Orders_Date, 104) = CONVERT(DATE, GETDATE(), 104)", connection);
                if (Convert.IsDBNull(cmd.ExecuteScalar()))
                {
                    TodayOrders.Text = "0";
                }
                else
                {
                    TodayOrders.Text = cmd.ExecuteScalar().ToString();
                }
                cmd = new SqlCommand($"SELECT COUNT(*) FROM Orders WHERE CONVERT(DATE, Orders_Date, 104) = CONVERT(DATE, GETDATE(), 104) AND Orders_Status = 'Отменён'", connection);
                if (Convert.IsDBNull(cmd.ExecuteScalar()))
                {
                    CancelledOrders.Text = "0";
                    Max = 0;
                }
                else
                {
                    CancelledOrders.Text = cmd.ExecuteScalar().ToString();
                    Max = Convert.ToInt32(ChartValues.Max() + (ChartValues.Max() * 0.3));
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

        private string getMonths(int Month)
        {
            switch (Month)
            {
                case 1:
                    return "Январь";
                case 2:
                    return "Февраль";
                case 3:
                    return "Март";
                case 4:
                    return "Апрель";
                case 5:
                    return "Май";
                case 6:
                    return "Июнь";
                case 7:
                    return "Июль";
                case 8:
                    return "Август";
                case 9:
                    return "Сентябрь";
                case 10:
                    return "Октябрь";
                case 11:
                    return "Ноябрь";
                case 12:
                    return "Декабрь";
                default:
                    return "";
            }
        }
        private void GenerateSchedule_Click(object sender, RoutedEventArgs e)
        {
            if (GarconComboBox.SelectedItem == null)
            {
                MessageBox.Show("Пожалуйста, выберите официанта.", "Ошибка ввода", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!(GarconComboBox.SelectedItem is DataRowView selectedRow))
            {
                MessageBox.Show("Неверный выбор официанта.", "Ошибка ввода", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // 2. Валидация полей времени и рабочих часов
            int garconId = (int)selectedRow["Garcon_ID"];
            int startHour, startMinute, hoursPerDay;

            if (!int.TryParse(StartHourBox.Text, out startHour) || startHour < 0 || startHour > 23)
            {
                MessageBox.Show("Пожалуйста, введите корректное значение для часа начала смены (от 0 до 23).", "Ошибка ввода", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!int.TryParse(StartMinuteBox.Text, out startMinute) || startMinute < 0 || startMinute > 59)
            {
                MessageBox.Show("Пожалуйста, введите корректное значение для минуты начала смены (от 0 до 59).", "Ошибка ввода", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!int.TryParse(HoursPerDayBox.Text, out hoursPerDay) || hoursPerDay <= 0)
            {
                MessageBox.Show("Пожалуйста, введите корректное количество рабочих часов (больше 0).", "Ошибка ввода", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!ScheduleStartDatePicker.SelectedDate.HasValue)
            {
                MessageBox.Show("Пожалуйста, выберите начальную дату для расписания.", "Ошибка ввода", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!ScheduleEndDatePicker.SelectedDate.HasValue)
            {
                MessageBox.Show("Пожалуйста, выберите конечную дату для расписания.", "Ошибка ввода", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            DateTime startDate = ScheduleStartDatePicker.SelectedDate.Value.Date; 
            DateTime endDate = ScheduleEndDatePicker.SelectedDate.Value.Date;    

            if (endDate < startDate)
            {
                MessageBox.Show("Конечная дата не может быть раньше начальной даты.", "Ошибка ввода", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
         
            TimeSpan shiftStart = new TimeSpan(startHour, startMinute, 0);
            TimeSpan shiftDuration = TimeSpan.FromHours(hoursPerDay);

            List<ScheduleEntry> scheduleEntries = new List<ScheduleEntry>();

            for (DateTime currentDay = startDate; currentDay <= endDate; currentDay = currentDay.AddDays(1))
            {
                if (IsDayOff(currentDay.DayOfWeek))
                    continue;

                // Вычисляем время окончания смены для текущего дня
                TimeSpan shiftEnd = shiftStart.Add(shiftDuration);

                // Обрабатываем смены, которые переходят через полночь
                if (shiftEnd.Days > 0)
                {
                    shiftEnd = new TimeSpan(shiftEnd.Hours, shiftEnd.Minutes, shiftEnd.Seconds);
                }

                scheduleEntries.Add(new ScheduleEntry
                {
                    WorkDate = currentDay,
                    ShiftStart = shiftStart,
                    ShiftEnd = shiftEnd
                });
            }

            using (SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings["constr"].ConnectionString))
            {
                conn.Open();
                SqlTransaction transaction = conn.BeginTransaction();
                try
                {
                    SqlCommand deleteCmd = new SqlCommand(@"
                DELETE FROM Garcon_Schedule
                WHERE Garcon_ID = @GarconID AND Work_Date >= @StartDate AND Work_Date <= @EndDate", conn, transaction);
                    deleteCmd.Parameters.AddWithValue("@GarconID", garconId);
                    deleteCmd.Parameters.AddWithValue("@StartDate", startDate);
                    deleteCmd.Parameters.AddWithValue("@EndDate", endDate);
                    deleteCmd.ExecuteNonQuery();

                    foreach (ScheduleEntry entry in scheduleEntries)
                    {
                        SqlCommand insertCmd = new SqlCommand(@"
                    INSERT INTO Garcon_Schedule (Garcon_ID, Work_Date, Shift_Start, Shift_End)
                    VALUES (@GarconID, @Date, @Start, @End)", conn, transaction);

                        insertCmd.Parameters.AddWithValue("@GarconID", garconId);
                        insertCmd.Parameters.AddWithValue("@Date", entry.WorkDate);
                        insertCmd.Parameters.AddWithValue("@Start", entry.ShiftStart);
                        insertCmd.Parameters.AddWithValue("@End", entry.ShiftEnd);
                        insertCmd.ExecuteNonQuery();
                    }
                    transaction.Commit();
                    MessageBox.Show("Расписание успешно создано!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    MessageBox.Show($"Ошибка при генерации расписания: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }

            if (scheduleComboBox != null)
            {
                scheduleComboBox.SelectedValue = garconId;
            }
            LoadMonthlyGarconSchedule(garconId, startDate);
        }
        private void ScheduleComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (scheduleComboBox.SelectedValue != null)
            {
                int selectedGarconId = (int)scheduleComboBox.SelectedValue;
                LoadMonthlyGarconSchedule(selectedGarconId, DateTime.Now);
            }
           
        }

        private void LoadMonthlyGarconSchedule(int garconId, DateTime targetMonth)
        {
            CalendarGrid.Children.Clear();
            CalendarGrid.Rows = 6;
            CalendarGrid.Columns = 7;

            DateTime firstDay = new DateTime(targetMonth.Year, targetMonth.Month, 1);
            int offset = ((int)firstDay.DayOfWeek + 6) % 7;

            DateTime calendarStart = firstDay.AddDays(-offset);
            DateTime calendarEnd = calendarStart.AddDays(41);

            Dictionary<DateTime, string> scheduleMap = new Dictionary<DateTime, string>();

            using (SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings["constr"].ConnectionString))
            {
                conn.Open();

                SqlCommand cmd = new SqlCommand(@"
            SELECT Work_Date, Shift_Start, Shift_End
            FROM Garcon_Schedule
            WHERE Garcon_ID = @GarconID AND Work_Date BETWEEN @Start AND @End
        ", conn);

                cmd.Parameters.AddWithValue("@GarconID", garconId);
                cmd.Parameters.AddWithValue("@Start", calendarStart);
                cmd.Parameters.AddWithValue("@End", calendarEnd);

                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        DateTime date = Convert.ToDateTime(reader["Work_Date"]);
                        TimeSpan? start = reader["Shift_Start"] != DBNull.Value
                                         ? (TimeSpan?)reader["Shift_Start"]
                                         : null;

                        TimeSpan? end = reader["Shift_End"] != DBNull.Value
                                        ? (TimeSpan?)reader["Shift_End"]
                                        : null;
                        string shiftText = $"{start:hh\\:mm} - {end:hh\\:mm}";
                        scheduleMap[date.Date] = shiftText;
                    }

                }
            }

            for (int i = 0; i < 42; i++)
            {
                DateTime day = calendarStart.AddDays(i);
                bool isWorkDay = scheduleMap.ContainsKey(day.Date);
                bool isToday = day.Date == DateTime.Today;
                string shift = isWorkDay ? $"Смена\n{scheduleMap[day.Date]}" : "Выходной";
                string weekDay = day.ToString("ddd", new CultureInfo("ru-RU")).ToUpper();

                Brush dayBackground;

                if (isToday)
                {
                    dayBackground = new SolidColorBrush(Color.FromArgb(150, 185, 74, 22)); 
                }
                else if (day.Month != targetMonth.Month)
                {
                    dayBackground = new SolidColorBrush(Color.FromArgb(60, 0, 0, 0)); // внешний месяц
                }
                else if (isWorkDay)
                {
                    dayBackground = new SolidColorBrush(Color.FromArgb(150, 255, 165, 0)); // оранжевый
                }
                else
                {
                    dayBackground = new SolidColorBrush(Color.FromArgb(150, 0, 0, 0)); // выходной
                }

                CalendarGrid.Children.Add(new Border
                {
                    BorderBrush = Brushes.Black,
                    CornerRadius = new CornerRadius(3),
                    BorderThickness = new Thickness(1),
                    Margin = new Thickness(5),
                    Background = dayBackground,

                    Child = new StackPanel
                    {
                        Margin = new Thickness(4),
                        Children =
            {
                new TextBlock
                {
                    Text = $"{day.Day} ({weekDay})",
                    FontSize = 18,
                    FontWeight = FontWeights.Bold,
                    FontFamily = new FontFamily("Fonts/#Inter Bold"),
                    Foreground = Brushes.Black,
                    HorizontalAlignment = HorizontalAlignment.Right
                },
                new TextBlock
                {
                    Text = shift,
                    FontSize = 17,
                    FontFamily = new FontFamily("Fonts/#Inter Medium"),
                    TextWrapping = TextWrapping.Wrap,
                    Foreground = Brushes.Black,
                    Margin = new Thickness(0, 4, 0, 0)
                }
            }
                    }
                });
            }

        }
        private void LoadGarconsToComboBox()
        {
            using (SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings["constr"].ConnectionString))
            {
                conn.Open();
                SqlCommand cmd = new SqlCommand("SELECT Garcon_ID, Garcon_Name FROM Garcon", conn);
                SqlDataAdapter da = new SqlDataAdapter(cmd);
                DataTable dt = new DataTable();
                da.Fill(dt);

                GarconComboBox.ItemsSource = dt.DefaultView;
                GarconComboBox.SelectedIndex = 0;
            }
           
        }
        private void LoadGarconsToScheduleComboBox()
        {
            List<Garcon> garcons = new List<Garcon>();

            using (SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings["constr"].ConnectionString))
            {
                conn.Open();
                SqlCommand cmd = new SqlCommand("SELECT Garcon_ID, Garcon_Name FROM Garcon", conn);
                SqlDataAdapter da = new SqlDataAdapter(cmd);
                DataTable dt = new DataTable();
                da.Fill(dt);

                scheduleComboBox.ItemsSource = dt.DefaultView;
                scheduleComboBox.SelectedIndex = 0;
            }
        }


        private void LoadGarconSummary()
        {
            List<GarconSummary> summaries = new List<GarconSummary>();

            using (SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings["constr"].ConnectionString))
            {
                conn.Open();
                SqlCommand cmd = new SqlCommand(@"
            SELECT
                G.Garcon_Name,
                SUM(O.Orders_Bill) AS TotalRevenue,
                COUNT(DISTINCT O.Orders_ID) AS TotalOrdersServed,
                SUM(O.Orders_Customers_Count) AS TotalCustomersServed
            FROM
                dbo.Garcon AS G
            JOIN
                dbo.Orders AS O ON G.Garcon_ID = O.Garcon_ID
            WHERE
                O.Orders_Date >= DATEADD(month, DATEDIFF(month, 0, GETDATE()), 0)
                AND O.Orders_Date < DATEADD(month, DATEDIFF(month, 0, GETDATE()) + 1, 0)
                AND O.Orders_Status = 'Завершен'
            GROUP BY
                G.Garcon_Name
            ORDER BY
                TotalRevenue DESC;", conn);

                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        summaries.Add(new GarconSummary
                        {
                            GarconName = reader["Garcon_Name"].ToString(),
                            TotalRevenue = Convert.ToDecimal(reader["TotalRevenue"]),
                            TotalOrdersServed = Convert.ToInt32(reader["TotalOrdersServed"]),
                            TotalCustomersServed = Convert.ToInt32(reader["TotalCustomersServed"])
                        });
                    }
                }
                GarconSummaryDataGrid.ItemsSource = summaries;
            }
        }


        private bool IsDayOff(DayOfWeek day)
        {
            switch (day)
            {
                case DayOfWeek.Sunday: return SundayCheck.IsChecked == true;
                case DayOfWeek.Monday: return MondayCheck.IsChecked == true;
                case DayOfWeek.Tuesday: return TuesdayCheck.IsChecked == true;
                case DayOfWeek.Wednesday: return WednesdayCheck.IsChecked == true;
                case DayOfWeek.Thursday: return ThursdayCheck.IsChecked == true;
                case DayOfWeek.Friday: return FridayCheck.IsChecked == true;
                case DayOfWeek.Saturday: return SaturdayCheck.IsChecked == true;
                default: return false;
            }
        }
        private void getLast3MonthOrders()
        {
            DataContext = this;
            DateTime threeMonthsAgo = DateTime.Today.AddMonths(-2);
            FirstMonthText = getMonths(threeMonthsAgo.Month);
            int daysCout = DateTime.DaysInMonth(threeMonthsAgo.Year, threeMonthsAgo.Month);
            Labels2 = new List<string> { };
            Max2 = new List<double> { };
            for (int c = 1; c < 32; c++)
            {
                Labels2.Add(c.ToString());
            }
            try
            {
                connection.Open();
                SqlCommand cmd = new SqlCommand("", connection);
                FirstMonthValues = new ChartValues<double> { };
                for (int i = 1; i <= daysCout; i++)
                {
                    cmd = new SqlCommand($"SELECT SUM(Orders_Bill) FROM Orders WHERE CONVERT(DATE, Orders_Date, 104) = '{threeMonthsAgo.Year}-{threeMonthsAgo.Month}-{i}'", connection);
                    if (Convert.IsDBNull(cmd.ExecuteScalar()))
                    {
                        Max2.Add(1);
                        FirstMonthValues.Add(1);
                    }
                    else
                    {
                        Max2.Add(Convert.ToDouble(cmd.ExecuteScalar()));
                        FirstMonthValues.Add(Convert.ToDouble(cmd.ExecuteScalar()));
                    }
                }
                threeMonthsAgo = DateTime.Today.AddMonths(-1);
                daysCout = DateTime.DaysInMonth(threeMonthsAgo.Year, threeMonthsAgo.Month);
                SecondMonthText = getMonths(threeMonthsAgo.Month);
                SecondMonthValues = new ChartValues<double> { };
                for (int i = 1; i <= daysCout; i++)
                {
                    cmd = new SqlCommand($"SELECT SUM(Orders_Bill) FROM Orders WHERE CONVERT(DATE, Orders_Date, 104) = '{threeMonthsAgo.Year}-{threeMonthsAgo.Month}-{i}'", connection);
                    if (Convert.IsDBNull(cmd.ExecuteScalar()))
                    {
                        Max2.Add(1);
                        SecondMonthValues.Add(1);
                    }
                    else
                    {
                        Max2.Add(Convert.ToDouble(cmd.ExecuteScalar()));
                        SecondMonthValues.Add(Convert.ToDouble(cmd.ExecuteScalar()));
                    }
                }
                daysCout = DateTime.DaysInMonth(now.Year, now.Month);
                ThirdMonthText = getMonths(now.Month);
                ThirdMonthValues = new ChartValues<double> { };
                Labels = new List<string> { };
                for (int i = 1; i <= daysCout; i++)
                {
                    cmd = new SqlCommand($"SELECT SUM(Orders_Bill) FROM Orders WHERE CONVERT(DATE, Orders_Date, 104) = '{now.Year}-{now.Month}-{i}'", connection);
                    if (Convert.IsDBNull(cmd.ExecuteScalar()))
                    {
                        Max2.Add(1);
                        Labels.Add(i.ToString());
                        ThirdMonthValues.Add(1);
                    }
                    else
                    {
                        Max2.Add(Convert.ToDouble(cmd.ExecuteScalar()));
                        Labels.Add(i.ToString());
                        ThirdMonthValues.Add(Convert.ToDouble(cmd.ExecuteScalar()));
                    }
                }
                Max2Value = Max2.Max() + Math.Round((Max2.Max() * 0.2), 0);
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

        private void getTopDish()
        {
            DataContext = this;
            try
            {
                connection.Open();

                string query = @"
            SELECT TOP 1 
                m.Menu_Name AS DishName,
                COUNT(od.Order_Dish_ID) AS OrdersCount,
                m.Menu_Image
            FROM Order_Dishes od
            JOIN Menu m ON od.Menu_ID = m.Menu_ID
            JOIN Orders o ON od.Orders_ID = o.Orders_ID
            WHERE MONTH(o.Orders_Date) = MONTH(GETDATE())
              AND YEAR(o.Orders_Date) = YEAR(GETDATE())
              AND m.Menu_Name NOT IN (
                    'Pouilly-Montrachet', 'Chablis', 'Saint-Émilion', 
                    'Côte dOr', 'Chateau Angludet', 
                    'Чёрный чай', 'Зелёный чай', 'Domaine Clarence'
                )
            GROUP BY m.Menu_Name, m.Menu_Image
            ORDER BY OrdersCount DESC;";

                SqlCommand cmd = new SqlCommand(query, connection);
                SqlDataReader reader = cmd.ExecuteReader();

                if (reader.Read())
                {
                    Dish = reader["DishName"].ToString();
                    int count = Convert.ToInt32(reader["OrdersCount"]);

                    // Подстановка формы слова "заказ"
                    if (count % 10 == 1 && count % 100 != 11)
                        OrdersCount = count + " заказ";
                    else if ((count % 10 >= 2 && count % 10 <= 4) && (count % 100 < 10 || count % 100 >= 20))
                        OrdersCount = count + " заказа";
                    else
                        OrdersCount = count + " заказов";

                    if (reader["Menu_Image"] != DBNull.Value)
                    {
                        byte[] imageData = (byte[])reader["Menu_Image"];
                        using (MemoryStream ms = new MemoryStream(imageData))
                        {
                            BitmapImage originalImage = new BitmapImage();
                            originalImage.BeginInit();
                            originalImage.CacheOption = BitmapCacheOption.OnLoad;
                            originalImage.StreamSource = ms;
                            originalImage.EndInit();
                            originalImage.Freeze();

                            int cropHeight = Math.Max(0, originalImage.PixelHeight - 20); 

                            CroppedBitmap croppedImage = new CroppedBitmap(originalImage, new Int32Rect(
                                0, // X
                                20, // Y (сдвиг вниз)
                                originalImage.PixelWidth, // ширина
                                cropHeight // высота без верхних 20 пикселей
                            ));

                            topDishImageControl.Source = croppedImage;
                        }
                    }

                }
                else
                {
                    Dish = "Нет информации";
                    OrdersCount = "0 заказов";
                }

                reader.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка: " + ex.Message);
            }
            finally
            {
                if (connection.State != ConnectionState.Closed)
                    connection.Close();
            }
        }


        private void getOrdersCount()
        {
            DataContext = this;
            try
            {
                connection.Open();
                SqlCommand cmd = new SqlCommand($"SELECT COUNT(*) FROM Orders WHERE MONTH(CONVERT(DATE, Orders_Date, 104)) = MONTH(GETDATE()) AND YEAR(CONVERT(DATE, Orders_Date, 104)) = YEAR(GETDATE())", connection);
                if (Convert.IsDBNull(cmd.ExecuteScalar()))
                {
                    MonthOrdersCount = "0 заказов";
                }
                else
                {
                    MonthOrdersCount = cmd.ExecuteScalar().ToString() + " заказов";
                }

                MonthOrdersChartValues = new ChartValues<double> { };
                MonthOrdersChartValues.Add(1);
                Labels3 = new List<string> { };
                int daysCout = DateTime.DaysInMonth(now.Year, now.Month);
                for (int i = 1; i < now.Day + 1; i++)
                {
                    cmd = new SqlCommand($"SELECT COUNT(*) FROM Orders WHERE CONVERT(DATE, Orders_Date, 104) = '{now.Year}-{now.Month}-{i}'", connection);
                    Labels3.Add(i.ToString());
                    if (Convert.IsDBNull(cmd.ExecuteScalar()))
                    {
                        MonthOrdersChartValues.Add(1);
                    }
                    else
                    {
                        MonthOrdersChartValues.Add(Convert.ToDouble(cmd.ExecuteScalar()));
                    }
                }
                MonthOrdersMax = Convert.ToInt32(MonthOrdersChartValues.Max() + (MonthOrdersChartValues.Max() * 0.3));
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

       
        static string getProjectFolderPath()
        {

            string currentDirectory = Directory.GetCurrentDirectory();
            while (!string.IsNullOrEmpty(currentDirectory))
            {
                if (Directory.GetFiles(currentDirectory, "*.csproj").Length > 0)
                {
                    return currentDirectory;
                }

                currentDirectory = Directory.GetParent(currentDirectory)?.FullName;
            }

            return null;
        }
        //Menu
        private void LoadMenuCategories()
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings["constr"].ConnectionString))
                {
                    conn.Open();

                    using (SqlCommand cmd = new SqlCommand("SELECT DISTINCT Menu_Type FROM Menu WHERE Menu_Type IS NOT NULL", conn))
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            MenuCategoryComboBox.Items.Add(reader.GetString(0));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при загрузке категорий: " + ex.Message);
            }
        }

        public void getMenu(string parameter)
        {
            List<GarconMenu> Dishes = new List<GarconMenu>();
            try
            {
                connection.Open();
                SqlCommand cmd = new SqlCommand($"SELECT * FROM Menu {parameter};", connection);
                SqlDataReader rdr = cmd.ExecuteReader();
                while (rdr.Read())
                {
                    GarconMenu tableFiller = new GarconMenu
                    {
                        ID = rdr["Menu_ID"].ToString(),
                        Name = rdr["Menu_Name"].ToString(),
                        Price = Convert.ToInt32(rdr["Menu_Price"]),
                        Type = rdr["Menu_Type"].ToString(),
                        Discount = Convert.ToInt32(rdr["Menu_Discount"]),
                    };

                    var imageData = rdr["Menu_Image"] as byte[];
                    if (imageData != null && imageData.Length > 0)
                    {
                        using (var stream = new MemoryStream(imageData))
                        {
                            BitmapImage bitmap = new BitmapImage();
                            bitmap.BeginInit();
                            bitmap.CacheOption = BitmapCacheOption.OnLoad;
                            bitmap.StreamSource = stream;
                            bitmap.EndInit();
                            bitmap.Freeze();
                            tableFiller.Image = bitmap;
                        }
                    }
                    else
                    {
                        tableFiller.Image = new BitmapImage(new Uri($"{getProjectFolderPath()}\\Images\\Dishes\\no photo.png"));
                    }

                    Dishes.Add(tableFiller);
                }
                menuListView.ItemsSource = Dishes;
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
        private void MenuCategoryComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (MenuCategoryComboBox.SelectedItem is string selectedCategory)
            {
                string condition = $"WHERE Menu_Type = '{selectedCategory.Replace("'", "''")}'";
                getMenu(condition); 
            }
        }
        //Analytics
        private void getAnalyticsCount(int days)
        {
            DataContext = this;
            List<Garcon> garcons = new List<Garcon>();
            if (days == 1)
            {
                try
                {
                    connection.Open();
                    CustomersDate.Text = $"На\n{DateTime.Now.ToString("dd.MM.yyyy")}";
                    SqlCommand cmd = new SqlCommand($"SELECT SUM(Orders_Customers_Count) AS TotalVisitors FROM Orders WHERE CONVERT(DATE, Orders_Date, 104) = '{now.Date:yyyy-MM-dd}'", connection);
                    if (Convert.IsDBNull(cmd.ExecuteScalar()))
                    {
                        CustomersCount.Text = "0";
                    }
                    else
                    {
                        CustomersCount.Text = cmd.ExecuteScalar().ToString();
                    }
                    //REVENUE
                    cmd = new SqlCommand($"SELECT SUM(Orders_Bill) FROM Orders WHERE Orders_Date = DATEADD(DAY, 0, CAST(GETDATE() AS DATE));", connection);
                    SqlCommand cmd2 = new SqlCommand($"SELECT SUM(Orders_Bill) FROM Orders WHERE Orders_Date = DATEADD(DAY, -1, CAST(GETDATE() AS DATE));", connection);
                    revenueMoney.Text = $"{0}%";
                    revenueBorder.Background = new SolidColorBrush(Color.FromArgb(0xFF, 0xD4, 0xD4, 0xD4));
                    if (!Convert.IsDBNull(cmd.ExecuteScalar()) && !Convert.IsDBNull(cmd2.ExecuteScalar()))
                    {
                        int firstNumber = Convert.ToInt32(cmd.ExecuteScalar());
                        int secondNumber = Convert.ToInt32(cmd2.ExecuteScalar());
                        double weekPercentDifference;
                        if (firstNumber > secondNumber)
                        {
                            weekPercentDifference = ((double)(secondNumber - firstNumber) / secondNumber) * 100;

                            revenueBorder.Background = new SolidColorBrush(Color.FromRgb(0xA0, 0xE1, 0x82));
                            revenueMoney.Text = $"{Math.Abs((int)weekPercentDifference)}%";
                        }
                        else if (firstNumber < secondNumber)
                        {
                            weekPercentDifference = ((double)(firstNumber - secondNumber) / Math.Max(firstNumber, secondNumber)) * 100;
                            revenueBorder.Background = new SolidColorBrush(Color.FromArgb(0xFF, 0xEA, 0x72, 0x72));
                            if (Math.Abs((int)weekPercentDifference) < 1)
                            {
                                revenueMoney.Text = $"{Math.Abs((int)weekPercentDifference)}%";
                            }
                            else
                            {
                                revenueMoney.Text = $"-{Math.Abs((int)weekPercentDifference)}%";
                            }
                        }
                    }
                    else
                    {
                    }
                    revenueText.Text = $"На сегодня\n{DateTime.Now.ToString("dd.MM.yyyy")}";
                    //CUSTOMERS
                    cmd = new SqlCommand($"SELECT SUM(Orders_Customers_Count) FROM Orders WHERE Orders_Date = DATEADD(DAY, 0, CAST(GETDATE() AS DATE));", connection);
                    cmd2 = new SqlCommand($"SELECT SUM(Orders_Customers_Count) FROM Orders WHERE Orders_Date = DATEADD(DAY, -1, CAST(GETDATE() AS DATE));", connection);
                    customersPercent.Text = $"{0}%";
                    customersBorder.Background = new SolidColorBrush(Color.FromArgb(0xFF, 0xD4, 0xD4, 0xD4));
                    if (!Convert.IsDBNull(cmd.ExecuteScalar()) && !Convert.IsDBNull(cmd2.ExecuteScalar()))
                    {
                        int first = Convert.ToInt32(cmd.ExecuteScalar());
                        int second = Convert.ToInt32(cmd2.ExecuteScalar());
                        int maxNumber = Math.Max(first, second);
                        int minNumber = Math.Min(first, second);
                        double weekPercentDifference;
                        if (first > second)
                        {
                            weekPercentDifference = ((double)(second - first) / second) * 100;

                            customersBorder.Background = new SolidColorBrush(Color.FromRgb(0xA0, 0xE1, 0x82));
                            customersPercent.Text = $"{Math.Abs((int)weekPercentDifference)}%";
                        }
                        else if (first < second)
                        {
                            weekPercentDifference = ((double)(first - second) / Math.Max(first, second)) * 100;
                            customersBorder.Background = new SolidColorBrush(Color.FromArgb(0xFF, 0xEA, 0x72, 0x72));
                            if (Math.Abs((int)weekPercentDifference) < 1)
                            {
                                customersPercent.Text = $"{Math.Abs((int)weekPercentDifference)}%";
                            }
                            else
                            {
                                customersPercent.Text = $"-{Math.Abs((int)weekPercentDifference)}%";
                            }
                        }
                    }
                    else
                    {
                    }
                    customersText.Text = $"На сегодня\n{DateTime.Now.ToString("dd.MM.yyyy")}";
                  
                    connection.Close();
                    connection.Open();
                    cmd = new SqlCommand($"SELECT AVG(DATEDIFF(MINUTE, Orders_Time, Orders_serving_time)) AS Average_Waiting_Time FROM Orders WHERE Orders_Date = CAST(GETDATE() AS DATE);", connection);
                    if (Convert.IsDBNull(cmd.ExecuteScalar()))
                    {
                        timeWaiting.Text = "0 минут";
                    }
                    else
                    {
                        timeWaiting.Text = $"{cmd.ExecuteScalar()} минут";
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
            else if (days == 7)
            {
                try
                {
                    connection.Open();
                    CustomersDate.Text = $"С {DateTime.Today.AddDays(-7).ToString("dd.MM.yyyy")}\nпо {DateTime.Now.ToString("dd.MM.yyyy")}";
                    SqlCommand cmd = new SqlCommand($"SELECT SUM(Orders_Customers_Count) AS TotalVisitors FROM Orders WHERE Orders_Date >= DATEADD(DAY, -7, GETDATE());", connection);
                    if (Convert.IsDBNull(cmd.ExecuteScalar()))
                    {
                        CustomersCount.Text = "0";
                    }
                    else
                    {
                        CustomersCount.Text = cmd.ExecuteScalar().ToString();
                    }
                    //REVENUE
                    cmd = new SqlCommand($"SELECT SUM(Orders_Bill) FROM Orders WHERE Orders_Date >= DATEADD(DAY, -7, GETDATE()) AND Orders_Date < GETDATE();", connection);
                    SqlCommand cmd2 = new SqlCommand($"SELECT SUM(Orders_Bill) FROM Orders WHERE Orders_Date >= DATEADD(DAY, -14, GETDATE()) AND Orders_Date < DATEADD(DAY, -7, GETDATE());", connection);
                    revenueMoney.Text = $"{0}%";
                    revenueBorder.Background = new SolidColorBrush(Color.FromArgb(0xFF, 0xD4, 0xD4, 0xD4));
                    if (!Convert.IsDBNull(cmd.ExecuteScalar()) && !Convert.IsDBNull(cmd2.ExecuteScalar()))
                    {
                        int firstNumber = Convert.ToInt32(cmd.ExecuteScalar());
                        int secondNumber = Convert.ToInt32(cmd2.ExecuteScalar());
                        double weekPercentDifference;
                        if (firstNumber > secondNumber)
                        {
                            weekPercentDifference = ((double)(secondNumber - firstNumber) / secondNumber) * 100;

                            revenueBorder.Background = new SolidColorBrush(Color.FromRgb(0xA0, 0xE1, 0x82));
                            revenueMoney.Text = $"{Math.Abs((int)weekPercentDifference)}%";
                        }
                        else if (firstNumber < secondNumber)
                        {
                            weekPercentDifference = ((double)(firstNumber - secondNumber) / Math.Max(firstNumber, secondNumber)) * 100;
                            revenueBorder.Background = new SolidColorBrush(Color.FromArgb(0xFF, 0xEA, 0x72, 0x72));
                            if (Math.Abs((int)weekPercentDifference) < 1)
                            {
                                revenueMoney.Text = $"{Math.Abs((int)weekPercentDifference)}%";
                            }
                            else
                            {
                                revenueMoney.Text = $"-{Math.Abs((int)weekPercentDifference)}%";
                            }
                        }
                    }
                    else
                    {
                    }
                    revenueText.Text = $"С {DateTime.Today.AddDays(-7).ToString("dd.MM.yyyy")}\nпо {DateTime.Now.ToString("dd.MM.yyyy")}";
                    //CUSTOMERS
                    cmd = new SqlCommand($"SELECT SUM(Orders_Customers_Count) FROM Orders WHERE Orders_Date = DATEADD(DAY, 0, CAST(GETDATE() AS DATE));", connection);
                    cmd2 = new SqlCommand($"SELECT SUM(Orders_Customers_Count) FROM Orders WHERE Orders_Date = DATEADD(DAY, -1, CAST(GETDATE() AS DATE));", connection);
                    customersPercent.Text = $"{0}%";
                    customersBorder.Background = new SolidColorBrush(Color.FromArgb(0xFF, 0xD4, 0xD4, 0xD4));
                    if (!Convert.IsDBNull(cmd.ExecuteScalar()) && !Convert.IsDBNull(cmd2.ExecuteScalar()))
                    {
                        int first = Convert.ToInt32(cmd.ExecuteScalar());
                        int second = Convert.ToInt32(cmd2.ExecuteScalar());
                        int maxNumber = Math.Max(first, second);
                        int minNumber = Math.Min(first, second);
                        double weekPercentDifference;
                        if (first > second)
                        {
                            weekPercentDifference = ((double)(second - first) / second) * 100;

                            customersBorder.Background = new SolidColorBrush(Color.FromRgb(0xA0, 0xE1, 0x82));
                            customersPercent.Text = $"{Math.Abs((int)weekPercentDifference)}%";
                        }
                        else if (first < second)
                        {
                            weekPercentDifference = ((double)(first - second) / Math.Max(first, second)) * 100;
                            customersBorder.Background = new SolidColorBrush(Color.FromArgb(0xFF, 0xEA, 0x72, 0x72));
                            if (Math.Abs((int)weekPercentDifference) < 1)
                            {
                                customersPercent.Text = $"{Math.Abs((int)weekPercentDifference)}%";
                            }
                            else
                            {
                                customersPercent.Text = $"-{Math.Abs((int)weekPercentDifference)}%";
                            }
                        }
                    }
                    else
                    {
                    }
                    customersText.Text = $"С {DateTime.Today.AddDays(-7).ToString("dd.MM.yyyy")}\nпо {DateTime.Now.ToString("dd.MM.yyyy")}";
                  
                    connection.Close();
                    connection.Open();
                    cmd = new SqlCommand($"SELECT AVG(DATEDIFF(MINUTE, Orders_Time, Orders_serving_time)) AS Average_Waiting_Time FROM Orders WHERE Orders_Date >= DATEADD(DAY, -7, GETDATE());", connection);
                    if (Convert.IsDBNull(cmd.ExecuteScalar()))
                    {
                        timeWaiting.Text = "0 минут";
                    }
                    else
                    {
                        timeWaiting.Text = $"{cmd.ExecuteScalar()} минут";
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
            else if (days == 30)
            {
                try
                {
                    connection.Open();
                    CustomersDate.Text = $"С {DateTime.Today.AddMonths(-1).ToString("dd.MM.yyyy")}\nпо {DateTime.Now.ToString("dd.MM.yyyy")}";
                    SqlCommand cmd = new SqlCommand($"SELECT SUM(Orders_Customers_Count) FROM Orders WHERE Orders_Date >= DATEADD(DAY, -30, GETDATE());", connection);
                    if (Convert.IsDBNull(cmd.ExecuteScalar()))
                    {
                        CustomersCount.Text = "0";
                    }
                    else
                    {
                        CustomersCount.Text = cmd.ExecuteScalar().ToString();
                    }
                    //REVENUE
                    cmd = new SqlCommand($"SELECT SUM(Orders_Bill) FROM Orders WHERE Orders_Date >= DATEADD(MONTH, -1, GETDATE());", connection);
                    SqlCommand cmd2 = new SqlCommand($"SELECT SUM(Orders_Bill) FROM Orders WHERE Orders_Date >= DATEADD(MONTH, -2, GETDATE()) AND Orders_Date < DATEADD(MONTH, -1, GETDATE());", connection);
                    revenueMoney.Text = $"{0}%";
                    revenueBorder.Background = new SolidColorBrush(Color.FromArgb(0xFF, 0xD4, 0xD4, 0xD4));
                    if (!Convert.IsDBNull(cmd.ExecuteScalar()) && !Convert.IsDBNull(cmd2.ExecuteScalar()))
                    {
                        int firstNumber = Convert.ToInt32(cmd.ExecuteScalar());
                        int secondNumber = Convert.ToInt32(cmd2.ExecuteScalar());
                        double weekPercentDifference;
                        if (firstNumber > secondNumber)
                        {
                            weekPercentDifference = ((double)(secondNumber - firstNumber) / secondNumber) * 100;

                            revenueBorder.Background = new SolidColorBrush(Color.FromRgb(0xA0, 0xE1, 0x82));
                            revenueMoney.Text = $"{Math.Abs((int)weekPercentDifference)}%";
                        }
                        else if (firstNumber < secondNumber)
                        {
                            weekPercentDifference = ((double)(firstNumber - secondNumber) / Math.Max(firstNumber, secondNumber)) * 100;
                            revenueBorder.Background = new SolidColorBrush(Color.FromArgb(0xFF, 0xEA, 0x72, 0x72));
                            if (Math.Abs((int)weekPercentDifference) < 1)
                            {
                                revenueMoney.Text = $"{Math.Abs((int)weekPercentDifference)}%";
                            }
                            else
                            {
                                revenueMoney.Text = $"-{Math.Abs((int)weekPercentDifference)}%";
                            }
                        }
                    }
                    else
                    {
                    }
                    revenueText.Text = $"С {DateTime.Today.AddMonths(-1).ToString("dd.MM.yyyy")}\nпо {DateTime.Now.ToString("dd.MM.yyyy")}";
                    //CUSTOMERS
                    cmd = new SqlCommand($"SELECT SUM(Orders_Customers_Count) FROM Orders WHERE Orders_Date >= DATEADD(MONTH, -1, GETDATE()) AND Orders_Date < GETDATE();", connection);
                    cmd2 = new SqlCommand($"SELECT SUM(Orders_Customers_Count) FROM Orders WHERE Orders_Date >= DATEADD(MONTH, -2, GETDATE()) AND Orders_Date < DATEADD(MONTH, -1, GETDATE());", connection);
                    customersPercent.Text = $"{0}%";
                    customersBorder.Background = new SolidColorBrush(Color.FromArgb(0xFF, 0xD4, 0xD4, 0xD4));
                    if (!Convert.IsDBNull(cmd.ExecuteScalar()) && !Convert.IsDBNull(cmd2.ExecuteScalar()))
                    {
                        int first = Convert.ToInt32(cmd.ExecuteScalar());
                        int second = Convert.ToInt32(cmd2.ExecuteScalar());
                        int maxNumber = Math.Max(first, second);
                        int minNumber = Math.Min(first, second);
                        double weekPercentDifference;
                        if (first > second)
                        {
                            weekPercentDifference = ((double)(second - first) / second) * 100;

                            customersBorder.Background = new SolidColorBrush(Color.FromRgb(0xA0, 0xE1, 0x82));
                            customersPercent.Text = $"{Math.Abs((int)weekPercentDifference)}%";
                        }
                        else if (first < second)
                        {
                            weekPercentDifference = ((double)(first - second) / Math.Max(first, second)) * 100;
                            customersBorder.Background = new SolidColorBrush(Color.FromArgb(0xFF, 0xEA, 0x72, 0x72));
                            if (Math.Abs((int)weekPercentDifference) < 1)
                            {
                                customersPercent.Text = $"{Math.Abs((int)weekPercentDifference)}%";
                            }
                            else
                            {
                                customersPercent.Text = $"-{Math.Abs((int)weekPercentDifference)}%";
                            }
                        }
                    }
                    else
                    {
                    }
                    customersText.Text = $"С {DateTime.Today.AddMonths(-1).ToString("dd.MM.yyyy")}\nпо {DateTime.Now.ToString("dd.MM.yyyy")}";
                    connection.Close();
                    connection.Open();
                    cmd = new SqlCommand($"SELECT AVG(DATEDIFF(MINUTE, Orders_Time, Orders_serving_time)) AS Average_Waiting_Time FROM Orders WHERE Orders_Date >= DATEADD(MONTH, -2, GETDATE()) AND Orders_Date < DATEADD(MONTH, -1, GETDATE());", connection);
                    if (Convert.IsDBNull(cmd.ExecuteScalar()))
                    {
                        timeWaiting.Text = "0 минут";
                    }
                    else
                    {
                        timeWaiting.Text = $"{cmd.ExecuteScalar()} минут";
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
            else if (days == 90)
            {
                try
                {
                    connection.Open();
                    CustomersDate.Text = $"С {DateTime.Today.AddMonths(-3).ToString("dd.MM.yyyy")}\nпо {DateTime.Now.ToString("dd.MM.yyyy")}";
                    SqlCommand cmd = new SqlCommand($"SELECT SUM(Orders_Customers_Count) AS TotalVisitors FROM Orders WHERE Orders_Date >= DATEADD(MONTH, -3, GETDATE());", connection);
                    if (Convert.IsDBNull(cmd.ExecuteScalar()))
                    {
                        CustomersCount.Text = "0";
                    }
                    else
                    {
                        CustomersCount.Text = cmd.ExecuteScalar().ToString();
                    }
                    //REVENUE
                    cmd = new SqlCommand($"SELECT SUM(Orders_Bill) FROM Orders WHERE Orders_Date >= DATEADD(MONTH, -3, GETDATE());", connection);
                    SqlCommand cmd2 = new SqlCommand($"SELECT SUM(Orders_Bill) FROM Orders WHERE Orders_Date >= DATEADD(MONTH, -6, GETDATE()) AND Orders_Date < DATEADD(MONTH, -3, GETDATE());", connection);
                    revenueMoney.Text = $"{0}%";
                    revenueBorder.Background = new SolidColorBrush(Color.FromArgb(0xFF, 0xD4, 0xD4, 0xD4));
                    if (!Convert.IsDBNull(cmd.ExecuteScalar()) && !Convert.IsDBNull(cmd2.ExecuteScalar()))
                    {
                        int firstNumber = Convert.ToInt32(cmd.ExecuteScalar());
                        int secondNumber = Convert.ToInt32(cmd2.ExecuteScalar());
                        double weekPercentDifference;
                        if (firstNumber > secondNumber)
                        {
                            weekPercentDifference = ((double)(secondNumber - firstNumber) / secondNumber) * 100;

                            revenueBorder.Background = new SolidColorBrush(Color.FromRgb(0xA0, 0xE1, 0x82));
                            revenueMoney.Text = $"{Math.Abs((int)weekPercentDifference)}%";
                        }
                        else if (firstNumber < secondNumber)
                        {
                            weekPercentDifference = ((double)(firstNumber - secondNumber) / Math.Max(firstNumber, secondNumber)) * 100;
                            revenueBorder.Background = new SolidColorBrush(Color.FromArgb(0xFF, 0xEA, 0x72, 0x72));
                            if (Math.Abs((int)weekPercentDifference) < 1)
                            {
                                revenueMoney.Text = $"{Math.Abs((int)weekPercentDifference)}%";
                            }
                            else
                            {
                                revenueMoney.Text = $"-{Math.Abs((int)weekPercentDifference)}%";
                            }
                        }
                    }
                    else
                    {
                    }
                    revenueText.Text = $"С {DateTime.Today.AddMonths(-1).ToString("dd.MM.yyyy")}\nпо {DateTime.Now.ToString("dd.MM.yyyy")}";
                    //CUSTOMERS
                    cmd = new SqlCommand($"SELECT SUM(Orders_Customers_Count) FROM Orders WHERE Orders_Date >= DATEADD(MONTH, -3, GETDATE());", connection);
                    cmd2 = new SqlCommand($"SELECT SUM(Orders_Customers_Count) FROM Orders WHERE Orders_Date >= DATEADD(MONTH, -6, GETDATE()) AND Orders_Date < DATEADD(MONTH, -3, GETDATE());", connection);
                    customersPercent.Text = $"{0}%";
                    customersBorder.Background = new SolidColorBrush(Color.FromArgb(0xFF, 0xD4, 0xD4, 0xD4));
                    if (!Convert.IsDBNull(cmd.ExecuteScalar()) && !Convert.IsDBNull(cmd2.ExecuteScalar()))
                    {
                        int first = Convert.ToInt32(cmd.ExecuteScalar());
                        int second = Convert.ToInt32(cmd2.ExecuteScalar());
                        int maxNumber = Math.Max(first, second);
                        int minNumber = Math.Min(first, second);
                        double weekPercentDifference;
                        if (first > second)
                        {
                            weekPercentDifference = ((double)(second - first) / second) * 100;

                            customersBorder.Background = new SolidColorBrush(Color.FromRgb(0xA0, 0xE1, 0x82));
                            customersPercent.Text = $"{Math.Abs((int)weekPercentDifference)}%";
                        }
                        else if (first < second)
                        {
                            weekPercentDifference = ((double)(first - second) / Math.Max(first, second)) * 100;
                            customersBorder.Background = new SolidColorBrush(Color.FromArgb(0xFF, 0xEA, 0x72, 0x72));
                            if (Math.Abs((int)weekPercentDifference) < 1)
                            {
                                customersPercent.Text = $"{Math.Abs((int)weekPercentDifference)}%";
                            }
                            else
                            {
                                customersPercent.Text = $"-{Math.Abs((int)weekPercentDifference)}%";
                            }
                        }
                    }
                    else
                    {
                    }
                    customersText.Text = $"С {DateTime.Today.AddMonths(-3).ToString("dd.MM.yyyy")}\nпо {DateTime.Now.ToString("dd.MM.yyyy")}";
                    connection.Close();
                    connection.Open();
                    cmd = new SqlCommand($"SELECT AVG(DATEDIFF(MINUTE, Orders_Time, Orders_serving_time)) AS Average_Waiting_Time FROM Orders WHERE Orders_Date >= DATEADD(MONTH, -3, GETDATE());", connection);
                    if (Convert.IsDBNull(cmd.ExecuteScalar()))
                    {
                        timeWaiting.Text = "0 минут";
                    }
                    else
                    {
                        timeWaiting.Text = $"{cmd.ExecuteScalar()} минут";
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
        }
        private void getOccupancy(string formattedDate)
        {
            DataContext = this;
            analyticsOccupancyHourButton.Tag = "Selected";
            occupancyColumnSeries.Title = "За этот час:";
            try
            {
                connection.Open();
                Occupancy = new ChartValues<double> { };
                Occupancy_Labels = new List<string> { };
                int hours = 0;
                if (formattedDate == $"{now.Date:yyyy-MM-dd}") 
                { 
                    if (now.Hour<10)
                    {
                        hours = 10;
                    }
                    else
                    {
                        hours = now.Hour;
                    }
                    
                }
                else { hours = 22; }
                for (int i = 10; i < hours; i++)
                {
                    SqlCommand cmd = new SqlCommand($"SELECT SUM(Orders_Customers_Count) AS Orders_Count FROM Orders WHERE CONVERT(TIME, Orders_Time) >= '{i}:00' AND CONVERT(TIME, Orders_Time) < '{i + 1}:00' AND CONVERT(DATE, Orders_Date, 104) = '{formattedDate}';", connection);
                    if (i < 22)
                    {
                        Occupancy_Labels.Add(i.ToString());
                        if (Convert.IsDBNull(cmd.ExecuteScalar()))
                        {
                            Occupancy.Add(1);
                        }
                        else
                        {
                            Occupancy.Add(Convert.ToDouble(cmd.ExecuteScalar()));
                        }
                    }
                    else { }


                }
                Occupancy_Max = Convert.ToInt32(Occupancy.Max() + (Occupancy.Max() * 0.3));
                occupancyColumnSeries.Values = Occupancy;
                occupancyLabels.Labels = Occupancy_Labels;
                occupancyMax.MaxValue = Occupancy_Max;
            }
            catch (Exception ex)
            {
                //MessageBox.Show("Error: " + ex.Message);
            }
            finally
            {
                connection.Close();
            }
        }
        private void getOccupancyByWeek()
        {
            DataContext = this;
            occupancyColumnSeries.Title = "За этот день:";
            try
            {
                connection.Open();
                Occupancy = new ChartValues<double> { };
                Occupancy_Labels = new List<string> { };

              
                string[] daysOfWeek = { "воскресенье", "понедельник", "вторник", "среда", "четверг", "пятница", "суббота" };

                DateTime today = DateTime.Today;
                int todayIndex = (int)today.DayOfWeek;

                List<DateTime> lastWeekDates = new List<DateTime>();
                for (int i = 0; i < 7; i++)
                {
                    lastWeekDates.Add(today.AddDays(-todayIndex + i));
                }

                foreach (DateTime date in lastWeekDates)
                {
                    int dayIndex = (int)date.DayOfWeek;
                    string dayName = daysOfWeek[dayIndex];

                    SqlCommand cmd = new SqlCommand($@"
                SELECT SUM(Orders_Customers_Count) AS Orders_Count
                FROM Orders
                WHERE Orders_Date = CONVERT(DATE, '{date:yyyy-MM-dd}');", connection);

                    Occupancy_Labels.Add(dayName);
                    if (Convert.IsDBNull(cmd.ExecuteScalar()))
                    {
                        Occupancy.Add(0); 
                    }
                    else
                    {
                        Occupancy.Add(Convert.ToDouble(cmd.ExecuteScalar()));
                    }
                }

                Occupancy_Max = Convert.ToInt32(Occupancy.Max() + (Occupancy.Max() * 0.3));
                occupancyColumnSeries.Values = Occupancy;
                occupancyLabels.Labels = Occupancy_Labels;
                occupancyMax.MaxValue = Occupancy_Max;
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
        private void getOccupancyForCurrentMonth()
        {
            DataContext = this;
            occupancyColumnSeries.Title = "За этот день:";
            try
            {
                connection.Open();
                Occupancy = new ChartValues<double> { };
                Occupancy_Labels = new List<string> { };

                
                DateTime firstDayOfMonth = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
                DateTime lastDayOfMonth = firstDayOfMonth.AddMonths(1).AddDays(-1);

                for (DateTime date = firstDayOfMonth; date <= lastDayOfMonth; date = date.AddDays(1))
                {
                    SqlCommand cmd = new SqlCommand($@"
                SELECT SUM(Orders_Customers_Count) AS Orders_Count
                FROM Orders
                WHERE Orders_Date = CONVERT(DATE, '{date:yyyy-MM-dd}');", connection);

                    Occupancy_Labels.Add(date.ToString("dd MMM"));
                    if (Convert.IsDBNull(cmd.ExecuteScalar()))
                    {
                        Occupancy.Add(0); 
                    }
                    else
                    {
                        Occupancy.Add(Convert.ToDouble(cmd.ExecuteScalar()));
                    }
                }

                Occupancy_Max = Convert.ToInt32(Occupancy.Max() + (Occupancy.Max() * 0.3));
                occupancyColumnSeries.Values = Occupancy;
                occupancyLabels.Labels = Occupancy_Labels;
                occupancyMax.MaxValue = Occupancy_Max;
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
        //Buttons
        private void mainButton_Click(object sender, RoutedEventArgs e)
        {
            hideAll();
            DeselectAllButtons();
            showMainPage();
            mainButton.Tag = "Selected";
        }

        private void menuButton_Click(object sender, RoutedEventArgs e)
        {
            hideAll();
            DeselectAllButtons();
            showMenuPage();
            menuButton.Tag = "Selected";
        }

        private void tablesButton_Click(object sender, RoutedEventArgs e)
        {
            hideAll();
            DeselectAllButtons();
            tablesButton.Tag = "Selected";
            showReservationsPage();
        }

        private void analyticsButton_Click(object sender, RoutedEventArgs e)
        {
            hideAll();
            DeselectAllButtons();
            analyticsButton.Tag = "Selected";
            analyticsOccupancyHourButton.Tag = "Selected";
            showAnalyticsPage();
            analytcsGrid.Visibility = Visibility.Visible;
        }
        private void PersonalButton_Click(object sender, RoutedEventArgs e)
        {
            hideAll();
            DeselectAllButtons();
            showPersonalPage();
            garconsTopBorder.Visibility = Visibility.Visible;
            garconsGrid.Visibility = Visibility.Visible;
            PersonalButton.Tag = "Selected";
        }
        private void infoButton_Click(object sender, RoutedEventArgs e)
        {
            hideAll();
            DeselectAllButtons();
            infoBorder.Visibility = Visibility.Visible;
            infoGrid.Visibility = Visibility.Visible;
            DataContext = this;
            generateHardwareId();
            infoHardwareID.Text = readHardwareId();
            if (CheckDatabaseConnection() == true)
            {
                infoDBStatus.Foreground = Brushes.Green;
                infoDBStatus.Text = "Стабильное";
            }
            else
            {
                infoDBStatus.Foreground = Brushes.Red;
                infoDBStatus.Text = "Отсутствует";
            }
            infoButton.Tag = "Selected";
        }
        //Reservations
        private void getTablesCount()
        {
            DataContext = this;
            try
            {
                connection.Open();
                SqlCommand cmd = new SqlCommand($"SELECT COUNT(*) FROM Reservation WHERE CONVERT(DATE, Reservation_Date, 104) = '{now.Date:yyyy-MM-dd}' AND Reservation_Status = 'Активна' AND Reservation_Start < CONVERT(TIME, GETDATE()) AND Reservation_End > CONVERT(TIME, GETDATE());", connection);
                if (Convert.IsDBNull(cmd.ExecuteScalar()) || ((int)cmd.ExecuteScalar() == 0))
                {
                    tablesActiveReservations.Text = "0";
                }
                else
                {
                    tablesActiveReservations.Text = cmd.ExecuteScalar().ToString();
                }

                cmd = new SqlCommand($"SELECT COUNT(*) FROM Tables WHERE Tables_Status = 'Занят'", connection);
                if (Convert.IsDBNull(cmd.ExecuteScalar()) || ((int)cmd.ExecuteScalar() == 0))
                {
                    tablesBusyTables.Text = "0";
                }
                else
                {
                    tablesBusyTables.Text = cmd.ExecuteScalar().ToString();
                }
                tablesFreeTables.Text = $"{16 - (Convert.ToInt32(tablesActiveReservations.Text)) - (Convert.ToInt32(tablesBusyTables.Text))}";
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

        public void ReservationUpdater()
        {
            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(5);
            timer.Tick += Timer_Tick;
            timer.Start();
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            CheckAndUpdateTableStatus();
            getTablesCount();
        }
        public void CheckAndUpdateTableStatus()
        {
            using (SqlConnection connection = new SqlConnection(ConfigurationManager.ConnectionStrings["constr"].ConnectionString))
            {
                try
                {
                    connection.Open();
                    SqlCommand cmd = new SqlCommand($"UPDATE Reservation SET Reservation_Status='Закрыта' WHERE Reservation_Date < '{now.Date:yyyy-MM-dd}' OR (Reservation_Date = '{now.Date:yyyy-MM-dd}' AND Reservation_End <= CONVERT(TIME, GETDATE()));", connection);
                    cmd.ExecuteScalar();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error: " + ex.Message);
                }
            }
            using (SqlConnection connection = new SqlConnection(ConfigurationManager.ConnectionStrings["constr"].ConnectionString))
            {
                try
                {
                    connection.Open();
                    for (int tableNumber = 1; tableNumber <= 16; tableNumber++)
                    {

                        string tablesQuery = $"SELECT COUNT(*) FROM Tables WHERE Tables_ID = {tableNumber} AND Tables_Status = 'Занят'";
                        SqlCommand tablesCommand = new SqlCommand(tablesQuery, connection);
                        int reservationCount = (int)tablesCommand.ExecuteScalar();
                        if (reservationCount > 0)
                        {
                            UpdateTableStatusBusy(tableNumber, reservationCount > 0);
                        }
                        else
                        {
                            DateTime currentDateTime = DateTime.Now;
                            string reservationQuery = $"SELECT COUNT(*) FROM Reservation " +
                                                       $"WHERE Tables_ID = {tableNumber} " +
                                                       $"AND Reservation_Date = '{currentDateTime.Date:yyyy-MM-dd}' " +
                                                       $"AND '{currentDateTime:HH:mm:ss}' BETWEEN Reservation_Start AND Reservation_End AND Reservation_Status='Активна'";
                            SqlCommand reservationCommand = new SqlCommand(reservationQuery, connection);
                            reservationCount = (int)reservationCommand.ExecuteScalar();
                            UpdateTableStatus(tableNumber, reservationCount > 0);
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error: " + ex.Message);
                }
            }

        }

        private void UpdateTableStatus(int tableNumber, bool isOccupied)
        {
            string status = isOccupied ? "Забронирован" : "Свободен";
            SetTableStatus(tableNumber, status);
        }
        private void UpdateTableStatusBusy(int tableNumber, bool isOccupied)
        {
            string status = isOccupied ? "Занят" : "Свободен";
            SetTableStatus(tableNumber, status);
        }

        private void SetTableStatus(int tableNumber, string status)
        {
            switch (tableNumber)
            {
                case 1:
                    tableNumber1.Status = status;
                    break;
                case 2:
                    tableNumber2.Status = status;
                    break;
                case 3:
                    tableNumber3.Status = status;
                    break;
                case 4:
                    tableNumber4.Status = status;
                    break;
                case 5:
                    tableNumber5.Status = status;
                    break;
                case 6:
                    tableNumber6.Status = status;
                    break;
                case 7:
                    tableNumber7.Status = status;
                    break;
                case 8:
                    tableNumber8.Status = status;
                    break;
                case 9:
                    tableNumber9.Status = status;
                    break;
                case 10:
                    tableNumber10.Status = status;
                    break;
                case 11:
                    tableNumber11.Status = status;
                    break;
                case 12:
                    tableNumber12.Status = status;
                    break;
                case 13:
                    tableNumber13.Status = status;
                    break;
                case 14:
                    tableNumber14.Status = status;
                    break;
                case 15:
                    tableNumber15.Status = status;
                    break;
                case 16:
                    tableNumber16.Status = status;
                    break;
                default:
                    break;
            }
        }
        //Mac Gen 
        public static string generateHardwareId()
        {

            string macAddress = getMacAddress();
            if (!string.IsNullOrEmpty(macAddress))
            {

                using (MD5 md5 = MD5.Create())
                {

                    byte[] hashBytes = md5.ComputeHash(System.Text.Encoding.UTF8.GetBytes(macAddress));
                    string hardwareId = BitConverter.ToString(hashBytes).Replace("-", "").ToLower().Substring(0, 10);

                    var dataToSave = new { HardwareId = hardwareId };

                    string filePath = $"{getProjectFolderPath()}\\Services\\hardware_id.json";
                    if (!File.Exists(filePath))
                    {
                        File.WriteAllText(filePath, JsonConvert.SerializeObject(dataToSave));
                    }
                    return hardwareId;
                }
            }
            else
            {
                throw new Exception("Не удалось получить MAC-адрес");
            }
        }
        public static string readHardwareId()
        {
            string filePath = $"{getProjectFolderPath()}\\Services\\hardware_id.json";
            if (File.Exists(filePath))
            {

                string json = File.ReadAllText(filePath);

                JObject jsonObject = JObject.Parse(json);

                return (string)jsonObject["HardwareId"];
            }
            else
            {
                throw new FileNotFoundException("Файл с уникальным идентификатором не найден");
            }
        }
        private static string getMacAddress()
        {
            try
            {

                ManagementClass mc = new ManagementClass("Win32_NetworkAdapterConfiguration");
                ManagementObjectCollection moc = mc.GetInstances();

                string macAddress = string.Empty;
                foreach (ManagementObject mo in moc)
                {
                    if ((bool)mo["IPEnabled"])
                    {
                        macAddress = mo["MacAddress"].ToString();
                        break;
                    }
                }
                return macAddress;
            }
            catch (Exception ex)
            {

                Console.WriteLine("Ошибка при получении MAC-адреса: " + ex.Message);
                return null;
            }
        }
        private void menuResetButton_Click(object sender, RoutedEventArgs e)
        {
            getMenu("");
            MenuCategoryComboBox.SelectedItem = null;
        }
        //DBCON
        public static bool CheckDatabaseConnection()
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(ConfigurationManager.ConnectionStrings["constr"].ConnectionString))
                {
                    connection.Open();
                    return true;
                }
            }
            catch (SqlException ex)
            {
                Console.WriteLine("Ошибка подключения к базе данных: " + ex.Message);
                return false;
            }
        }
        //Buttons
        private void analyticsDayButton_Click(object sender, RoutedEventArgs e)
        {
            DeselectAllAnalyticsButton();
            getAnalyticsCount(1);
            analyticsDayButton.Tag = "Selected";
        }
        private void analyticsWeekButton_Click(object sender, RoutedEventArgs e)
        {
            DeselectAllAnalyticsButton();
            getAnalyticsCount(7);
            analyticsWeekButton.Tag = "Selected";
        }
        private void analyticsMonthButton_Click(object sender, RoutedEventArgs e)
        {
            DeselectAllAnalyticsButton();
            getAnalyticsCount(30);
            analyticsMonthButton.Tag = "Selected";
        }
        private void analyticsQuarterButton_Click(object sender, RoutedEventArgs e)
        {
            DeselectAllAnalyticsButton();
            getAnalyticsCount(90);
            analyticsQuarterButton.Tag = "Selected";
        }
        private void analyticsOccupancyHourButton_Click(object sender, RoutedEventArgs e)
        {
            analyticsOccupancyHourButton.Tag = "Selected";
            analyticsOccupancyDayButton.Tag = null;
            getOccupancy($"{now.Date:yyyy-MM-dd}");
        }

        private void analyticsOccupancyDayButton_Click(object sender, RoutedEventArgs e)
        {
            analyticsOccupancyDayButton.Tag = "Selected";
            analyticsOccupancyHourButton.Tag = null;
            getOccupancyForCurrentMonth();
        }
        private void DeselectAllButtons()
        {
            mainButton.Tag = null;
            menuButton.Tag = null;
            tablesButton.Tag = null;
            analyticsButton.Tag = null;
            infoButton.Tag = null;
            PersonalButton.Tag = null;
        }
        private void DeselectAllAnalyticsButton()
        {
            analyticsDayButton.Tag = null;
            analyticsWeekButton.Tag = null;
            analyticsMonthButton.Tag = null;
            analyticsQuarterButton.Tag = null;
        }
        private void hideAll()
        {
            mainDishBorder.Visibility = Visibility.Collapsed;
            mainHiBorder.Visibility = Visibility.Collapsed;
            main3Border.Visibility = Visibility.Collapsed;
           
            mainTop.Visibility = Visibility.Collapsed;
            mainTopDish.Visibility = Visibility.Collapsed;

            menuTopBorder.Visibility = Visibility.Collapsed;
            menuGrid.Visibility = Visibility.Collapsed;

            analyticsTopBorder.Visibility = Visibility.Collapsed;
            analytcsGrid.Visibility = Visibility.Collapsed;

            tablesBorder.Visibility = Visibility.Collapsed;
            reservationsGrid.Visibility = Visibility.Collapsed;

            infoBorder.Visibility = Visibility.Collapsed;
            infoGrid.Visibility = Visibility.Collapsed;

            garconsGrid.Visibility = Visibility.Collapsed;

            garconsTopBorder.Visibility = Visibility.Collapsed;
            garconsGrid.Visibility = Visibility.Collapsed;
        }
        private void showMainPage()
        {
            getOrders();
            getLast3MonthOrders();
            getTopDish();
            getOrdersCount();
            getMenu("");
            mainHiBorder.Visibility = Visibility.Visible;
            mainDishBorder.Visibility = Visibility.Visible;
            main3Border.Visibility = Visibility.Visible;

            mainTop.Visibility = Visibility.Visible;
            mainTopDish.Visibility = Visibility.Visible;
        }
        private void showMenuPage()
        {
            menuTopBorder.Visibility = Visibility.Visible;
            menuGrid.Visibility = Visibility.Visible;
            LoadMenuCategories();
            getMenu("");
        }
        public void showAnalyticsPage()
        {
            getOccupancy($"{now.Date:yyyy-MM-dd}");
            getOccupancyByWeek();
            getAnalyticsCount(1);
            analyticsDayButton.Tag = "Selected";
            analyticsTopBorder.Visibility = Visibility.Visible;
            analytcsGrid.Visibility = Visibility.Visible;
        }
        public void showPersonalPage()
        {
            LoadMonthlyGarconSchedule(1, DateTime.Now);
            LoadGarconsToComboBox();
            LoadGarconSummary();
            LoadGarconsToScheduleComboBox();
        }
        public void showReservationsPage()
        {
            getTablesCount();
            ReservationUpdater();
            tablesBorder.Visibility = Visibility.Visible;
            reservationsGrid.Visibility = Visibility.Visible;
        }
        private void Border_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                this.DragMove();
            }
        }
        private bool IsMaximize = false;
        private void Border_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                if (IsMaximize)
                {
                    this.WindowState = WindowState.Normal;
                    this.Width = 1280;
                    this.Height = 780;

                    IsMaximize = false;
                }
                else
                {
                    this.WindowState = WindowState.Maximized;

                    IsMaximize = true;
                }
            }
        }
        private void menuListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (menuListView.SelectedIndex >= 0)
            {
                GarconMenu items = (GarconMenu)menuListView.Items.GetItemAt(menuListView.SelectedIndex);
                menuEditWindow secondWindow = new menuEditWindow(Convert.ToInt32(items.ID), false);
                bool? result = secondWindow.ShowDialog();
                if (result == true)
                {
                    getMenu("");
                }
            }
        }

        private void datePicker_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            DateTime selectedDate = datePicker.SelectedDate.GetValueOrDefault();
            string formattedDate = selectedDate.ToString("yyyy-MM-dd");
            getOccupancy(formattedDate);
            analyticsOccupancyDayButton.Tag = null;
        }

        private void printButton_Click(object sender, RoutedEventArgs e)
        {
            menuPrintWindow secondWindow = new menuPrintWindow();
            secondWindow.ShowDialog();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            tablesReservations tablesWindow = new tablesReservations();
            tablesWindow.ShowDialog();
        }

        private void AddDishButton_Click(object sender, RoutedEventArgs e)
        {
                menuEditWindow secondWindow = new menuEditWindow(0, true);
                secondWindow.ShowDialog();
        }
       

        private void AddOptionButton_Click(object sender, RoutedEventArgs e)
        {
            TopingWindow tablesWindow = new TopingWindow();
            tablesWindow.ShowDialog();
        }

        private void deleteScheduleButton_Click(object sender, RoutedEventArgs e)
        {
            int selectedGarconId;
            string selectedGarconName;
            if (scheduleComboBox.SelectedValue != null)
            {
                selectedGarconId = (int)scheduleComboBox.SelectedValue;
                selectedGarconName = scheduleComboBox.DisplayMemberPath;
                using (SqlConnection connection = new SqlConnection(ConfigurationManager.ConnectionStrings["constr"].ConnectionString))
                {
                    try
                    {
                        connection.Open();
                        SqlCommand cmd = new SqlCommand($"DELETE FROM Garcon_Schedule WHERE Garcon_ID = {selectedGarconId} AND Work_Date >= GETDATE();", connection);
                        var Result = MessageBox.Show($"Вы точно хотите удалить расписание для сотрудника?", "Предупреждение о внесении изменений", MessageBoxButton.YesNo, MessageBoxImage.Question);
                        if (Result == MessageBoxResult.Yes)
                        {
                            cmd.ExecuteNonQuery();
                            MessageBox.Show("Расписание удалено!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                            LoadMonthlyGarconSchedule(selectedGarconId, DateTime.Now);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Ошибка: " + ex.Message);
                    }
                }
            }
            
        }

        private void ToStopListButton_Click(object sender, RoutedEventArgs e)
        {
            StopListWindow stopListWindow = new StopListWindow();
            stopListWindow.ShowDialog();
        }
    }

}
