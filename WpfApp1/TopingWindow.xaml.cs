using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace WpfApp1
{
    public partial class TopingWindow : Window
    {
        public class ToppingDataModel
        {
            public int Option_ID { get; set; }
            public string Option_Name { get; set; }
            public string Option_Type { get; set; }
            public decimal Option_Price { get; set; }
            public bool Is_Active { get; set; }
            public bool? Is_Important { get; set; }
        }

        private List<ToppingDataModel> _toppings = new List<ToppingDataModel>();
        public TopingWindow()
        {
            InitializeComponent();
            LoadOptionTypesToComboBox();
            LoadToppings();
        }
        private void AddTopping_Click(object sender, RoutedEventArgs e)
        {
            string optionName = optionNameTextBox.Text.Trim();
            string optionType = optionTypeComboBox.Text.Trim();
            if (!decimal.TryParse(optionPriceTextBox.Text, out decimal optionPrice) || optionPrice < 0)
            {
                MessageBox.Show("Цена должна быть неотрицательным числом.", "Ошибка ввода", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            bool isActive = isActiveCheckBox.IsChecked ?? false; 
            bool isImportant = isImportantCheckBox.IsChecked ?? false; 

            if (string.IsNullOrWhiteSpace(optionName))
            {
                MessageBox.Show("Пожалуйста, введите название топпинга.", "Ошибка ввода", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(optionType))
            {
                MessageBox.Show("Пожалуйста, выберите тип топпинга.", "Ошибка ввода", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                using (SqlConnection connection = new SqlConnection(ConfigurationManager.ConnectionStrings["constr"].ConnectionString))
                {
                    connection.Open();

                    string query = @"
                        INSERT INTO Dish_Options (Option_Name, Option_Type, Option_Price, Is_Active, Is_Important)
                        VALUES (@OptionName, @OptionType, @OptionPrice, @IsActive, @IsImportant)";

                    SqlCommand command = new SqlCommand(query, connection);
                    command.Parameters.AddWithValue("@OptionName", optionName);
                    command.Parameters.AddWithValue("@OptionType", optionType);
                    command.Parameters.AddWithValue("@OptionPrice", optionPrice);
                    command.Parameters.AddWithValue("@IsActive", isActive);
                    command.Parameters.AddWithValue("@IsImportant", isImportant);

                    command.ExecuteNonQuery(); 
                }

                MessageBox.Show("Топпинг успешно добавлен!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                LoadOptionTypesToComboBox();
                this.DialogResult = true; 
                this.Close(); 
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при добавлении топпинга: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void LoadOptionTypesToComboBox()
        {
            List<string> optionTypes = new List<string>();

            try
            {
                using (SqlConnection connection = new SqlConnection(ConfigurationManager.ConnectionStrings["constr"].ConnectionString))
                {
                    connection.Open();

                    string query = "SELECT DISTINCT Option_Type FROM Dish_Options WHERE Option_Type IS NOT NULL AND Option_Type != '' ORDER BY Option_Type";

                    SqlCommand command = new SqlCommand(query, connection);
                    SqlDataReader reader = command.ExecuteReader();

                    while (reader.Read())
                    {
                        optionTypes.Add(reader["Option_Type"].ToString());
                    }
                    reader.Close();
                }
                optionTypeComboBox.ItemsSource = optionTypes;

                if (optionTypes.Count > 0)
                {
                    optionTypeComboBox.SelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке типов топпингов: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void LoadToppings()
        {
            _toppings.Clear(); 
            try
            {
                using (SqlConnection connection = new SqlConnection(ConfigurationManager.ConnectionStrings["constr"].ConnectionString))
                {
                    connection.Open();
                    string query = "SELECT Option_ID, Option_Name, Option_Type, Option_Price, Is_Active, Is_Important FROM Dish_Options ORDER BY Option_Name";
                    SqlCommand command = new SqlCommand(query, connection);
                    SqlDataReader reader = command.ExecuteReader();

                    while (reader.Read())
                    {
                        _toppings.Add(new ToppingDataModel
                        {
                            Option_ID = Convert.ToInt32(reader["Option_ID"]),
                            Option_Name = reader["Option_Name"].ToString(),
                            Option_Type = reader["Option_Type"].ToString(),
                            Option_Price = Convert.ToDecimal(reader["Option_Price"]),
                            Is_Active = Convert.ToBoolean(reader["Is_Active"]),
                            Is_Important = reader["Is_Important"] != DBNull.Value ? Convert.ToBoolean(reader["Is_Important"]) : (bool?)null
                        });
                    }
                    reader.Close();
                }
                toppingsListBox.ItemsSource = null; 
                toppingsListBox.ItemsSource = _toppings;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке топпингов: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void RemoveTopping_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is int toppingId)
            {
                MessageBoxResult result = MessageBox.Show(
                    $"Вы уверены, что хотите удалить топпинг с ID: {toppingId}? Это также удалит его из всех блюд!",
                    "Подтверждение удаления",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        using (SqlConnection connection = new SqlConnection(ConfigurationManager.ConnectionStrings["constr"].ConnectionString))
                        {
                            connection.Open();

                            string deleteMenuToppingQuery = "DELETE FROM Menu_Dish_Options WHERE Option_ID = @OptionID";
                            SqlCommand deleteMenuToppingCommand = new SqlCommand(deleteMenuToppingQuery, connection);
                            deleteMenuToppingCommand.Parameters.AddWithValue("@OptionID", toppingId);
                            deleteMenuToppingCommand.ExecuteNonQuery();

                            string deleteToppingQuery = "DELETE FROM Dish_Options WHERE Option_ID = @OptionID";
                            SqlCommand deleteToppingCommand = new SqlCommand(deleteToppingQuery, connection);
                            deleteToppingCommand.Parameters.AddWithValue("@OptionID", toppingId);
                            deleteToppingCommand.ExecuteNonQuery();
                        }

                        MessageBox.Show("Топпинг успешно удален.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                        LoadToppings(); 
                        LoadOptionTypesToComboBox();

                    }
                    catch (SqlException sqlEx)
                    {
                        if (sqlEx.Number == 547) 
                        {
                            MessageBox.Show("Невозможно удалить топпинг, так как он используется в заказах или других связанных таблицах. Сначала удалите его из них.", "Ошибка удаления", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                        else
                        {
                            MessageBox.Show($"Ошибка базы данных при удалении топпинга: {sqlEx.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка при удалении топпинга: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }
       
        
        public class BooleanToActiveConverter : IValueConverter
        {
            public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            {
                if (value is bool isActive)
                {
                    return isActive ? "Активен" : "Неактивен";
                }
                return string.Empty;
            }

            public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            {
                throw new NotImplementedException();
            }
        }

    }
}

