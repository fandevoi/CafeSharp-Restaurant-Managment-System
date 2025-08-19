using Microsoft.ReportingServices.Diagnostics.Internal;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
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

namespace WpfApp1
{
    /// <summary>
    /// Логика взаимодействия для menuEditWindow.xaml
    /// </summary>
    public partial class menuEditWindow : Window
    {
        
        public int CurrentMenuId { get; set; }
        public bool IsAddGlobal { get; set; }
        public string Menu_Name { get; set; }
        public string Menu_Type { get; set; }
        public int Menu_Price { get; set; }
        public int? Menu_Discount { get; set; } 
        public bool IsBar { get; set; }
        public byte[] Image { get; set; } 

        public class DishOption
        {
            public int Option_ID { get; set; }
            public string Option_Name { get; set; }
        }
        public List<string> MenuTypes { get; set; } = new List<string>();
        public List<DishOption> AvailableOptions { get; set; } = new List<DishOption>();
        public List<DishOption> SelectedDishOptions { get; set; } = new List<DishOption>();
        public menuEditWindow(int selectedMenuId,bool isAdd)
        {
            InitializeComponent();
            InitializeComponent();
            CurrentMenuId = selectedMenuId;
            IsAddGlobal = isAdd;
            this.DataContext = this; 
            if (CurrentMenuId != 0)
            {
                Title.Text = "Изменить блюдо";
                LoadMenuData(CurrentMenuId);
                LoadSelectedToppings(CurrentMenuId);
                deleteButton.Visibility = Visibility.Visible;
            }
            LoadMenuTypes();
            LoadAvailableToppings();
        }
        private void LoadMenuData(int menuId)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(ConfigurationManager.ConnectionStrings["constr"].ConnectionString))
                {
                    connection.Open();
                    string query = "SELECT Menu_Name, Menu_Image, Menu_Price, Menu_Type, Menu_Discount, Menu_IsStop,IsBar FROM Menu WHERE Menu_ID = @MenuID";
                    SqlCommand command = new SqlCommand(query, connection);
                    command.Parameters.AddWithValue("@MenuID", menuId);

                    SqlDataReader reader = command.ExecuteReader();
                    if (reader.Read())
                    {
                        Menu_Name = reader["Menu_Name"].ToString();
                        Menu_Price = Convert.ToInt32(reader["Menu_Price"]);
                        Menu_Type = reader["Menu_Type"].ToString();
                        Menu_Discount = reader["Menu_Discount"] != DBNull.Value ? Convert.ToInt32(reader["Menu_Discount"]) : (int?)null;
                        IsBar = reader["IsBar"] != DBNull.Value ? Convert.ToBoolean(reader["IsBar"]) : false; 

                        if (reader["Menu_Image"] != DBNull.Value)
                        {
                            Image = (byte[])reader["Menu_Image"];
                            BitmapImage bitmap = new BitmapImage();
                            using (MemoryStream stream = new MemoryStream(Image))
                            {
                                bitmap.BeginInit();
                                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                                bitmap.StreamSource = stream;
                                bitmap.EndInit();
                            }
                        }
                    }
                    reader.Close();
                }

                nameTextBlock.Text = Menu_Name;
                priceTextBlock.Text = Menu_Price.ToString();
                discountTextBox.Text = Menu_Discount?.ToString() ?? string.Empty;
                isBarCheckBox.IsChecked = IsBar;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке данных блюда: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadMenuTypes()
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(ConfigurationManager.ConnectionStrings["constr"].ConnectionString))
                {
                    connection.Open();
                    string query = "SELECT DISTINCT Menu_Type FROM Menu";
                    SqlCommand command = new SqlCommand(query, connection);

                    SqlDataReader reader = command.ExecuteReader();
                    while (reader.Read())
                    {
                        MenuTypes.Add(reader["Menu_Type"].ToString());
                    }
                    reader.Close();
                }
                typeComboBox.ItemsSource = MenuTypes;
                typeComboBox.SelectedItem = Menu_Type; 
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке видов блюд: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadAvailableToppings()
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(ConfigurationManager.ConnectionStrings["constr"].ConnectionString))
                {
                    connection.Open();
                    string query = @"
                        SELECT DO.Option_ID, DO.Option_Name
                        FROM Dish_Options DO
                        WHERE DO.Is_Active = 1
                        AND DO.Option_ID NOT IN (
                            SELECT MDO.Option_ID
                            FROM Menu_Dish_Options MDO
                            WHERE MDO.Menu_ID = @MenuID
                        )";
                    SqlCommand command = new SqlCommand(query, connection);
                    command.Parameters.AddWithValue("@MenuID", CurrentMenuId);

                    SqlDataReader reader = command.ExecuteReader();
                    AvailableOptions.Clear(); 
                    while (reader.Read())
                    {
                        AvailableOptions.Add(new DishOption
                        {
                            Option_ID = Convert.ToInt32(reader["Option_ID"]),
                            Option_Name = reader["Option_Name"].ToString()
                        });
                    }
                    reader.Close();
                }
                availableToppingsComboBox.ItemsSource = AvailableOptions;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке доступных топпингов: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadSelectedToppings(int menuId)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(ConfigurationManager.ConnectionStrings["constr"].ConnectionString))
                {
                    connection.Open();
                    string query = @"
                        SELECT DO.Option_ID, DO.Option_Name
                        FROM Menu_Dish_Options MDO
                        JOIN Dish_Options DO ON MDO.Option_ID = DO.Option_ID
                        WHERE MDO.Menu_ID = @MenuID";
                    SqlCommand command = new SqlCommand(query, connection);
                    command.Parameters.AddWithValue("@MenuID", menuId);

                    SqlDataReader reader = command.ExecuteReader();
                    SelectedDishOptions.Clear(); 
                    while (reader.Read())
                    {
                        SelectedDishOptions.Add(new DishOption
                        {
                            Option_ID = Convert.ToInt32(reader["Option_ID"]),
                            Option_Name = reader["Option_Name"].ToString()
                        });
                    }
                    reader.Close();
                }
           
                selectedToppingsListBox.ItemsSource = null; 
                selectedToppingsListBox.ItemsSource = SelectedDishOptions;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке выбранных топпингов: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void AddTopping_Click(object sender, RoutedEventArgs e)
        {
            if (availableToppingsComboBox.SelectedItem is DishOption selectedOption)
            {
                try
                {
                    using (SqlConnection connection = new SqlConnection(ConfigurationManager.ConnectionStrings["constr"].ConnectionString))
                    {
                        connection.Open();
                        string query = "INSERT INTO Menu_Dish_Options (Menu_ID, Option_ID) VALUES (@MenuID, @OptionID)";
                        SqlCommand command = new SqlCommand(query, connection);
                        command.Parameters.AddWithValue("@MenuID", CurrentMenuId);
                        command.Parameters.AddWithValue("@OptionID", selectedOption.Option_ID);
                        command.ExecuteNonQuery();
                    }

                  
                    LoadSelectedToppings(CurrentMenuId);
                    LoadAvailableToppings();
                    availableToppingsComboBox.SelectedItem = null; 
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при добавлении топпинга: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                MessageBox.Show("Пожалуйста, выберите топпинг для добавления.", "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void SelectImage_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Image files (*.png;*.jpeg;*.jpg;*.gif;*.bmp)|*.png;*.jpeg;*.jpg;*.gif;*.bmp|All files (*.*)|*.*";
            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    Image = File.ReadAllBytes(openFileDialog.FileName);
                    BitmapImage bitmap = new BitmapImage();
                    using (MemoryStream stream = new MemoryStream(Image))
                    {
                        bitmap.BeginInit();
                        bitmap.CacheOption = BitmapCacheOption.OnLoad;
                        bitmap.StreamSource = stream;
                        bitmap.EndInit();
                    }
                    dishImage.Source = bitmap; 
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при загрузке изображения: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void RemoveTopping_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is int optionIdToRemove)
            {
                try
                {
                    using (SqlConnection connection = new SqlConnection(ConfigurationManager.ConnectionStrings["constr"].ConnectionString))
                    {
                        connection.Open();
                        string query = "DELETE FROM Menu_Dish_Options WHERE Menu_ID = @MenuID AND Option_ID = @OptionID";
                        SqlCommand command = new SqlCommand(query, connection);
                        command.Parameters.AddWithValue("@MenuID", CurrentMenuId);
                        command.Parameters.AddWithValue("@OptionID", optionIdToRemove);
                        command.ExecuteNonQuery();
                    }

                    LoadSelectedToppings(CurrentMenuId);
                    LoadAvailableToppings(); 
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при удалении топпинга: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void saveButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(nameTextBlock.Text))
            {
                MessageBox.Show("Наименование блюда не может быть пустым.", "Ошибка ввода", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (!int.TryParse(priceTextBlock.Text, out int price) || price < 0)
            {
                MessageBox.Show("Цена должна быть положительным числом.", "Ошибка ввода", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            int? discount = null;
            if (string.IsNullOrWhiteSpace(discountTextBox.Text))
            {
               
                MessageBox.Show("Поле 'Скидка' не может быть пустым. Введите 0, если скидки нет.", "Ошибка ввода", MessageBoxButton.OK, MessageBoxImage.Warning);
                return; 
            }
            if (!string.IsNullOrWhiteSpace(discountTextBox.Text))
            {
                if (int.TryParse(discountTextBox.Text, out int parsedDiscount) && parsedDiscount >= 0 && parsedDiscount <= 100)
                {
                    discount = parsedDiscount;
                }
                else
                {
                    MessageBox.Show("Скидка должна быть числом от 0 до 100.", "Ошибка ввода", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
            }
            if (IsAddGlobal!=true)
            {
                try
                {
                    using (SqlConnection connection = new SqlConnection(ConfigurationManager.ConnectionStrings["constr"].ConnectionString))
                    {
                        connection.Open();
                        string query = @"
                        UPDATE Menu
                        SET
                            Menu_Name = @MenuName,
                            Menu_Price = @MenuPrice,
                            Menu_Type = @MenuType,
                            Menu_Discount = @MenuDiscount,
                            IsBar = @IsBar,
                            Menu_Image = @MenuImage,
                            Menu_Edit_Date = GETDATE()
                        WHERE Menu_ID = @MenuID";

                        SqlCommand command = new SqlCommand(query, connection);
                        command.Parameters.AddWithValue("@MenuName", nameTextBlock.Text);
                        command.Parameters.AddWithValue("@MenuPrice", price);
                        command.Parameters.AddWithValue("@MenuType", typeComboBox.SelectedItem?.ToString());
                        command.Parameters.AddWithValue("@MenuDiscount", (object)discount ?? DBNull.Value);
                        command.Parameters.AddWithValue("@IsBar", isBarCheckBox.IsChecked ?? false);
                        command.Parameters.AddWithValue("@MenuImage", (object)Image ?? DBNull.Value);
                        command.Parameters.AddWithValue("@MenuID", CurrentMenuId);

                        command.ExecuteNonQuery();
                    }

                    MessageBox.Show("Данные блюда успешно обновлены!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                    this.DialogResult = true; 
                    this.Close();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при сохранении данных блюда: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else 
            {
                try
                {
                    using (SqlConnection connection = new SqlConnection(ConfigurationManager.ConnectionStrings["constr"].ConnectionString))
                    {
                        connection.Open();
                        string query = @"
                         INSERT INTO Menu (Menu_Name, Menu_Image, Menu_Price, Menu_Type, Menu_Discount,IsBar, Menu_Edit_Date)
                        VALUES (@MenuName, @MenuImage, @MenuPrice, @MenuType, @MenuDiscount, @IsBar, GETDATE());";

                        SqlCommand command = new SqlCommand(query, connection);
                        command.Parameters.AddWithValue("@MenuName", nameTextBlock.Text);
                        command.Parameters.AddWithValue("@MenuImage", (object)Image ?? DBNull.Value);
                        command.Parameters.AddWithValue("@MenuPrice", price);
                        command.Parameters.AddWithValue("@MenuType", typeComboBox.SelectedItem?.ToString());
                        command.Parameters.AddWithValue("@MenuDiscount", (object)discount ?? 0);
                        command.Parameters.AddWithValue("@IsBar", isBarCheckBox.IsChecked ?? false);
                        command.Parameters.AddWithValue("@MenuID", CurrentMenuId);

                        command.ExecuteNonQuery();
                    }

                    MessageBox.Show("Блюдо добавлено!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                    this.DialogResult = true; 
                    this.Close();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при сохранении данных блюда: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
        private void deleteButton_Click(object sender, RoutedEventArgs e)
        {

            var Result = MessageBox.Show("Вы точно хотите удалить блюдо?", "Предупреждение об удалении", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (Result == MessageBoxResult.Yes)
            {
                Exception exeptionFlag = null;
                using (SqlConnection connection = new SqlConnection(ConfigurationManager.ConnectionStrings["constr"].ConnectionString))
                {
                    try
                    {
                        connection.Open();
                        SqlCommand cmd = new SqlCommand($"DELETE FROM Menu WHERE Menu_ID='{CurrentMenuId}';", connection);
                        cmd.ExecuteScalar();
                    }
                    catch (Exception ex)
                    {
                        exeptionFlag = ex;
                        MessageBox.Show("Ошибка при удалении: " + ex.Message);
                    }
                    finally
                    {
                        if (exeptionFlag == null)
                        {
                            MessageBox.Show("Блюдо успешно удалено!", "Удалено", MessageBoxButton.OK, MessageBoxImage.Information);
                            this.DialogResult = true;
                            this.Close();
                        }
                        connection.Close();
                    }
                }
            }
            else if (Result == MessageBoxResult.No)
            {

            }
        }
        private void Border_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                this.DragMove();
            }
        }
    }
}
