using MahApps.Metro.IconPacks;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
    public partial class StopListWindow : Window
    {
        public class DishDisplayItem
        {
            public int Menu_ID { get; set; }
            public string Menu_Name { get; set; }
            public bool Menu_IsStop { get; set; }

            public bool IsInStopList => Menu_IsStop;
        }
        public ObservableCollection<DishDisplayItem> Dishes { get; set; }
        public StopListWindow()
        {
            InitializeComponent();
            Dishes = new ObservableCollection<DishDisplayItem>();
            this.DataContext = this; 
            LoadDishesFromDatabase();
        }
        private void LoadDishesFromDatabase()
        {
            Dishes.Clear();

            string query = "SELECT Menu_ID, Menu_Name, CAST(COALESCE(Menu_IsStop, 0) AS BIT) AS Menu_IsStop FROM Menu;";

            using (SqlConnection connection = new SqlConnection(ConfigurationManager.ConnectionStrings["constr"].ConnectionString))
            {
                SqlCommand command = new SqlCommand(query, connection);
                try
                {
                    connection.Open();
                    SqlDataReader reader = command.ExecuteReader();

                    while (reader.Read())
                    {
                        Dishes.Add(new DishDisplayItem
                        {
                            Menu_ID = reader.GetInt32(reader.GetOrdinal("Menu_ID")),
                            Menu_Name = reader.GetString(reader.GetOrdinal("Menu_Name")),
                            Menu_IsStop = reader.GetBoolean(reader.GetOrdinal("Menu_IsStop"))
                        });
                    }
                    reader.Close();
                    dishesListBox.ItemsSource = null;
                    dishesListBox.ItemsSource = Dishes;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при загрузке блюд: {ex.Message}", "Ошибка базы данных", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void ToggleStopList_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            if (button != null)
            {
                int menuId = (int)button.Tag;

                DishDisplayItem dishToUpdate = null;
                foreach (var dish in Dishes)
                {
                    if (dish.Menu_ID == menuId)
                    {
                        dishToUpdate = dish;
                        break;
                    }
                }

                if (dishToUpdate != null)
                {
                    bool newIsStopStatus = !dishToUpdate.Menu_IsStop;

                    UpdateDishStopStatusInDatabase(menuId, newIsStopStatus);

                    dishToUpdate.Menu_IsStop = newIsStopStatus;
                    RefreshListBoxItem(dishToUpdate);
                    MessageBox.Show($"Статус стоп-листа для \"{dishToUpdate.Menu_Name}\" изменен на: {(dishToUpdate.Menu_IsStop ? "В стоп-листе" : "Не в стоп-листе")}", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
        }

        private void UpdateDishStopStatusInDatabase(int menuId, bool isStop)
        {
            string query = "UPDATE Menu SET Menu_IsStop = @IsStop WHERE Menu_ID = @MenuID;";

            using (SqlConnection connection = new SqlConnection(ConfigurationManager.ConnectionStrings["constr"].ConnectionString))
            {
                SqlCommand command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@IsStop", isStop);
                command.Parameters.AddWithValue("@MenuID", menuId);

                try
                {
                    connection.Open();
                    command.ExecuteNonQuery();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при обновлении статуса блюда в базе данных: {ex.Message}", "Ошибка базы данных", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void RefreshListBoxItem(DishDisplayItem item)
        {
            int index = Dishes.IndexOf(item);
            if (index != -1)
            {
                Dishes.RemoveAt(index);
                Dishes.Insert(index, item);
            }
        }
    }

    public class BoolToTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is bool isStop)
            {
                return isStop ? "Убрать из стопа" : "Добавить в стоп";
            }
            return DependencyProperty.UnsetValue;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class BoolToIconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is bool isStop)
            {
                return isStop ? PackIconMaterialKind.CheckCircleOutline : PackIconMaterialKind.MinusCircleOutline;
            }
            return DependencyProperty.UnsetValue;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    public class BoolToStyleConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is bool isStop)
            {
               
                if (isStop)
                {
                    return Application.Current.FindResource("menuDialogButtonOrange") as Style;
                }
                else
                {
                 
                    return Application.Current.FindResource("menuDialogButtonGreen") as Style;
                }
            }
            return DependencyProperty.UnsetValue; 
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException(); 
        }
    }
}

