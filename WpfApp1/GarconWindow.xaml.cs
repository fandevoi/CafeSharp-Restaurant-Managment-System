using Microsoft.ReportingServices.Diagnostics.Internal;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.IO;
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
using System.Windows.Threading;
using System.Text.RegularExpressions;
using System.Transactions;
using System.Management.Instrumentation;
using System.Security.Cryptography;
using Newtonsoft.Json.Linq;
using System.Windows.Media.Animation;
using System.Globalization;
using System.Threading;
using System.Timers;
using System.Data;
using Brushes = System.Windows.Media.Brushes;
using Color = System.Windows.Media.Color;
using FontFamily = System.Windows.Media.FontFamily;
using Mysqlx.Crud;
using System.Collections;
using System.Data.Common;
using System.Dynamic;

namespace WpfApp1
{
    public class GarconMenu
    {
        public string ID { get; set; }
        public string Name { get; set; }
        public int Price { get; set; }
        public int Discount { get; set; }
        public ImageSource Image { get; set; }
        public string Type { get; set; }
    }
    public class GarconDish
    {
        public int Count { get; set; }
        public string DishName { get; set; }
        public int DishPrice { get; set; }
        public int DishDiscount { get; set; }
        public List<DishSelectedOption> SelectedOptions { get; set; } = new List<DishSelectedOption>();
        public string OptionsText { get; set; }
        public int GuestNumber { get; set; }
    }
    public class DishSelectedOption
    {
        public int OptionId { get; set; }
        public double OptionPrice { get; set; }
        public string OptionName { get; set; }
    }
    public class DishOption
    {
        public int OptionId { get; set; }
        public string OptionName { get; set; }
        public string OptionType { get; set; } // Например: "checkbox" или "radiobutton"
        public bool IsImportant { get; set; }  // Это флаг важности опции
    }
    public class ScheduleEntry
    {
        public DateTime WorkDate { get; set; }
        public TimeSpan ShiftStart { get; set; }
        public TimeSpan ShiftEnd { get; set; }
    }
    public class Guest
    {
        public int GuestNumber { get; set; }
        public List<GarconDish> Dishes { get; set; } = new List<GarconDish>();

        public double TotalPrice => Dishes.Sum(d => d.DishPrice + d.SelectedOptions.Sum(opt => opt.OptionPrice));
    }
    public class TableOrder
    {
        public int TableNumber { get; set; }
        public List<Guest> Guests { get; set; } = new List<Guest>();
    }
    public class GuestDish
    {
        public int GuestNumber { get; set; }
        public List<GarconDish> Dishes { get; set; } = new List<GarconDish>();

        public int TotalPrice => Dishes.Sum(d => d.Count * d.DishPrice);
    }
    public class OrderInfo
    {
        public int OrderId { get; set; }
        public int TableId { get; set; }
        public int TotalPrice { get; set; }
        public DateTime OrderDateTime { get; set; }
    }
    public partial class GarconWindow : Window
    {
        public int totalPrice;
        private int activeGuestIndex = -1;
        private DispatcherTimer timer; DateTime now = DateTime.Now;
        private List<GarconDish> selectedDishes = new List<GarconDish>();
        private List<Guest> guests = new List<Guest>();
        private List<TableOrder> allTableOrders = new List<TableOrder>();

        public int SelectedTable;
        public int SelectedGarcon;
        public string PaymentMethodGlobal = "Картой";

        public class DaySchedule
        {
            public DateTime Date { get; set; }
            public string ShiftTime { get; set; }
        }

        private System.Timers.Timer ClockTimer;

        private readonly Random _random = new Random();

        private readonly Ellipse[] dots;
        private int currentIndex = 0;
        private readonly DispatcherTimer Dottimer;

        private DispatcherTimer _timer;
        public string nowChangedCountDishName { get; set; }
        private SqlConnection connection = new SqlConnection(ConfigurationManager.ConnectionStrings["constr"].ConnectionString);
        public GarconWindow()
        {
            InitializeComponent();
            SelectedGarcon = 1;
            //WindowState = WindowState.Maximized;
            //WindowStyle = WindowStyle.None;
            ReservationUpdater();
            guests = new List<Guest>();
            guests.Add(new Guest { GuestNumber = 1 });
            activeGuestIndex = 0;
            selectedDishes = guests[activeGuestIndex].Dishes;
            UpdateGuestListUI();
            getMenu("");
            CheckDatabaseConnection();
            LoadCurrentOrders("WHERE o.Orders_Status != 'Завершен' AND o.Orders_Status != 'Отменён'");
            this.Loaded += new RoutedEventHandler(Window_Loaded);
            GarconNameText.Text = GetGarconName(SelectedGarcon,true);
            DateTime now = DateTime.Now;
            StartUpdatingTextBlocks();
            LoadMonthlyGarconSchedule(SelectedGarcon, now);

            dots = new Ellipse[]
           {
                Dot0, Dot1, Dot2, Dot3, Dot4, Dot5, Dot6, Dot7, Dot8, Dot9
           };

            Dottimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(160)
            };
            Dottimer.Tick += AnimateDots;
           

        }
        private void StartUpdatingTextBlocks()
        {
            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromSeconds(30);
            _timer.Tick += UpdateTextBlocks;
            _timer.Start();

            UpdateTextBlocks(null, null);
        }

        private void UpdateTextBlocks(object sender, EventArgs e)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings["constr"].ConnectionString))
                {
                    conn.Open();

                    // 1. Блюдо дня — выберем первое блюдо (можно по типу или по дате обновления)
                    using (SqlCommand cmd = new SqlCommand(
                        "SELECT TOP 1 M.Menu_Name " +
                        "FROM Order_Dishes OD " +
                        "JOIN Orders O ON OD.Orders_ID = O.Orders_ID " +
                        "JOIN Menu M ON OD.Menu_ID = M.Menu_ID " +
                        "WHERE CAST(O.Orders_Date AS DATE) = CAST(GETDATE() AS DATE) " +
                        "GROUP BY M.Menu_Name ORDER BY SUM(OD.Quantity) DESC;", conn))
                    {
                        var result = cmd.ExecuteScalar();
                        DishOfTheDay.Text = result != null ? result.ToString() : "Нет данных";
                    }

                    // 2. Сколько заказов у конкретного официанта
                    using (SqlCommand cmd = new SqlCommand(
                        "SELECT COUNT(*) FROM Orders WHERE Garcon_ID = @GarconID AND Orders_Date = CONVERT(VARCHAR(30),GETDATE(),105) ", conn))
                    {
                        cmd.Parameters.AddWithValue("@GarconID", SelectedGarcon);
                        var result = cmd.ExecuteScalar();
                        GarconHowManyOrdersToday.Text = result?.ToString() ?? "0";
                    }

                    // 3. Сколько блюд в стоп-листе (Menu_IsStop = 1)
                    using (SqlCommand cmd = new SqlCommand(
                        "SELECT COUNT(*) FROM Menu WHERE Menu_IsStop = 1", conn))
                    {
                        var result = cmd.ExecuteScalar();
                        StopListCount.Text = result?.ToString() ?? "0";
                    }
                }
            }
            catch (Exception ex)
            {
                // Обработка ошибок — логируйте или выводите куда-нибудь
                Console.WriteLine("Ошибка при обновлении данных: " + ex.Message);
            }
        }
        private void AnimateDots(object sender, EventArgs e)
        {
            for (int i = 0; i < dots.Length; i++)
            {
                int diff = Math.Min(
                    (i - currentIndex + dots.Length) % dots.Length,
                    (currentIndex - i + dots.Length) % dots.Length
                );

                double targetOpacity = 0.2;
                Color targetColor = Colors.Gray;

                switch (diff)
                {
                    case 0:
                        targetOpacity = 1.0;
                        targetColor = Colors.White;
                        break;
                    case 1:
                        targetOpacity = 0.7;
                        targetColor = Color.FromRgb(200, 200, 200);
                        break;
                    case 2:
                        targetOpacity = 0.5;
                        targetColor = Color.FromRgb(150, 150, 150);
                        break;
                    default:
                        targetOpacity = 0.2;
                        targetColor = Colors.Gray;
                        break;
                }

                AnimateEllipse(dots[i], targetOpacity, targetColor);
            }

            currentIndex = (currentIndex + 1) % dots.Length;
        }

        private void AnimateEllipse(Ellipse ellipse, double toOpacity, Color toColor)
        {
            var opacityAnimation = new DoubleAnimation
            {
                To = toOpacity,
                Duration = TimeSpan.FromMilliseconds(300),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut }
            };
            ellipse.BeginAnimation(UIElement.OpacityProperty, opacityAnimation);

            var colorAnimation = new ColorAnimation
            {
                To = toColor,
                Duration = TimeSpan.FromMilliseconds(300),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut }
            };

            if (ellipse.Fill is SolidColorBrush brush)
            {
                // Чтобы анимация цвета сработала, кисть должна быть динамической (не frozen)
                if (brush.IsFrozen)
                    ellipse.Fill = brush = brush.Clone();

                brush.BeginAnimation(SolidColorBrush.ColorProperty, colorAnimation);
            }
        }
        private void ToStopListButton_Click(object sender, RoutedEventArgs e)
        {
            StopListWindow stopListWindow = new StopListWindow();
            stopListWindow.ShowDialog();
        }
        private List<GarconDish> LoadDishesForOrder(int orderId)
        {
            List<GarconDish> result = new List<GarconDish>();

            using (SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings["constr"].ConnectionString))
            {
                conn.Open();

                SqlCommand cmd = new SqlCommand(@"
            SELECT od.Count, m.Menu_Name, m.Menu_Price, m.Menu_Discount,
                   do.Option_ID, do.Option_Name, do.Option_Price
            FROM Order_Dishes od
            JOIN Menu m ON od.Menu_ID = m.Menu_ID
            LEFT JOIN Order_Dish_Options odo ON odo.Order_ID = od.Orders_ID AND odo.Menu_ID = m.Menu_ID
            LEFT JOIN Dish_Options do ON do.Option_ID = odo.Option_ID
            WHERE od.Orders_ID = @OrderID
        ", conn);

                cmd.Parameters.AddWithValue("@OrderID", orderId);

                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    Dictionary<string, GarconDish> dishMap = new Dictionary<string, GarconDish>();

                    while (reader.Read())
                    {
                        string dishName = reader["Menu_Name"].ToString();
                        int count = Convert.ToInt32(reader["Count"]);
                        int price = Convert.ToInt32(reader["Menu_Price"]);
                        int discount = Convert.ToInt32(reader["Menu_Discount"]);

                        if (!dishMap.ContainsKey(dishName))
                        {
                            dishMap[dishName] = new GarconDish
                            {
                                Count = count,
                                DishName = dishName,
                                DishPrice = (price - (price / 100) * discount) * count,
                                DishDiscount = (price / 100) * discount * count
                            };
                        }

                        if (!reader.IsDBNull(reader.GetOrdinal("Option_ID")))
                        {
                            var option = new DishSelectedOption
                            {
                                OptionId = Convert.ToInt32(reader["Option_ID"]),
                                OptionName = reader["Option_Name"].ToString(),
                                OptionPrice = Convert.ToDouble(reader["Option_Price"])
                            };

                            dishMap[dishName].SelectedOptions.Add(option);
                        }
                    }

                    // Формируем OptionText
                    foreach (var dish in dishMap.Values)
                    {
                        dish.OptionsText = string.Join(", ",
                            dish.SelectedOptions.Select(opt => $"Опция {opt.OptionId}"));
                        result.Add(dish);
                    }
                }
            }

            return result;
        }
        //РАСПИСАНИЕ
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

                        if (start.HasValue && end.HasValue)
                        {
                            string shiftText = $"{start.Value:hh\\:mm} - {end.Value:hh\\:mm}";
                            scheduleMap[date.Date] = shiftText;
                        }
                        else
                        {
                            scheduleMap[date.Date] = "Смена не указана";
                        }
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
                    dayBackground = new SolidColorBrush(Color.FromArgb(150, 185, 74, 22)); // ярко-синий
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
                    Foreground = Brushes.White,
                    HorizontalAlignment = HorizontalAlignment.Right
                },
                new TextBlock
                {
                    Text = shift,
                    FontSize = 17,
                    FontFamily = new FontFamily("Fonts/#Inter Medium"),
                    TextWrapping = TextWrapping.Wrap,
                    Foreground = Brushes.White,
                    Margin = new Thickness(0, 4, 0, 0)
                }
            }
                    }
                });
            }

        }

        

        private void ScheduleButtonClick(object sender, RoutedEventArgs e)
        {
            MainGrid.Visibility = Visibility.Collapsed;
            ScheduleGrid.Visibility = Visibility.Visible;
        }

       
        //ЗАГРУЗКА АКТИВНЫХ ЗАКАЗОВ

        private void LoadCurrentOrders(string StatusParam)
        {
            List<dynamic> orders = new List<dynamic>();

            using (SqlConnection connection = new SqlConnection(ConfigurationManager.ConnectionStrings["constr"].ConnectionString))
            {
                connection.Open();

                        SqlCommand cmd = new SqlCommand($@"
                    SELECT 
            o.Orders_ID,
            o.Orders_Date,
	        CONVERT(varchar(5), o.Orders_Time, 108) AS OrderTime,
	        CONVERT(varchar(5), o.Orders_Serving_Time, 108) AS ServingTime,
            o.Orders_Status,
            o.Orders_Customers_Count,
            o.Orders_Bill,
            t.Tables_ID,
            g.Garcon_Name,
            m.Menu_Name,
            STRING_AGG(do.Option_Name, ', ') WITHIN GROUP (ORDER BY do.Option_Name) AS Options
        FROM Orders o
        JOIN Tables t ON o.Table_ID = t.Tables_ID
        JOIN Garcon g ON o.Garcon_ID = g.Garcon_ID
        JOIN Order_Dishes od ON od.Orders_ID = o.Orders_ID
        JOIN Menu m ON od.Menu_ID = m.Menu_ID
        LEFT JOIN Order_Dish_Options odo ON odo.Order_ID = o.Orders_ID AND odo.Menu_ID = m.Menu_ID
        LEFT JOIN Dish_Options do ON odo.Option_ID = do.Option_ID
        {StatusParam}
        GROUP BY o.Orders_ID, o.Orders_Date, o.Orders_Time, o.Orders_Serving_Time, 
                 o.Orders_Status, o.Orders_Customers_Count, o.Orders_Bill, 
                 t.Tables_ID, g.Garcon_Name, m.Menu_Name
        ORDER BY o.Orders_Date DESC, o.Orders_Time DESC;", connection);

                // Временное хранилище блюд по ID заказа
                var orderMap = new Dictionary<int, dynamic>();

                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        int orderId = (int)reader["Orders_ID"];
                        string dishName = reader["Menu_Name"].ToString();
                        string options = reader["Options"] == DBNull.Value ? "" : reader["Options"].ToString();

                        string dishWithOptions = string.IsNullOrWhiteSpace(options)
                            ? dishName
                            : $"{dishName} ({options})";

                        if (!orderMap.ContainsKey(orderId))
                        {
                            orderMap[orderId] = new
                            {
                                OrderID = orderId,
                                OrderDate = Convert.ToDateTime(reader["Orders_Date"]).ToShortDateString(),
                                OrderTime = reader["OrderTime"].ToString(),
                                ServingTime = reader["ServingTime"].ToString(),
                                Status = reader["Orders_Status"].ToString(),
                                GuestsCount = reader["Orders_Customers_Count"],
                                TotalBill = reader["Orders_Bill"],
                                TableNumber = reader["Tables_ID"],
                                Garcon = reader["Garcon_Name"].ToString(),
                                Dishes = new List<string>()
                            };
                        }

                        ((List<string>)orderMap[orderId].Dishes).Add(dishWithOptions);
                    }
                }

                // Форматируем блюда в строку с переносом
                foreach (var entry in orderMap.Values)
                {
                    var dishes = (List<string>)entry.Dishes;
                    StringBuilder dishBuilder = new StringBuilder();

                    for (int i = 0; i < dishes.Count; i++)
                    {
                        dishBuilder.Append(dishes[i]);

                        if ((i + 1) % 3 == 0 && i != dishes.Count - 1)
                            dishBuilder.Append(",\n");
                        else if (i != dishes.Count - 1)
                            dishBuilder.Append(", ");
                    }

                    orders.Add(new
                    {
                        entry.OrderID,
                        entry.OrderDate,
                        entry.OrderTime,
                        entry.ServingTime,
                        entry.Status,
                        entry.GuestsCount,
                        entry.TotalBill,
                        entry.TableNumber,
                        entry.Garcon,
                        DishDisplay = dishBuilder.ToString()
                    });
                }
            }

            currentOrdersDataGrid.ItemsSource = orders;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            ClockTimer = new System.Timers.Timer(1000); 
            ClockTimer.Elapsed += ClockTimer_Elapsed;
            ClockTimer.Start();
           
        }
        private void ClockTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                DateTime now = DateTime.Now;
                ClockText.Text = now.ToString("HH:mm:ss");
                DateText.Text = now.ToString("dddd, dd MMMM yyyy", new CultureInfo("ru-RU"));
            });
        }
        public void CheckDatabaseConnection()
        {
            HardwareIDText.Text = $"ID устройства: {readHardwareId()}";
            try
            {
                using (SqlConnection connection = new SqlConnection(ConfigurationManager.ConnectionStrings["constr"].ConnectionString))
                {
                    connection.Open();
                    DBInfoText.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(85, 179, 60));
                    NetIcon.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(85, 179, 60));
                    DBInfoText.Text = "Подключение стабильное";
                }
            }
            catch (SqlException ex)
            {
                Console.WriteLine("Ошибка подключения к базе данных: " + ex.Message);
                DBInfoText.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(179, 60, 60));
                NetIcon.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(179, 60, 60));
                DBInfoText.Text = "Нет подключения";
            }
        }
        private TableOrder GetOrCreateTableOrder(int tableNumber)
        {
            var existingOrder = allTableOrders.FirstOrDefault(t => t.TableNumber == tableNumber);
            if (existingOrder == null)
            {
                existingOrder = new TableOrder { TableNumber = tableNumber };
                allTableOrders.Add(existingOrder);
            }
            return existingOrder;
        }
        private void AddGuestButton_Click(object sender, RoutedEventArgs e)
        {
            var guest = new Guest { GuestNumber = guests.Count + 1 };
            guests.Add(guest);
            activeGuestIndex = guests.Count - 1;
            UpdateGuestListUI();
            UpdateOrderListView();
        }
        private void DeleteGuestButton_Click(object sender, RoutedEventArgs e)
        {
            if (guests.Count <= 1)
            {
                ShowNotification("Нельзя удалить последнего гостя!");
                return;
            }

            if (guestListBox.SelectedIndex >= 0 && guestListBox.SelectedIndex < guests.Count)
            {
                int indexToRemove = guestListBox.SelectedIndex;
                guests.RemoveAt(indexToRemove);

                // Обновляем номера гостей
                for (int i = 0; i < guests.Count; i++)
                {
                    guests[i].GuestNumber = i + 1;
                }

                // Корректируем активный индекс
                activeGuestIndex = indexToRemove - 1;
                guestListBox.SelectedItem = 0;
                UpdateGuestListUI();
                UpdateOrderListView();
            }
        }

        private void guestListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            activeGuestIndex = guestListBox.SelectedIndex;
            UpdateOrderListView();
        }

        private void UpdateGuestListUI()
        {
            guestListBox.ItemsSource = null;
            guestListBox.ItemsSource = guests.Select(g => $"Гость {g.GuestNumber}").ToList();
            guestListBox.SelectedItem = activeGuestIndex;

        }

        private void UpdateOrderListView()
        {
            if (activeGuestIndex >= 0 && activeGuestIndex < guests.Count)
            {
                orderListView.ItemsSource = null;
                orderListView.ItemsSource = guests[activeGuestIndex].Dishes;
                totalPriceButton.Text = $"{guests.Sum(g => g.TotalPrice)} ₽";
            }
        }

        private void PlaceOrder_Click(object sender, RoutedEventArgs e)
        {
            if (guests.Count == 0 || guests.All(g => g.Dishes.Count == 0))
            {
                ShowNotification("Добавьте блюда хотя бы одному гостю.");
                return;
            }
            using (SqlConnection connection = new SqlConnection(ConfigurationManager.ConnectionStrings["constr"].ConnectionString))
            {
                try
                {
                    connection.Open();
                    SqlTransaction transaction = connection.BeginTransaction();

                    SqlCommand orderCmd = new SqlCommand(
                        "INSERT INTO Orders (Orders_Date, Orders_Bill, Orders_Time, Orders_Status, Orders_Customers_Count, Garcon_ID, Table_ID) " +
                        "OUTPUT INSERTED.Orders_ID VALUES (@Date, @Bill, @Time, @Status, @CustomersCount, @GarconID, @TableID)",
                        connection, transaction);
                    orderCmd.Parameters.AddWithValue("@Date", DateTime.Now.Date);
                    orderCmd.Parameters.AddWithValue("@Bill", guests.Sum(g => g.TotalPrice)); 
                    orderCmd.Parameters.AddWithValue("@Time", DateTime.Now.TimeOfDay);
                    orderCmd.Parameters.AddWithValue("@Status", "В обработке");
                    orderCmd.Parameters.AddWithValue("@CustomersCount", guests.Count);
                    orderCmd.Parameters.AddWithValue("@GarconID", SelectedGarcon);
                    orderCmd.Parameters.AddWithValue("@TableID", SelectedTable);
                    int orderId = (int)orderCmd.ExecuteScalar();
                    Dictionary<int, int> guestToDbId = new Dictionary<int, int>();
                    //ОБРАБОТКА КАЖДОГО ГОСТЯ
                    foreach (var guest in guests)
                    {
                        SqlCommand guestCmd = new SqlCommand(
                            "INSERT INTO Order_Guests (Orders_ID, Guest_Number) OUTPUT INSERTED.Guest_ID VALUES (@OrderID, @GuestNum)",
                            connection, transaction);
                        guestCmd.Parameters.AddWithValue("@OrderID", orderId);
                        guestCmd.Parameters.AddWithValue("@GuestNum", guest.GuestNumber);
                        int guestId = (int)guestCmd.ExecuteScalar();
                        guestToDbId[guest.GuestNumber] = guestId;
                        foreach (var dish in guest.Dishes)
                        {
                            int menuId = GetMenuIDByName(dish.DishName, connection, transaction);

                            SqlCommand dishCmd = new SqlCommand(
                                "INSERT INTO Order_Dishes (Orders_ID, Menu_ID, Quantity, Guest_ID) OUTPUT INSERTED.Order_Dish_ID VALUES (@OrderID, @MenuID, @Quantity, @GuestID)",
                                connection, transaction);

                            dishCmd.Parameters.AddWithValue("@OrderID", orderId);
                            dishCmd.Parameters.AddWithValue("@MenuID", menuId);
                            dishCmd.Parameters.AddWithValue("@Quantity", dish.Count);
                            dishCmd.Parameters.AddWithValue("@GuestID", guestId);
                            int orderDishId = (int)dishCmd.ExecuteScalar();
                            foreach (var option in dish.SelectedOptions)
                            {
                                SqlCommand optionCmd = new SqlCommand(
                                    "INSERT INTO Order_Dish_Options (Order_ID, Menu_ID, Option_ID, Quantity, Option_Price) VALUES (@OrderID, @MenuID, @OptionID, @Quantity, @OptionPrice)",
                                    connection, transaction);

                                optionCmd.Parameters.AddWithValue("@OrderID", orderId);
                                optionCmd.Parameters.AddWithValue("@MenuID", menuId);
                                optionCmd.Parameters.AddWithValue("@OptionID", option.OptionId);
                                optionCmd.Parameters.AddWithValue("@Quantity", dish.Count); // количество опции = количеству блюда
                                optionCmd.Parameters.AddWithValue("@OptionPrice", option.OptionPrice);

                                optionCmd.ExecuteNonQuery();
                            }
                           
                        }
                    }
                    SqlCommand tableCommand = new SqlCommand(
                               $"UPDATE Tables SET Tables_Status='Занят' WHERE Tables_ID = '{SelectedTable}'",
                               connection, transaction);
                    tableCommand.ExecuteNonQuery();
                    transaction.Commit();
                    ShowNotification("Заказ успешно оформлен!");
                    MainMenuTabItem.IsSelected = true;
                    //ОЧИСТКА UI
                    ClearMenuSelector();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Ошибка оформления заказа: " + ex.Message);
                }
            }
        }



        private int GetMenuIDByName(string dishName, SqlConnection connection, SqlTransaction transaction)
        {
            SqlCommand cmd = new SqlCommand("SELECT Menu_ID FROM Menu WHERE Menu_Name = @Name", connection, transaction);
            cmd.Parameters.AddWithValue("@Name", dishName);
            object result = cmd.ExecuteScalar();
            return result != null ? Convert.ToInt32(result) : 0;
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
                            tableFiller.Image = bitmap; // Без обработки — напрямую из БД
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


        private void orderListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (menuListView.SelectedIndex == -1 || activeGuestIndex < 0)
                return;

            var item = (GarconMenu)menuListView.Items[menuListView.SelectedIndex];
            var guest = guests[activeGuestIndex];

            // Получаем полное имя блюда (без усечения)
            string fullDishName = item.Name;

            // Загружаем все опции для этого блюда
            List<DishOption> options = GetOptionsForMenu(fullDishName);

            // Ищем, есть ли уже такое блюдо у гостя
            var existingDish = guest.Dishes.FirstOrDefault(d => d.DishName == fullDishName);

            if (options == null || options.Count == 0)
            {
                // Если опций нет, просто добавляем блюдо
                if (existingDish != null)
                {
                    existingDish.Count++;
                    existingDish.DishPrice += item.Price - (item.Price / 100) * item.Discount;
                    existingDish.DishDiscount += (item.Price / 100) * item.Discount;
                }
                else
                {
                    guest.Dishes.Add(new GarconDish
                    {
                        DishDiscount = (item.Price / 100) * item.Discount,
                        DishPrice = item.Price - (item.Price / 100) * item.Discount,
                        DishName = fullDishName,
                        Count = 1,
                    });
                }

                optionsBorder.Visibility = Visibility.Collapsed;
                UpdateOrderListView();
                return;
            }

            // Если опции есть, создаем блюдо (если еще нет), и загружаем опции
            if (existingDish == null)
            {
                existingDish = new GarconDish
                {
                    DishDiscount = (item.Price / 100) * item.Discount,
                    DishPrice = item.Price - (item.Price / 100) * item.Discount,
                    DishName = fullDishName,
                    Count = 1
                };

                guest.Dishes.Add(existingDish);
            }

            // Загружаем опции для выбранного блюда
            LoadOptionsForDish(existingDish);
            optionsBorder.Visibility = Visibility.Visible;
        }



        private List<DishOption> GetOptionsForMenu(string dishName)
        {
            List<DishOption> options = new List<DishOption>();

            using (SqlConnection connection = new SqlConnection(ConfigurationManager.ConnectionStrings["constr"].ConnectionString))
            {
                connection.Open();

                // Получаем Menu_ID по названию блюда
                SqlCommand menuIdCmd = new SqlCommand("SELECT Menu_ID FROM Menu WHERE Menu_Name = @DishName", connection);
                menuIdCmd.Parameters.AddWithValue("@DishName", dishName);

                object result = menuIdCmd.ExecuteScalar();
                if (result == null)
                    return options; // блюдо не найдено

                int menuId = (int)result;

                // Теперь по Menu_ID получаем доступные опции
                SqlCommand optionsCmd = new SqlCommand(@"
            SELECT o.Option_ID, o.Option_Name, o.Option_Type, o.Is_Important
            FROM Menu_Dish_Options mdo
            INNER JOIN Dish_Options o ON o.Option_ID = mdo.Option_ID
            WHERE mdo.Menu_ID = @MenuID", connection);

                optionsCmd.Parameters.AddWithValue("@MenuID", menuId);

                using (SqlDataReader reader = optionsCmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        options.Add(new DishOption
                        {
                            OptionId = reader.GetInt32(0),
                            OptionName = reader.GetString(1),
                            OptionType = reader.GetString(2),
                            IsImportant = reader.GetBoolean(3)
                        });
                    }
                }
            }

            return options;
        }

        private void SaveSelectedOptionsForDish(GarconDish dish)
        {
            dish.SelectedOptions.Clear();

            foreach (var item in optionsListBox.Items)
            {
                if (item is CheckBox checkBox && checkBox.IsChecked == true)
                {
                    dish.SelectedOptions.Add(new DishSelectedOption
                    {
                        OptionId = (int)checkBox.Tag,
                        OptionPrice = GetOptionPriceById((int)checkBox.Tag)
                    });
                }
                else if (item is RadioButton radio && radio.IsChecked == true)
                {
                    dish.SelectedOptions.Add(new DishSelectedOption
                    {
                        OptionId = (int)radio.Tag,
                        OptionPrice = GetOptionPriceById((int)radio.Tag)
                    });
                }
            }

            dish.OptionsText = GenerateOptionsText(dish.SelectedOptions.Select(o => o.OptionId).ToList());
        }

        private double GetOptionPriceById(int optionId)
        {
            using (SqlConnection connection = new SqlConnection(ConfigurationManager.ConnectionStrings["constr"].ConnectionString))
            {
                connection.Open();
                using (SqlCommand cmd = new SqlCommand("SELECT Option_Price FROM Dish_Options WHERE Option_ID = @id", connection))
                {
                    cmd.Parameters.AddWithValue("@id", optionId);
                    var result = cmd.ExecuteScalar();
                    if (result != null)
                        return Convert.ToDouble(result);
                }
            }
            return 0;
        }

        private string GenerateOptionsText(List<int> optionIds)
        {
            if (optionIds == null || optionIds.Count == 0)
                return "";

            using (SqlConnection connection = new SqlConnection(ConfigurationManager.ConnectionStrings["constr"].ConnectionString))
            {
                connection.Open();
                List<string> optionNames = new List<string>();

                foreach (var id in optionIds)
                {
                    SqlCommand cmd = new SqlCommand("SELECT Option_Name FROM Dish_Options WHERE Option_ID = @id", connection);
                    cmd.Parameters.AddWithValue("@id", id);
                    var name = cmd.ExecuteScalar() as string;
                    if (!string.IsNullOrEmpty(name))
                        optionNames.Add("• " + name);
                }

                return string.Join("\n", optionNames);
            }
        }

        private void OptionsNextButton_Click(object sender, RoutedEventArgs e)
        {
            var items = (GarconMenu)menuListView.Items.GetItemAt(menuListView.SelectedIndex);
            var guest = guests[activeGuestIndex];
            var existingDish = guest.Dishes.FirstOrDefault(d => d.DishName == items.Name);

            if (existingDish != null)
            {
                SaveSelectedOptionsForDish(existingDish);
            }
            UpdateOrderListView();
            optionsBorder.Visibility = Visibility.Collapsed;
        }
        private void LoadOptionsForDish(GarconDish dish)
        {
            if (optionsListBox == null)
                return;

            optionsListBox.Items.Clear();

            // Загружаем все опции для блюда из БД
            List<DishOption> options = GetOptionsForMenu(dish.DishName);

            foreach (var option in options)
            {
                if (option.IsImportant)
                {
                    RadioButton radioButton = new RadioButton
                    {
                        Content = option.OptionName,
                        Tag = option.OptionId,
                        FontSize = 25,
                        Foreground = System.Windows.Media.Brushes.White,
                        FontFamily = new System.Windows.Media.FontFamily("Fonts/#Inter Regular"),
                        GroupName = "ImportantOptions_" + dish.DishName,
                        IsChecked = dish.SelectedOptions.Any(o => o.OptionId == option.OptionId)
                    };

                    optionsListBox.Items.Add(radioButton);
                }

                else
                {
                    CheckBox checkBox = new CheckBox
                    {
                        Content = option.OptionName,
                        Tag = option.OptionId,
                        FontSize = 25,
                        Foreground = System.Windows.Media.Brushes.White,
                        FontFamily = new System.Windows.Media.FontFamily("Fonts/#Inter Regular"),
                        IsChecked = dish.SelectedOptions.Any(o => o.OptionId == option.OptionId)
                    };

                    optionsListBox.Items.Add(checkBox);
                }
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
        }
        public void CheckAndUpdateTableStatus()
        {
            DateTime now = DateTime.Now;

            // 1. Закрываем просроченные бронирования
            using (SqlConnection connection = new SqlConnection(ConfigurationManager.ConnectionStrings["constr"].ConnectionString))
            {
                try
                {
                    connection.Open();
                    string updateReservations = $@"
                UPDATE Reservation 
                SET Reservation_Status = 'Закрыта' 
                WHERE Reservation_Date < '{now.Date:yyyy-MM-dd}' 
                OR (Reservation_Date = '{now.Date:yyyy-MM-dd}' AND Reservation_End <= CONVERT(TIME, GETDATE()))";

                    SqlCommand cmd = new SqlCommand(updateReservations, connection);
                    cmd.ExecuteNonQuery();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Ошибка при обновлении статусов бронирований: " + ex.Message);
                }
            }

            // 2. Обновляем статус каждого стола
            using (SqlConnection connection = new SqlConnection(ConfigurationManager.ConnectionStrings["constr"].ConnectionString))
            {
                try
                {
                    connection.Open();

                    for (int tableNumber = 1; tableNumber <= 16; tableNumber++)
                    {
                        // Проверка, установлен ли уже статус "Занят"
                        string tableStatusQuery = $"SELECT Tables_Status FROM Tables WHERE Tables_ID = {tableNumber}";
                        SqlCommand statusCmd = new SqlCommand(tableStatusQuery, connection);
                        string currentStatus = statusCmd.ExecuteScalar()?.ToString();

                        if (currentStatus == "Занят")
                        {
                            UpdateTableStatusBusy(tableNumber, true);
                        }
                        else
                        {
                            DateTime currentDateTime = DateTime.Now;

                            string activeReservationQuery = $@"
                        SELECT COUNT(*) 
                        FROM Reservation 
                        WHERE Tables_ID = {tableNumber} 
                          AND Reservation_Date = '{currentDateTime.Date:yyyy-MM-dd}' 
                          AND '{currentDateTime:HH:mm:ss}' BETWEEN Reservation_Start AND Reservation_End 
                          AND Reservation_Status = 'Активна'";

                            SqlCommand reservationCommand = new SqlCommand(activeReservationQuery, connection);
                            int reservationCount = (int)reservationCommand.ExecuteScalar();

                            UpdateTableStatus(tableNumber, reservationCount > 0);
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Ошибка при обновлении статусов столов: " + ex.Message);
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
                    garconTable1.Status = status;
                    break;
                case 2:
                    garconTable2.Status = status;
                    break;
                case 3:
                    garconTable3.Status = status;
                    break;
                case 4:
                    garconTable4.Status = status;
                    break;
                case 5:
                    garconTable5.Status = status;
                    break;
                case 6:
                    garconTable6.Status = status;
                    break;
                case 7:
                    garconTable7.Status = status;
                    break;
                case 8:
                    garconTable8.Status = status;
                    break;
                case 9:
                    garconTable9.Status = status;
                    break;
                case 10:
                    garconTable10.Status = status;
                    break;
                case 11:
                    garconTable11.Status = status;
                    break;
                case 12:
                    garconTable12.Status = status;
                    break;
                case 13:
                    garconTable13.Status = status;
                    break;
                case 14:
                    garconTable14.Status = status;
                    break;
                case 15:
                    garconTable15.Status = status;
                    break;
                case 16:
                    garconTable16.Status = status;
                    break;
                default:
                    break;
            }
        }
        private string GetGarconName(int garconID,bool IsSurnameToo)
        {
            string garconName = "Неизвестный"; 

            string query = "SELECT Garcon_Name FROM Garcon WHERE Garcon_ID = @GarconID";

            using (SqlConnection connection = new SqlConnection(ConfigurationManager.ConnectionStrings["constr"].ConnectionString))
            {
                connection.Open();
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@GarconID", garconID);
                    object result = command.ExecuteScalar();

                    if (result != null)
                    {
                        string[] words = result.ToString().Split(' ');
                        if (IsSurnameToo)
                        {
                            garconName =  $"{words[0]} {words[1]}";
                        }
                        else
                        {
                            garconName = words[0];
                        }
                    }

                }
            }
            return garconName;
        }
        private void NumPad_Click(object sender, RoutedEventArgs e)
        {
            var value = (string)((Button)sender).Content;
            PasswordBox.Password += value;

        }

        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            if (PasswordBox.Password.Length > 0)
                PasswordBox.Password = PasswordBox.Password = "";
        }

        private void Login_Click(object sender, RoutedEventArgs e)
        {
            byte[] hashedPassword = HashPassword(PasswordBox.Password);

            using (SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings["constr"].ConnectionString))
            {
                conn.Open();
                string query = "SELECT Garcon_ID FROM Garcon WHERE Garcon_Enter_Password = @Password";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@Password", hashedPassword);
                    int Garcon_ID;
                    if (cmd.ExecuteScalar()!= null)
                    {
                         Garcon_ID = (int)cmd.ExecuteScalar();
                    }
                    else
                    {
                         Garcon_ID = 0;
                    }
                    if (Garcon_ID > 0)
                    {
                        SelectedGarcon = Garcon_ID;
                        foreach (var child in MenuGridRoot.Children.OfType<Grid>())
                        {
                            child.Visibility = Visibility.Collapsed;
                        }
                        LoadMonthlyGarconSchedule(SelectedGarcon, now);
                        MainGrid.Visibility = Visibility.Visible;
                        MainMenuTabItem.IsSelected = true;
                        GarconNameText.Text = GetGarconName(SelectedGarcon, true);
                        UpdateTextBlocks(null, null);
                        PasswordBox.Password = "";
                    }
                    else
                    {
                        ShowNotification("Неверный код пароль!");
                    }
                }
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
        private byte[] HashPassword(string password)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                return sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            }
        }
        private void LoadTableOrder()
        {
            var tableOrder = GetOrCreateTableOrder(SelectedTable);
            guests = tableOrder.Guests;
            if (guests.Count == 0)
            {
                guests.Add(new Guest { GuestNumber = 1 });
            }
            activeGuestIndex = 0;
            UpdateGuestListUI();
            UpdateOrderListView();
        }
        public void ChangeTableState(string Number, string Status)
        {
            GarconEnterTab.Visibility = Visibility.Collapsed;
            DishesSelectorTab.Visibility = Visibility.Visible;
            DishesSelectorTab.IsSelected = true;

            SelectedTable = Convert.ToInt32(Number);
            // Получаем фамилию официанта из БД
            string garconName = GetGarconName(SelectedGarcon,false);

            // Обновляем текстовые блоки
            TableText.Text = $"Стол №{Number}";
            GarconText.Text = garconName;
            LoadTableOrder();
        }
       
        private void orderListView_SelectedItemChanged(object sender, SelectionChangedEventArgs e)
        {
            foreach (var item in e.RemovedItems)
            {
                ToggleButtonsVisibility(item, Visibility.Collapsed);
            }
            if (orderListView.SelectedItem != null)
            {
                ToggleButtonsVisibility(orderListView.SelectedItem, Visibility.Visible);
            }
        }

        private void ToggleButtonsVisibility(object selectedItem, Visibility visibility)
        {
            var container = orderListView.ItemContainerGenerator.ContainerFromItem(selectedItem) as ListViewItem;
            if (container != null)
            {
                Button deleteButton = FindVisualChild<Button>(container, "deleteButton");
                Button setQuantityButton = FindVisualChild<Button>(container, "setCountButton");
                TextBlock dishDiscountText = FindVisualChild<TextBlock>(container, "dishDiscountText");
                if (deleteButton != null)
                {
                    deleteButton.Visibility = visibility;
                    if (dishDiscountText != null && dishDiscountText.Visibility == Visibility.Visible)
                    {
                        deleteButton.Margin = new Thickness(0, 30, 0, 0);
                        setQuantityButton.Margin = new Thickness(15, 30, 0, 0);
                    }
                    else
                    {
                        deleteButton.Margin = new Thickness(0, 10, 0, 0);
                        setQuantityButton.Margin = new Thickness(15, 10, 0, 0);
                    }
                }
                if (setQuantityButton != null)
                {
                    setQuantityButton.Visibility = visibility;
                }
            }
        }
        private static T FindVisualChild<T>(DependencyObject parent, string childName) where T : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(parent, i);
                T childType = child as T;
                if (childType != null && !string.IsNullOrEmpty(childName))
                {
                    var frameworkElement = child as FrameworkElement;
                    if (frameworkElement != null && frameworkElement.Name == childName)
                    {
                        return childType;
                    }
                }

                T childOfChild = FindVisualChild<T>(child, childName);
                if (childOfChild != null)
                {
                    return childOfChild;
                }
            }
            return null;
        }
        private static T FindAncestor<T>(DependencyObject current) where T : DependencyObject
        {
            while (current != null)
            {
                if (current is T)
                {
                    return (T)current;
                }
                current = VisualTreeHelper.GetParent(current);
            }
            return null;
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

        private Button currentlySelectedButton = null;

        private void CategoryButton_Click(object sender, RoutedEventArgs e)
        {
            var clickedButton = sender as Button;

            if (currentlySelectedButton != null)
            {
                currentlySelectedButton.ClearValue(Button.BackgroundProperty);
                currentlySelectedButton.ClearValue(Button.ForegroundProperty);
            }

            // Устанавливаем подсветку на новую кнопку
            clickedButton.Background = new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#B2232323"));
            clickedButton.Foreground = System.Windows.Media.Brushes.White;

            // Запоминаем выбранную кнопку
            currentlySelectedButton = clickedButton;

            // Фильтруем блюда по категории
            var selectedCategory = clickedButton.Tag?.ToString();
            if (selectedCategory == "Всё")
            {
                getMenu("");
            }
            else
            {
                getMenu($"WHERE Menu_Type = '{selectedCategory}'");
            }
        }



        private void deleteButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var container = FindAncestor<ListViewItem>(button);
            if (container != null)
            {
                var item = (GarconDish)container.DataContext;

                var existingDish = guests[activeGuestIndex].Dishes.FirstOrDefault(dish => dish.DishName == item.DishName);
                if (existingDish != null)
                {
                    guests[activeGuestIndex].Dishes.Remove(existingDish);
                }
                orderListView.Items.Refresh();
                totalPriceButton.Text = $"{guests.Sum(g => g.TotalPrice)} ₽";
            }
        }

        private void setCountButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var container = FindAncestor<ListViewItem>(button);
            if (container != null)
            {
                var item = (GarconDish)container.DataContext;

                var existingDish = guests[activeGuestIndex].Dishes.FirstOrDefault(dish => dish.DishName == item.DishName);
                if (existingDish != null)
                {
                    nowChangedCountDishName = existingDish.DishName;
                    orderListView.SelectedItem = null;
                    OrderGrid.Visibility = Visibility.Collapsed;
                    DishCountGrid.Visibility = Visibility.Visible;
                }
            }
        }

        private void NumberButton_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            if (button != null)
            {
                countTextBox.Text += button.Content.ToString();
            }
        }

        private void dishCountNextButton_Click(object sender, RoutedEventArgs e)
        {
            var existingDish = guests[activeGuestIndex].Dishes.FirstOrDefault(dish => dish.DishName == nowChangedCountDishName);
            if (existingDish != null)
            {
                if (countTextBox.Text == "") { countTextBox.Text = "1"; }
                if (Convert.ToInt32(countTextBox.Text) <= 0) { countTextBox.Text = "1"; }
                if (Convert.ToInt32(countTextBox.Text) >= existingDish.Count)
                {
                    existingDish.Count = Convert.ToInt32(countTextBox.Text);
                    existingDish.DishPrice = existingDish.DishPrice * Convert.ToInt32(countTextBox.Text);
                    existingDish.DishDiscount = existingDish.DishDiscount * Convert.ToInt32(countTextBox.Text);
                    totalPriceButton.Text = $"{guests.Sum(g => g.TotalPrice)} ₽";
                    countTextBox.Text = "";
                }
                else
                {
                    int tmp = existingDish.Count - Convert.ToInt32(countTextBox.Text);
                    int tmp2 = existingDish.DishPrice / existingDish.Count;
                    int tmpDiscount = existingDish.DishDiscount / existingDish.Count;
                    existingDish.Count = Convert.ToInt32(countTextBox.Text);
                    existingDish.DishPrice = existingDish.DishPrice - tmp2 * tmp;
                    existingDish.DishDiscount = existingDish.DishDiscount - tmpDiscount * tmp;
                    totalPriceButton.Text = $"{guests.Sum(g => g.TotalPrice)} ₽";
                    countTextBox.Text = "";
                }

            }
            orderListView.Items.Refresh();
            DishCountGrid.Visibility = Visibility.Collapsed;
            OrderGrid.Visibility = Visibility.Visible;
        }
        private void clearDishCount_Click(object sender, RoutedEventArgs e)
        {
            countTextBox.Text = "";
        }
        private void InputTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            // Проверяем, что вводимая строка состоит только из цифр
            e.Handled = !IsTextAllowed(e.Text);
        }

        private void InputTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            // Проверяем, что введенное значение не больше 100
            if (int.TryParse(countTextBox.Text, out int result))
            {
                if (result > 100)
                {
                    countTextBox.Text = "100"; // Ограничиваем до 100
                    countTextBox.SelectionStart = countTextBox.Text.Length; // Устанавливаем курсор в конец
                }
            }
        }

        private void InputTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            // Запрещаем вставку текста с использованием Ctrl + V
            if (e.Key == Key.V && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                e.Handled = true; // Отмена события
            }

            // Разрешаем Backspace для удаления
            if (e.Key == Key.Back)
            {
                return;
            }
        }

        private void InputTextBox_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Запрет вставки через правую кнопку мыши
            e.Handled = true; // Отмена события
        }

        private void InputTextBox_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            // Запрет отображения контекстного меню
            e.Handled = true; // Отмена события
        }

        private bool IsTextAllowed(string text)
        {
            // Разрешаем только цифры
            Regex regex = new Regex("[^0-9]"); // Регулярное выражение для нецифровых символов
            return !regex.IsMatch(text);
        }

        private void BackToMenuButton_Click(object sender, RoutedEventArgs e)
        {
       
            foreach (var child in MenuGridRoot.Children.OfType<Grid>())
            {
                child.Visibility = Visibility.Collapsed;
            }
            ActiveOrdersTopMenu.Visibility = Visibility.Collapsed;
            ClockTopMenu.Visibility = Visibility.Visible;
            MainGrid.Visibility = Visibility.Visible;
    }

        private void CreateOrderButton_Click(object sender, RoutedEventArgs e)
        {
            GarconEnterTab.IsSelected = true;
        }

        private void ActiveOrdersButton_Click(object sender, RoutedEventArgs e)
        {
            foreach (var child in MenuGridRoot.Children.OfType<Grid>())
            {
                child.Visibility = Visibility.Collapsed;
            }
            ShowOthersToggle.IsChecked = false;
            OrdersInfoStatusText.Text = "Активные заказы";
            LoadCurrentOrders($"WHERE o.Orders_Status != 'Завершен' AND o.Orders_Status != 'Отменён' AND o.Garcon_ID = {SelectedGarcon}");
            LastOrdersGrid.Visibility = Visibility.Visible;
            ActiveOrdersTopMenu.Visibility = Visibility.Visible;
            ClockTopMenu.Visibility = Visibility.Collapsed;
        }

        private void ShowOthersToggle_Checked(object sender, RoutedEventArgs e)
        {
            if (OrdersInfoStatusText.Text == "Активные заказы") { LoadCurrentOrders($"WHERE o.Orders_Status != 'Завершен' "); }
            else { LoadCurrentOrders($"WHERE o.Orders_Status = 'Завершен' AND o.Orders_Status != 'Отменён'"); }
            
        }

        private void ShowOthersToggle_Unchecked(object sender, RoutedEventArgs e)
        {
            if (OrdersInfoStatusText.Text == "Активные заказы") { LoadCurrentOrders($"WHERE o.Orders_Status != 'Завершен' AND o.Orders_Status != 'Отменён' AND o.Garcon_ID = {SelectedGarcon}"); }
            else { LoadCurrentOrders($"WHERE o.Orders_Status = 'Завершен' AND o.Orders_Status != 'Отменён' AND o.Garcon_ID = {SelectedGarcon}"); }
        }

        private void DeActiveOrdersButton_Click(object sender, RoutedEventArgs e)
        {
            foreach (var child in MenuGridRoot.Children.OfType<Grid>())
            {
                child.Visibility = Visibility.Collapsed;
            }
            ShowOthersToggle.IsChecked = false;
            LoadCurrentOrders($"WHERE o.Orders_Status = 'Завершен' OR o.Orders_Status = 'Отменён' AND o.Garcon_ID = {SelectedGarcon}");
            OrdersInfoStatusText.Text = "Завершённые заказы";
            LastOrdersGrid.Visibility = Visibility.Visible;
        }
        private void ClearMenuSelector ()
        {
            guests.Clear();
            guests.Add(new Guest { GuestNumber = 1 });
            activeGuestIndex = 0;
            UpdateGuestListUI();
            selectedDishes = guests[0].Dishes;
            orderListView.ItemsSource = null;
            totalPriceButton.Text = "0 ₽";
        }
        private void BackToMainMenu_Click_1(object sender, RoutedEventArgs e)
        {
            ClearMenuSelector();
            DishesSelectorTab.IsSelected = false;
            MainMenuTabItem.IsSelected = true;
        }

        private DataRowView selectedOrder;
        private void currentOrdersDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (currentOrdersDataGrid.SelectedItem is DataRowView row)
            {
                selectedOrder = row;
            }
        }

        private void CancelOrderButton_Click(object sender, RoutedEventArgs e)
        {
            dynamic selectedOrder = currentOrdersDataGrid.SelectedItem;
            if (selectedOrder == null)
            {
                ShowNotification("Выберите заказ.");
                return;
            }
            else
            {
                SqlCommand cancel = new SqlCommand(
                              $"UPDATE ORDERS SET Orders_Status = 'Отменён' WHERE Orders_ID = {selectedOrder.OrderID} ",
                              connection);

                connection.Open();
                cancel.ExecuteNonQuery();
                connection.Close();
                ShowNotification("Заказ успешно отменён.");
                LoadCurrentOrders($"WHERE o.Orders_Status != 'Завершен' AND o.Orders_Status != 'Отменён' AND o.Garcon_ID = {SelectedGarcon}");
            }
           

        }
        private List<GarconDish> GetDishesForOrder(int orderId)
        {
            List<GarconDish> dishes = new List<GarconDish>();

            using (SqlConnection connection = new SqlConnection(ConfigurationManager.ConnectionStrings["constr"].ConnectionString))
            {
                connection.Open();

                SqlCommand cmd = new SqlCommand(@"
            SELECT 
                od.Menu_ID,
                od.Quantity,
                m.Menu_Name,
                m.Menu_Price,
                ISNULL(m.Menu_Discount, 0) AS Menu_Discount,
                do.Option_ID,
                do.Option_Name,
                ISNULL(do.Option_Price, 0) AS Option_Price,
                og.Guest_Number
            FROM Order_Dishes od
            JOIN Menu m ON od.Menu_ID = m.Menu_ID
            JOIN Order_Guests og ON od.Guest_ID = og.Guest_ID
            LEFT JOIN Order_Dish_Options odo ON odo.Order_ID = od.Orders_ID AND odo.Menu_ID = od.Menu_ID
            LEFT JOIN Dish_Options do ON odo.Option_ID = do.Option_ID
            WHERE od.Orders_ID = @OrderID
            ORDER BY og.Guest_Number, od.Menu_ID, do.Option_Name
        ", connection);

                cmd.Parameters.AddWithValue("@OrderID", orderId);

                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        int guestNumber = (int)reader["Guest_Number"];
                        int menuId = (int)reader["Menu_ID"];
                        string dishName = reader["Menu_Name"].ToString();

                        // Ищем блюдо с таким гостем и именем
                        var dish = dishes.FirstOrDefault(d => d.GuestNumber == guestNumber && d.DishName == dishName);

                        if (dish == null)
                        {
                            dish = new GarconDish
                            {
                                DishName = dishName,
                                DishPrice = Convert.ToInt32(reader["Menu_Price"]),
                                DishDiscount = Convert.ToInt32(reader["Menu_Discount"]),
                                Count = Convert.ToInt32(reader["Quantity"]),
                                SelectedOptions = new List<DishSelectedOption>(),
                                GuestNumber = guestNumber
                            };
                            dishes.Add(dish);
                        }
                        else
                        {
                            // Если блюдо уже есть для этого гостя, увеличиваем количество
                            dish.Count += Convert.ToInt32(reader["Quantity"]);
                        }

                        if (reader["Option_ID"] != DBNull.Value)
                        {
                            dish.SelectedOptions.Add(new DishSelectedOption
                            {
                                OptionId = Convert.ToInt32(reader["Option_ID"]),
                                OptionPrice = Convert.ToDouble(reader["Option_Price"])
                            });
                        }
                    }
                }
            }

            return dishes;
        }


        private void GenerateCheck(bool isRealCheck)
        {
            dynamic selectedOrder = currentOrdersDataGrid.SelectedItem;
            if (selectedOrder == null)
            {
                ShowNotification("Выберите заказ.");
                return;
            }
            else
            {
                int orderId = (int)selectedOrder.OrderID;

                List<GarconDish> dishList = GetDishesForOrder(orderId);

                var guests = dishList
         .GroupBy(d => d.GuestNumber)
         .Select(g => new Guest
         {
             GuestNumber = g.Key,
             Dishes = g.ToList()
         })
         .OrderBy(g => g.GuestNumber)
         .ToList();

                if (isRealCheck == false)
                {
                    connection.Open();
                    SqlCommand orderCmd = new SqlCommand(
                        "UPDATE Orders SET Orders_Status = 'Завершен' WHERE Orders_ID = @ID;", connection);
                    orderCmd.Parameters.AddWithValue("@ID", orderId);
                    orderCmd.ExecuteScalar();
                    connection.Close();
                    var sb = new StringBuilder();
                    sb.AppendLine("           ООО \"Ресторан Клодм\"");
                    sb.AppendLine();

                    sb.AppendLine("                  ПРЕДЧЕК");
                    sb.AppendLine("               (нефискальный)");
                    sb.AppendLine();

                    sb.AppendLine($"Дата: {selectedOrder.OrderDate:dd.MM.yyyy}");
                    sb.AppendLine($"Время открытия: {selectedOrder.OrderDate:dd.MM.yyyy} {selectedOrder.OrderTime}");
                    sb.AppendLine($"Время закрытия: {DateTime.Now.ToString("dd.MM.yyyy HH:mm")}");
                    sb.AppendLine($"Стол №: {selectedOrder.TableNumber}");
                    sb.AppendLine($"Официант: {selectedOrder.Garcon}");
                    sb.AppendLine($"Кол-во гостей: {selectedOrder.GuestsCount}");
                    sb.AppendLine();

                    decimal total = 0m;

                    var groupedByGuest = dishList.GroupBy(d => d.GuestNumber).OrderBy(g => g.Key);

                    foreach (var guestGroup in groupedByGuest)
                    {
                        sb.AppendLine($"Гость {guestGroup.Key}");
                        sb.AppendLine("------------------------------------------");
                        sb.AppendLine("Наименование         Кол-во          Сумма");
                        sb.AppendLine("------------------------------------------");

                        decimal guestTotal = 0;

                        foreach (var dish in guestGroup)
                        {
                            decimal basePrice = dish.DishPrice;
                            decimal discount = dish.DishDiscount;
                            int count = dish.Count;

                            decimal finalPrice = (basePrice - discount) * count;
                            guestTotal += finalPrice;

                            string name = dish.DishName.Length > 18
                                ? dish.DishName.Substring(0, 18) + "…"
                                : dish.DishName;

                            string qty = count.ToString().PadLeft(5);
                            string sum = $"{finalPrice:0.00} ₽".PadLeft(17);

                            sb.AppendLine($"{name.PadRight(20)}{qty}{sum}");

                            if (dish.SelectedOptions?.Any() == true)
                            {
                                foreach (var opt in dish.SelectedOptions)
                                {
                                    decimal optPrice = (decimal)opt.OptionPrice * count;
                                    guestTotal += optPrice;

                                    string optText = $"+ опция {opt.OptionId}".PadRight(25);
                                    string optSum = $"{optPrice:0.00} ₽".PadLeft(15);
                                    sb.AppendLine($"{optText}{optSum}");
                                }
                            }
                        }

                        sb.AppendLine("------------------------------------------");
                        sb.AppendLine($"Итого по гостю {guestGroup.Key}:".PadRight(30) + $"{guestTotal,10:0.00} ₽");
                        sb.AppendLine();

                        total += guestTotal; // аккумулируем общий итог
                    }

                    // После всех гостей выводим общий итог
                    decimal vat = total * 0.20m;
                    sb.AppendLine();
                    sb.AppendLine("------------------------------------------");
                    sb.AppendLine($"{"ИТОГО:".PadRight(30)}{total,10:0.00} ₽");
                    sb.AppendLine($"{"(в т.ч. НДС 20%):".PadRight(30)}{vat,10:0.00} ₽");
                    sb.AppendLine(" ");
                    sb.AppendLine("         Спасибо за посещение нашего");
                    sb.AppendLine("                 ресторана!");
                    sb.AppendLine(" ");
                    PrintText(sb.ToString());
                }
                else
                {
                    var sb = new StringBuilder();
                    sb.AppendLine("         ООО \"Ресторан Клодм\"");
                    sb.AppendLine("ИНН: 7701234567 ОГРН: 1027701196001"); // Пример ИНН и ОГРН
                    sb.AppendLine("Адрес: г. Москва, ул. Баумана, д. 1");
                    sb.AppendLine("Сайт ОФД: www.ofd.ru");
                    sb.AppendLine();

                    sb.AppendLine("            КАССОВЫЙ ЧЕК");
                    sb.AppendLine("           (ПРИХОД)");
                    sb.AppendLine();

                    sb.AppendLine($"Дата: {selectedOrder.OrderDate:dd.MM.yyyy}");
                    sb.AppendLine($"Время открытия: {selectedOrder.OrderDate:dd.MM.yyyy} {selectedOrder.OrderTime}");
                    sb.AppendLine($"Время закрытия: {DateTime.Now.ToString("dd.MM.yyyy HH:mm")}");
                    sb.AppendLine($"Стол №: {selectedOrder.TableNumber}");
                    sb.AppendLine($"Официант: {selectedOrder.Garcon}");
                    sb.AppendLine($"Кол-во гостей: {selectedOrder.GuestsCount}");
                    sb.AppendLine();

                    decimal total = 0m;

                    var groupedByGuest = dishList.GroupBy(d => d.GuestNumber).OrderBy(g => g.Key);

                    foreach (var guestGroup in groupedByGuest)
                    {
                        sb.AppendLine($"Гость {guestGroup.Key}");
                        sb.AppendLine("------------------------------------------");
                        sb.AppendLine("Наименование         Кол-во           Сумма");
                        sb.AppendLine("------------------------------------------");

                        decimal guestTotal = 0;

                        foreach (var dish in guestGroup)
                        {
                            decimal basePrice = dish.DishPrice;
                            decimal discount = dish.DishDiscount;
                            int count = dish.Count;

                            decimal finalPrice = (basePrice - discount) * count;
                            guestTotal += finalPrice;

                            string name = dish.DishName.Length > 18
                                ? dish.DishName.Substring(0, 18) + "…"
                                : dish.DishName;

                            string qty = count.ToString().PadLeft(5);
                            string sum = $"{finalPrice:0.00} ₽".PadLeft(17);

                            sb.AppendLine($"{name.PadRight(20)}{qty}{sum}");

                            if (dish.SelectedOptions?.Any() == true)
                            {
                                foreach (var opt in dish.SelectedOptions)
                                {
                                    decimal optPrice = (decimal)opt.OptionPrice * count;
                                    guestTotal += optPrice;

                                    string optText = $"+ опция {opt.OptionId}".PadRight(25);
                                    string optSum = $"{optPrice:0.00} ₽".PadLeft(15);
                                    sb.AppendLine($"{optText}{optSum}");
                                }
                            }
                        }

                        sb.AppendLine("------------------------------------------");
                        sb.AppendLine($"Итого по гостю {guestGroup.Key}:".PadRight(30) + $"{guestTotal,10:0.00} ₽");
                        sb.AppendLine();

                        total += guestTotal; // аккумулируем общий итог
                    }

                    // После всех гостей выводим общий итог
                    decimal vat = total * 0.20m;
                    sb.AppendLine();
                    sb.AppendLine("------------------------------------------");
                    sb.AppendLine($"{"ИТОГО:".PadRight(30)}{total,10:0.00} ₽");
                    sb.AppendLine($"{"(в т.ч. НДС 20%):".PadRight(30)}{vat,10:0.00} ₽");
                    sb.AppendLine("------------------------------------------");
                    sb.AppendLine(" ");

                    // Добавление фискальных данных (заглушки)
                    sb.AppendLine("№ Чека: 12345"); // Номер чека за смену
                    sb.AppendLine("Смена: 5"); // Номер смены
                    sb.AppendLine("РН ККТ: 0000000000000000"); // Регистрационный номер ККТ
                    sb.AppendLine("ЗН ФН: 1111111111111111"); // Заводской номер ФН
                    sb.AppendLine("ФПД: 1234567890"); // Фискальный признак документа
                    sb.AppendLine("Система налогообложения: ОСНО"); // Пример СНО
                    sb.AppendLine(" ");

                    // Место для QR-кода (в реальной реализации будет генерация изображения)
                    sb.AppendLine("        * QR-код для проверки *"); // Имитация QR-кода
                    sb.AppendLine(" https://kkt-online.nalog.ru/12345"); // Пример ссылки на сайт ОФД

                    sb.AppendLine(" ");
                    sb.AppendLine("         Спасибо за посещение нашего");
                    sb.AppendLine("               ресторана!");
                    sb.AppendLine(" ");

                    try
                    {
                        connection.Open();

                        SqlCommand orderCmd = new SqlCommand(
                            "UPDATE Orders SET Orders_Status = 'Завершен' WHERE Orders_ID = @ID;", connection);
                        orderCmd.Parameters.AddWithValue("@ID", orderId);
                        orderCmd.ExecuteScalar();

                        //Вставляем данные о платеже в таблицу Payments
                        SqlCommand paymentCmd = new SqlCommand(
                            "INSERT INTO Payments (Orders_ID, Payment_Date, Payment_Amount, Payment_Method, Receipt_File) " +
                            "VALUES (@Orders_ID, @Payment_Date, @Payment_Amount, @Payment_Method, @Receipt_File);", connection);

                        paymentCmd.Parameters.AddWithValue("@Orders_ID", orderId);
                        paymentCmd.Parameters.AddWithValue("@Payment_Date", DateTime.Now);
                        paymentCmd.Parameters.AddWithValue("@Payment_Amount", total); // Используем рассчитанную сумму
                        paymentCmd.Parameters.AddWithValue("@Payment_Method", PaymentMethodGlobal);

                        // Сохраняем текст чека в виде массива байтов (VARBINARY(MAX))
                        string checkText = sb.ToString();
                        byte[] receiptBytes = Encoding.UTF8.GetBytes(checkText);
                        paymentCmd.Parameters.AddWithValue("@Receipt_File", receiptBytes);

                        paymentCmd.ExecuteNonQuery();

                        ShowNotification("Заказ завершен и чек сохранен.");
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка при сохранении чека: {ex.Message}");
                    }
                    finally
                    {
                        if (connection.State == System.Data.ConnectionState.Open)
                        {
                            connection.Close();
                        }
                    }
                    PrintText(sb.ToString());
                }
            }
            
        }
        private void PrecheckButton_Click(object sender, RoutedEventArgs e)
        {
            GenerateCheck(false);
        }

        private void PrintText(string text)
        {
            FlowDocument doc = new FlowDocument
            {
                FontSize = 14,
                PageWidth = 420,
                FontFamily = new FontFamily("Courier New") // Моноширинный шрифт для выравнивания
            };

            Paragraph p = new Paragraph(new Run(text))
            {
                Margin = new Thickness(10)
            };
            doc.Blocks.Add(p);

            PrintDialog pd = new PrintDialog();
            if (pd.ShowDialog() == true)
            {
                pd.PrintDocument(((IDocumentPaginatorSource)doc).DocumentPaginator, "Предчек");
            }
        }

        private void RealCheckButton_Click(object sender, RoutedEventArgs e)
        {
           
            dynamic selectedOrder = currentOrdersDataGrid.SelectedItem;
            if (selectedOrder == null)
            {
                ShowNotification("Выберите заказ.");
                return;
            }
            else
            {
                DishesSelectorTab.IsSelected = true;
                DishesSelectorTab.Visibility = Visibility.Visible;
                PaymentTab.Visibility = Visibility.Visible;
                OrderGrid.Visibility = Visibility.Collapsed;
                int orderId = (int)selectedOrder.OrderID;
                LoadGuestsWithOrderInfo(orderId);
               
            }
        }

        private void StartShift_Click(object sender, RoutedEventArgs e)
        {
            using (SqlConnection con = new SqlConnection(ConfigurationManager.ConnectionStrings["constr"].ConnectionString))
            {
                con.Open();

                // Проверяем, есть ли уже открытая смена
                string checkOpenShift = @"
            SELECT COUNT(*) FROM Garcon_Schedule
            WHERE Garcon_ID = @GarconID AND Work_Date = CAST(GETDATE() AS date) 
              AND Shift_Real_Start IS NOT NULL";

                var cmdCheck = new SqlCommand(checkOpenShift, con);
                cmdCheck.Parameters.AddWithValue("@GarconID", SelectedGarcon);
                int openShiftCount = (int)cmdCheck.ExecuteScalar();

                if (openShiftCount > 0)
                {
                    ShowNotification("Смена уже открыта!");
                    return;
                }

                // Проверяем, есть ли запись с графиком на сегодня
                string checkSchedule = @"
            SELECT COUNT(*) FROM Garcon_Schedule
            WHERE Garcon_ID = @GarconID AND Work_Date = CAST(GETDATE() AS date)";

                var cmdSchedule = new SqlCommand(checkSchedule, con);
                cmdSchedule.Parameters.AddWithValue("@GarconID", SelectedGarcon);
                int scheduleCount = (int)cmdSchedule.ExecuteScalar();

                if (scheduleCount > 0)
                {
                    // Обновляем Shift_Real_Start
                    string updateRealStart = @"
                UPDATE Garcon_Schedule 
                SET Shift_Real_Start = CAST(GETDATE() AS time)
                WHERE Garcon_ID = @GarconID AND Work_Date = CAST(GETDATE() AS date)";

                    var cmdUpdate = new SqlCommand(updateRealStart, con);
                    cmdUpdate.Parameters.AddWithValue("@GarconID", SelectedGarcon);
                    cmdUpdate.ExecuteNonQuery();
                }
                else
                {
                    // Вставляем новую запись
                    string insertSchedule = @"
                INSERT INTO Garcon_Schedule (Garcon_ID, Work_Date, Shift_Real_Start)
                VALUES (@GarconID, CAST(GETDATE() AS date), CAST(GETDATE() AS time))";

                    var cmdInsert = new SqlCommand(insertSchedule, con);
                    cmdInsert.Parameters.AddWithValue("@GarconID", SelectedGarcon);
                    cmdInsert.ExecuteNonQuery();
                }

                ShowNotification("Смена начата!");
            }
        }


        private void EndShift_Click(object sender, RoutedEventArgs e)
        {
            using (SqlConnection con = new SqlConnection(ConfigurationManager.ConnectionStrings["constr"].ConnectionString))
            {
                con.Open();

                string updateEndShift = @"
            UPDATE Garcon_Schedule
            SET Shift_Real_End = CAST(GETDATE() AS time)
            WHERE Garcon_ID = @GarconID AND Work_Date = CAST(GETDATE() AS date) 
              AND Shift_Real_Start IS NOT NULL AND Shift_Real_End IS NULL";

                var cmd = new SqlCommand(updateEndShift, con);
                cmd.Parameters.AddWithValue("@GarconID", SelectedGarcon);

                int rowsAffected = cmd.ExecuteNonQuery();

                if (rowsAffected == 0)
                {
                    ShowNotification("Нет открытой смены для закрытия");
                }
                else
                {
                    ShowNotification("Смена закрыта");
                }
            }
        }
        private async void ShowNotification(string message, int durationMs = 3000)
        {
            NotificationText.Text = message;
            NotificationPanel.Visibility = Visibility.Visible;

            var showAnim = new DoubleAnimation
            {
                From = 100,
                To = 0,
                Duration = TimeSpan.FromMilliseconds(300),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };
            NotificationTranslate.BeginAnimation(TranslateTransform.XProperty, showAnim);

            var fadeIn = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(300));
            NotificationPanel.BeginAnimation(OpacityProperty, fadeIn);

            await Task.Delay(durationMs);

            var hideAnim = new DoubleAnimation
            {
                From = 0,
                To = 100,
                Duration = TimeSpan.FromMilliseconds(300),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
            };
            NotificationTranslate.BeginAnimation(TranslateTransform.XProperty, hideAnim);

            var fadeOut = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(300));
            NotificationPanel.BeginAnimation(OpacityProperty, fadeOut);

            await Task.Delay(300);

            NotificationPanel.Visibility = Visibility.Collapsed;
        }
        public OrderInfo GetOrderInfo(int orderId)
        {
            var info = new OrderInfo();

            using (var conn = new SqlConnection(ConfigurationManager.ConnectionStrings["constr"].ConnectionString))
            {
                conn.Open();
                var cmd = new SqlCommand(@"
            SELECT Orders_ID, Table_ID, 
                   CAST(Orders_Date AS datetime) + CAST(Orders_Time AS datetime) AS OrderDateTime,Orders_Bill
            FROM Orders
            WHERE Orders_ID = @OrderId", conn);
                cmd.Parameters.AddWithValue("@OrderId", orderId);

                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        info.OrderId = reader.GetInt32(0);
                        info.TableId = reader.IsDBNull(1) ? 0 : reader.GetInt32(1);
                        info.OrderDateTime = reader.GetDateTime(2);
                        info.TotalPrice = Convert.ToInt32(reader.GetDecimal(3));
                    }
                }
            }

            return info;
        }
        private void LoadGuestsWithOrderInfo(int orderId)
        {
            var guests = new List<Guest>();
            DateTime orderDateTime = DateTime.MinValue;
            int tableId = 0;

            using (var conn = new SqlConnection(ConfigurationManager.ConnectionStrings["constr"].ConnectionString))
            {
                conn.Open();

                // Получаем основную информацию о заказе
                using (var cmd = new SqlCommand(@"
            SELECT Orders_Date, Table_ID 
            FROM Orders 
            WHERE Orders_ID = @orderId", conn))
                {
                    cmd.Parameters.AddWithValue("@orderId", orderId);
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            orderDateTime = reader.GetDateTime(0);
                            tableId = reader.GetInt32(1);
                        }
                    }
                }

                // Получаем гостей, их блюда и опции
                using (var cmd = new SqlCommand(@"
            SELECT 
                g.Guest_Number,
                d.Menu_ID,
                m.Menu_Name,
                d.Quantity,
                m.Menu_Price,
                o.Option_Name,
                o.Option_Price
            FROM Order_Guests g
            JOIN Order_Dishes d ON d.Guest_ID = g.Guest_ID
            JOIN Menu m ON m.Menu_ID = d.Menu_ID
            LEFT JOIN Order_Dish_Options doo 
                ON doo.Order_ID = d.Orders_ID AND doo.Menu_ID = d.Menu_ID
            LEFT JOIN Dish_Options o ON o.Option_ID = doo.Option_ID
            WHERE g.Orders_ID = @orderId
            ORDER BY g.Guest_Number, d.Menu_ID", conn))
                {
                    cmd.Parameters.AddWithValue("@orderId", orderId);
                    using (var reader = cmd.ExecuteReader())
                    {
                        var guestsByNumber = new Dictionary<int, Guest>();

                        while (reader.Read())
                        {
                            // --- читаем поля из запроса ---
                            int guestNumber = reader.GetInt32(0);
                            int menuId = reader.GetInt32(1);          // можно сохранить, если нужно
                            string dishName = reader.GetString(2);
                            int quantity = reader.GetInt32(3);
                            int basePrice = reader.GetInt32(4);
                            string optionName = reader.IsDBNull(5) ? null : reader.GetString(5);
                            decimal optionPrice = reader.IsDBNull(6) ? 0m : reader.GetDecimal(6);

                            // --- гость ---
                            if (!guestsByNumber.TryGetValue(guestNumber, out var guest))
                            {
                                guest = new Guest { GuestNumber = guestNumber };
                                guestsByNumber[guestNumber] = guest;
                            }

                            // --- блюдо (ищем по имени + количеству; можно добавить menuId при необходимости) ---
                            var dish = guest.Dishes
                                .FirstOrDefault(d => d.DishName == dishName && d.Count == quantity);

                            if (dish == null)
                            {
                                dish = new GarconDish
                                {
                                    DishName = dishName,
                                    Count = quantity,
                                    DishPrice = basePrice,          // цена за ОДНУ порцию
                                    DishDiscount = 0                 // если есть скидки — добавьте
                                };
                                guest.Dishes.Add(dish);
                            }

                            // --- опция ---
                            if (!string.IsNullOrEmpty(optionName))
                            {
                                if (!dish.SelectedOptions.Any(o => o.OptionName == optionName))
                                {
                                    dish.SelectedOptions.Add(new DishSelectedOption
                                    {
                                        OptionName = optionName,
                                        OptionPrice = (double)optionPrice    // * quantity, если цена опции умножается
                                    });
                                }
                            }
                        }

                        // результат
                        guests = guestsByNumber.Values.ToList();
                    }
                }
            }

            // Выводим информацию в UI
            GuestsListView.ItemsSource = guests;

            var minutesAgo = (DateTime.Now - orderDateTime).TotalMinutes;
            OrderInfoTextBlock.Text =
                $"Заказ №{orderId}  Стол №{tableId}  {Math.Floor(minutesAgo)} мин. назад";

            var totalOrderPrice = guests.Sum(g => g.TotalPrice);
            PaymentTotalPrice.Text = $"Итого к оплате {totalOrderPrice} ₽";
        }
        private void PayWithCardButton_Click(object sender, RoutedEventArgs e)
        {
            Dottimer.Start();
            PaymentWait.Visibility = Visibility.Visible;
            PaymentMethodGlobal = "Карта";
          
        }

        private void PayWithCashButton_Click(object sender, RoutedEventArgs e)
        {
            Dottimer.Start();
            PaymentWait.Visibility = Visibility.Visible;
            PaymentMethodGlobal = "Наличные";
        }

        private void PayWithSBPButton_Click(object sender, RoutedEventArgs e)
        {
            Dottimer.Start();
            PaymentWait.Visibility = Visibility.Visible;
            PaymentMethodGlobal = "СБП";
        }

        private void CancelPaymentButton_Click(object sender, RoutedEventArgs e)
        {
            PaymentTab.Visibility = Visibility.Collapsed;
            PaymentWait.Visibility= Visibility.Collapsed;
            OrderGrid.Visibility = Visibility.Visible;
            MainMenuTabItem.IsSelected = true;
            ShowNotification("Оплата заказа отменена.");
        }

        private void PaymentSuccesfulButton_Click(object sender, RoutedEventArgs e)
        {
            PaymentTab.Visibility = Visibility.Collapsed;
            PaymentWait.Visibility = Visibility.Collapsed;
            OrderGrid.Visibility = Visibility.Visible;
            MainMenuTabItem.IsSelected = true;
            GenerateCheck(true);
            LoadCurrentOrders("WHERE o.Orders_Status != 'Завершен' AND o.Orders_Status != 'Отменён'");
            ShowNotification("Заказ успешно оплачен.");
        }

        private void OrdersFromKitchen_Click(object sender, RoutedEventArgs e)
        {
            KitchenWindow window = new KitchenWindow(true);
            window.ShowDialog();
        }

        private void ToLoginButton_Click(object sender, RoutedEventArgs e)
        {
            LoginTabItem.IsSelected= true;
        }
    }
}

