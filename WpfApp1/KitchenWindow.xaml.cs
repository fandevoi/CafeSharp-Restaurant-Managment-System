using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Data;
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
using System.Configuration;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using Newtonsoft.Json.Linq;
using System.IO;
using System.Windows.Threading;

namespace WpfApp1
{
    public class Order
    {
        public int Orders_ID { get; set; }
        public string OrderTime { get; set; }
        public string ServingTime { get; set; }
        public string Orders_Status { get; set; }
        public int Table_ID { get; set; }
        public string Garcon_Name { get; set; }
        public List<Dish> Dishes { get; set; } = new List<Dish>();
    }
    public class CategorizedOrders
    {
        public List<Order> NewOrders { get; private set; }
        public List<Order> PreparingOrders { get; private set; }
        public List<Order> AllReadyOrders { get; private set; }
        public CategorizedOrders()
        {
            NewOrders = new List<Order>();
            PreparingOrders = new List<Order>();
            AllReadyOrders = new List<Order>();
        }
    }
    public class Dish : INotifyPropertyChanged
    {
        public int Order_Dish_ID { get; set; } // Это PK из вашей таблицы Order_Dishes

        private string _menu_Name;
        public string Menu_Name // Из таблицы Menu
        {
            get => _menu_Name;
            set { _menu_Name = value; OnPropertyChanged(); }
        }

        private string _dish_Status;
        public string Dish_Status // Это значение будет храниться в Order_Dishes (нужно добавить столбец)
        {
            get { return _dish_Status; }
            set
            {
                if (_dish_Status != value)
                {
                    _dish_Status = value;
                    OnPropertyChanged(); // Это вызовет обновление UI (текст и цвет значка)
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public partial class KitchenWindow : Window
    {
        private SqlConnection connection = new SqlConnection(ConfigurationManager.ConnectionStrings["constr"].ConnectionString);
        string RoleGlobal;
        int IsBarParamGlobal;
        private DispatcherTimer _updateTimer;

        public KitchenWindow(bool IsFromGarcon = false, bool IsBarman = false)
        {
            InitializeComponent();
            RoleGlobal = "";
            IsBarParamGlobal = IsBarman == false ? 0 : 1;
            CheckDatabaseConnection();
            RefreshOrdersData();
            InitializeTimer();
        }
        private void InitializeTimer()
        {
            _updateTimer = new DispatcherTimer();
            _updateTimer.Tick += UpdateTimer_Tick;
            _updateTimer.Interval = TimeSpan.FromSeconds(5);
            _updateTimer.Start();
        }

        private void UpdateTimer_Tick(object sender, EventArgs e)
        {
            RefreshOrdersData();
        }

        private void RefreshOrdersData()
        {
            try
            {
                var categorizedData = LoadOrders(IsBarParamGlobal);
                this.DataContext = categorizedData;
            }
            catch (Exception ex)
            {
                 MessageBox.Show($"Ошибка при обновлении данных: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
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
                string query = "SELECT Login_Role FROM Login WHERE Login_Pinpad = @Password";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@Password", hashedPassword);
                    if (cmd.ExecuteScalar() != null)
                    {
                        RoleGlobal = (string)cmd.ExecuteScalar();
                        MainMenuTabItem.IsSelected = true;
                    }
                    else
                    {
                        RoleGlobal = "";
                    }
                }
            }
        }
        private byte[] HashPassword(string password)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                return sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            }
        }
        public CategorizedOrders LoadOrders(int IsBarParam)
        {
            var categorizedOrders = new CategorizedOrders();
            var orderDict = new Dictionary<int, Order>();
            try
            {
                if (connection.State == System.Data.ConnectionState.Closed)
                {
                    connection.Open();
                }

                string sql = $@"
        SELECT 
            o.Orders_ID, 
            CONVERT(varchar(5), o.Orders_Time, 108) AS OrderTime,
            CONVERT(varchar(5), o.Orders_Serving_Time, 108) AS ServingTime,
            o.Orders_Status,
            t.Tables_ID, 
            g.Garcon_Name,
            od.Order_Dish_ID,
            m.Menu_Name,
            od.Dish_Status 
        FROM Orders o
        JOIN Tables t ON o.Table_ID = t.Tables_ID 
        JOIN Garcon g ON o.Garcon_ID = g.Garcon_ID
        JOIN Order_Dishes od ON od.Orders_ID = o.Orders_ID
        JOIN Menu m ON od.Menu_ID = m.Menu_ID
        WHERE o.Orders_Status NOT IN ('Завершен', 'Отменён') 
          AND ISNULL(m.IsBar, 0) = {IsBarParam}
        ORDER BY o.Orders_Date DESC, o.Orders_Time DESC";

                using (SqlCommand cmd = new SqlCommand(sql, connection))
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        int orderId = reader.GetInt32(reader.GetOrdinal("Orders_ID"));
                        if (!orderDict.ContainsKey(orderId))
                        {
                            orderDict[orderId] = new Order
                            {
                                Orders_ID = orderId,
                                OrderTime = reader.GetString(reader.GetOrdinal("OrderTime")),
                                ServingTime = reader.IsDBNull(reader.GetOrdinal("ServingTime")) ? "" : reader.GetString(reader.GetOrdinal("ServingTime")),
                                Orders_Status = reader.GetString(reader.GetOrdinal("Orders_Status")),
                                Table_ID = reader.GetInt32(reader.GetOrdinal("Tables_ID")),
                                Garcon_Name = reader.GetString(reader.GetOrdinal("Garcon_Name")),
                                Dishes = new List<Dish>() // Важно инициализировать список блюд
                            };
                        }

                        var dish = new Dish
                        {
                            Order_Dish_ID = reader.GetInt32(reader.GetOrdinal("Order_Dish_ID")),
                            Menu_Name = reader.GetString(reader.GetOrdinal("Menu_Name")),
                            // Если Dish_Status из БД NULL, он станет пустой строкой ""
                            Dish_Status = reader.IsDBNull(reader.GetOrdinal("Dish_Status")) ? "" : reader.GetString(reader.GetOrdinal("Dish_Status")),
                        };
                        orderDict[orderId].Dishes.Add(dish);
                    }
                } // reader и cmd будут здесь закрыты/освобождены

                foreach (Order order in orderDict.Values)
                {
                    if (order.Dishes == null || !order.Dishes.Any()) // Пропускаем заказы без блюд
                    {
                        continue;
                    }

                    // Статусы для сравнения (для ясности и избежания опечаток)
                    const string statusPreparing = "Готовится";
                    const string statusReady = "Готово";
                    // Предполагаем, что статус "Отменить" устанавливается кнопкой "Отменить"
                    // Если в БД хранится как "Отменено", используйте это значение.
                    const string statusCancelled = "Отменить";

                    // 1. Сначала проверяем на "Готовится" - это самый высокий приоритет
                    if (order.Dishes.Any(d => d.Dish_Status == statusPreparing))
                    {
                        categorizedOrders.PreparingOrders.Add(order);
                        continue; // Заказ категоризирован, переходим к следующему
                    }

                    // 2. Затем проверяем на "Все активные блюда готовы"
                    // Отбираем только те блюда, которые не были отменены
                    var activeDishes = order.Dishes.Where(d => d.Dish_Status != statusCancelled).ToList();

                    // Если есть хотя бы одно активное (не отмененное) блюдо,
                    // и все из них имеют статус "Готово"
                    if (activeDishes.Any() && activeDishes.All(d => d.Dish_Status == statusReady))
                    {
                        categorizedOrders.AllReadyOrders.Add(order);
                        continue; // Заказ категоризирован, переходим к следующему
                    }

                    // 3. Затем проверяем на "Новый заказ" (все блюда без статуса)
                    // Этот заказ не "Готовится" и не "Все активные готовы".
                    // Новый заказ - это когда ВСЕ ИЗНАЧАЛЬНЫЕ блюда имеют пустой статус.
                    if (order.Dishes.All(d => string.IsNullOrEmpty(d.Dish_Status)))
                    {
                        categorizedOrders.NewOrders.Add(order);
                        continue; // Заказ категоризирован, переходим к следующему
                    }
                }

            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке заказов: {ex.Message}", "Ошибка БД");
                System.Diagnostics.Debug.WriteLine($"LoadOrders Error: {ex.ToString()}");
            }
            return categorizedOrders;
        }
        private Border _lastSelectedDishBorder = null;
        private bool UpdateDishStatusInDb(int orderDishId, string status)
        {
            using (SqlConnection connection = new SqlConnection(ConfigurationManager.ConnectionStrings["constr"].ConnectionString))
            {
                // Используйте параметризованные запросы для предотвращения SQL-инъекций
                string query = "UPDATE Order_Dishes SET Dish_Status = @Status WHERE Order_Dish_ID = @OrderDishID";
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Status", status);
                    command.Parameters.AddWithValue("@OrderDishID", orderDishId);

                    try
                    {
                        connection.Open();
                        int rowsAffected = command.ExecuteNonQuery();
                        return rowsAffected > 0; // true, если хотя бы одна строка была обновлена
                    }
                    catch (SqlException ex)
                    {
                        // Здесь можно логировать ошибку или показать пользователю
                        MessageBox.Show($"Ошибка SQL: {ex.Message}", "Ошибка Базы Данных", MessageBoxButton.OK, MessageBoxImage.Error);
                        System.Diagnostics.Debug.WriteLine($"SQL Error: {ex.ToString()}"); // Для отладки
                        return false;
                    }
                    catch (Exception ex) // Общий обработчик ошибок
                    {
                        MessageBox.Show($"Произошла ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                        System.Diagnostics.Debug.WriteLine($"General Error: {ex.ToString()}"); // Для отладки
                        return false;
                    }
                }
            }
        }
        private void StatusButton_Click(object sender, RoutedEventArgs e)
        {
            if (!(sender is Button clickedButton)) return;
            if (!(clickedButton.DataContext is Dish selectedDish)) return; // DataContext - это объект Dish
           
            string newStatus = clickedButton.Content.ToString();

            // 1. Попытаться обновить статус в базе данных
            bool success = UpdateDishStatusInDb(selectedDish.Order_Dish_ID, newStatus);

            if (success)
            {
                // 2. Если в БД успешно, обновить свойство объекта Dish
                // INotifyPropertyChanged позаботится об обновлении UI
                selectedDish.Dish_Status = newStatus;
                // Опционально: можно скрыть панель кнопок
                if (clickedButton.Parent is StackPanel buttonsPanel) // Parent - это ButtonsPanel
                {
                    buttonsPanel.Visibility = Visibility.Collapsed;
                }
                LoadOrders(IsBarParamGlobal);
            }
            else
            {
                MessageBox.Show($"Не удалось обновить статус блюда '{selectedDish.Menu_Name}' в базе данных.", "Ошибка БД", MessageBoxButton.OK, MessageBoxImage.Error);
                // Можно добавить логику отката или перезагрузки данных, если требуется
            }
        }
        private void Border_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                this.DragMove();
            }
        }
        private bool IsMaximize = false;
        private void DishBorder_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (RoleGlobal == "официант") return;
            var border = sender as Border;
            if (border == null) return;

            var buttonsPanel = FindChild<StackPanel>(border, "ButtonsPanel");
            if (buttonsPanel == null) return;

            if (_lastSelectedDishBorder != null && _lastSelectedDishBorder != border)
            {
                var lastButtonsPanel = FindChild<StackPanel>(_lastSelectedDishBorder, "ButtonsPanel");
                if (lastButtonsPanel != null)
                    lastButtonsPanel.Visibility = Visibility.Collapsed;
            }

            // Переключаем видимость кнопок у текущего блюда
            buttonsPanel.Visibility = buttonsPanel.Visibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;

            _lastSelectedDishBorder = border;
        }
        // Вспомогательный метод для поиска дочернего элемента по имени
        public static T FindChild<T>(DependencyObject parent, string childName) where T : FrameworkElement
        {
            if (parent == null) return null;

            int childrenCount = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < childrenCount; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T tChild && tChild.Name == childName)
                    return tChild;

                var foundChild = FindChild<T>(child, childName);
                if (foundChild != null)
                    return foundChild;
            }
            return null;
        }

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
    }
}

