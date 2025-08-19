using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Globalization;
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
using System.Windows.Navigation;
using System.Windows.Shapes;
using LiveCharts;
using LiveCharts.Wpf;
using Microsoft.Reporting.WinForms;

namespace WpfApp1
{
    /// <summary>
    /// Логика взаимодействия для Report.xaml
    /// </summary>
    public partial class Report : Window
    {
        private SqlConnection connection = new SqlConnection(ConfigurationManager.ConnectionStrings["constr"].ConnectionString);
        // Коллекции для графиков
        public SeriesCollection OrdersTrendSeries { get; set; }
        public SeriesCollection RevenueTrendSeries { get; set; }
        public SeriesCollection TopDishesSeries { get; set; }
        public SeriesCollection TopDrinksSeries { get; set; }

        private CultureInfo russianCulture = CultureInfo.GetCultureInfo("ru-RU");
        public Report()
        {
            InitializeComponent();
            // Инициализация коллекций и привязка к графикам
            OrdersTrendSeries = new SeriesCollection();
            OrdersTrendChart.Series = OrdersTrendSeries;

            RevenueTrendSeries = new SeriesCollection();
            RevenueTrendChart.Series = RevenueTrendSeries;
            RevenueTrendChartYAxis.LabelFormatter = value => value.ToString("C", russianCulture); // Формат валюты

            TopDishesSeries = new SeriesCollection();
            TopDishesChart.Series = TopDishesSeries;

            TopDrinksSeries = new SeriesCollection();
            TopDrinksChart.Series = TopDrinksSeries;

            // Установка дат по умолчанию (например, текущий месяц)
            StartDatePicker.SelectedDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            EndDatePicker.SelectedDate = DateTime.Now;

            this.Loaded += Window_Loaded;
        }
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            LoadAllDashboardData();
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            LoadAllDashboardData();
        }

        private void LoadAllDashboardData()
        {
            if (!StartDatePicker.SelectedDate.HasValue || !EndDatePicker.SelectedDate.HasValue)
            {
                MessageBox.Show("Пожалуйста, выберите начальную и конечную даты.", "Ошибка ввода", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            DateTime startDate = StartDatePicker.SelectedDate.Value;
            DateTime endDate = EndDatePicker.SelectedDate.Value.AddDays(1).AddSeconds(-1); // Включая весь конечный день

            LoadOrdersTrendData(startDate, endDate);
            LoadRevenueTrendData(startDate, endDate);
            LoadTopDishesData(startDate, endDate, 5); // Топ 5
            LoadTopDrinksData(startDate, endDate, 5); // Топ 5
            LoadKpiData(startDate, endDate);
        }

        // --- МЕТОДЫ ЗАГРУЗКИ ДАННЫХ ---

        private void LoadOrdersTrendData(DateTime startDate, DateTime endDate)
        {
            OrdersTrendSeries.Clear();
            var labels = new List<string>();
            var totalOrdersValues = new ChartValues<int>();
            var canceledOrdersValues = new ChartValues<int>();

            // !!! ЗАМЕНИТЕ 'Отменен' НА ВАШ СТАТУС ОТМЕНЕННОГО ЗАКАЗА В БД !!!
            string query = @"
                SELECT
                    YEAR(o.Orders_Date) AS OrderYear,
                    MONTH(o.Orders_Date) AS OrderMonth,
                    COUNT(o.Orders_ID) AS TotalOrders,
                    SUM(CASE WHEN o.Orders_Status = 'Отменен' THEN 1 ELSE 0 END) AS CanceledOrders
                FROM Orders o
                WHERE o.Orders_Date BETWEEN @StartDate AND @EndDate
                GROUP BY YEAR(o.Orders_Date), MONTH(o.Orders_Date)
                ORDER BY OrderYear, OrderMonth;";

            using (SqlConnection connection = new SqlConnection(ConfigurationManager.ConnectionStrings["constr"].ConnectionString))
            {
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@StartDate", startDate);
                    command.Parameters.AddWithValue("@EndDate", endDate);
                    try
                    {
                        connection.Open();
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                int year = Convert.ToInt32(reader["OrderYear"]);
                                int month = Convert.ToInt32(reader["OrderMonth"]);
                                labels.Add($"{russianCulture.DateTimeFormat.GetAbbreviatedMonthName(month)} {year}");
                                totalOrdersValues.Add(Convert.ToInt32(reader["TotalOrders"]));
                                canceledOrdersValues.Add(Convert.ToInt32(reader["CanceledOrders"]));
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка загрузки трендов заказов: {ex.Message}", "Ошибка БД", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            OrdersTrendSeries.Add(new LineSeries { Title = "Всего заказов", Values = totalOrdersValues });
            OrdersTrendSeries.Add(new LineSeries { Title = "Отменено заказов", Values = canceledOrdersValues });
            OrdersTrendChartXAxis.Labels = labels.ToArray();
        }

        private void LoadRevenueTrendData(DateTime startDate, DateTime endDate)
        {
            RevenueTrendSeries.Clear();
            var labels = new List<string>();
            var revenueValues = new ChartValues<decimal>();

            // !!! ЗАМЕНИТЕ 'Отменен' НА ВАШ СТАТУС ОТМЕНЕННОГО ЗАКАЗА В БД !!!
            string query = @"
                SELECT
                    YEAR(o.Orders_Date) AS OrderYear,
                    MONTH(o.Orders_Date) AS OrderMonth,
                    SUM(o.Orders_Bill) AS MonthlyRevenue
                FROM Orders o
                WHERE o.Orders_Status != 'Отменен' AND o.Orders_Date BETWEEN @StartDate AND @EndDate
                GROUP BY YEAR(o.Orders_Date), MONTH(o.Orders_Date)
                ORDER BY OrderYear, OrderMonth;";

            using (SqlConnection connection = new SqlConnection(ConfigurationManager.ConnectionStrings["constr"].ConnectionString))
            {
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@StartDate", startDate);
                    command.Parameters.AddWithValue("@EndDate", endDate);
                    try
                    {
                        connection.Open();
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                int year = Convert.ToInt32(reader["OrderYear"]);
                                int month = Convert.ToInt32(reader["OrderMonth"]);
                                labels.Add($"{russianCulture.DateTimeFormat.GetAbbreviatedMonthName(month)} {year}");
                                revenueValues.Add(reader["MonthlyRevenue"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["MonthlyRevenue"]));
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка загрузки трендов выручки: {ex.Message}", "Ошибка БД", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            RevenueTrendSeries.Add(new LineSeries { Title = "Ежемесячная выручка", Values = revenueValues });
            RevenueTrendChartXAxis.Labels = labels.ToArray();
        }

        private void LoadTopDishesData(DateTime startDate, DateTime endDate, int topN)
        {
            TopDishesSeries.Clear();
            // !!! УТОЧНИТЕ Menu_Type для блюд и напитков !!!
            string query = $@"
                SELECT TOP {topN}
                    m.Menu_Name AS ItemName,
                    SUM(od.Quantity) AS TotalQuantitySold
                FROM Order_Dishes od
                JOIN Menu m ON od.Menu_ID = m.Menu_ID
                JOIN Orders o ON od.Orders_ID = o.Orders_ID 
                WHERE m.Menu_Type NOT IN ('Напиток', 'Drink') AND o.Orders_Date BETWEEN @StartDate AND @EndDate 
                GROUP BY m.Menu_Name
                ORDER BY TotalQuantitySold DESC;";

            using (SqlConnection connection = new SqlConnection(ConfigurationManager.ConnectionStrings["constr"].ConnectionString))
            {
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@StartDate", startDate);
                    command.Parameters.AddWithValue("@EndDate", endDate);
                    try
                    {
                        connection.Open();
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                TopDishesSeries.Add(new PieSeries
                                {
                                    Title = reader["ItemName"].ToString(),
                                    Values = new ChartValues<int> { Convert.ToInt32(reader["TotalQuantitySold"]) },
                                    DataLabels = true
                                });
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка загрузки топ блюд: {ex.Message}", "Ошибка БД", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        private void LoadTopDrinksData(DateTime startDate, DateTime endDate, int topN)
        {
            TopDrinksSeries.Clear();
            // !!! УТОЧНИТЕ Menu_Type для блюд и напитков !!!
            string query = $@"
                SELECT TOP {topN}
                    m.Menu_Name AS ItemName,
                    SUM(od.Quantity) AS TotalQuantitySold
                FROM Order_Dishes od
                JOIN Menu m ON od.Menu_ID = m.Menu_ID
                JOIN Orders o ON od.Orders_ID = o.Orders_ID
                WHERE m.Menu_Type IN ('Напиток', 'Drink') AND o.Orders_Date BETWEEN @StartDate AND @EndDate
                GROUP BY m.Menu_Name
                ORDER BY TotalQuantitySold DESC;";

            using (SqlConnection connection = new SqlConnection(ConfigurationManager.ConnectionStrings["constr"].ConnectionString))
            {
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@StartDate", startDate);
                    command.Parameters.AddWithValue("@EndDate", endDate);
                    try
                    {
                        connection.Open();
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                TopDrinksSeries.Add(new PieSeries
                                {
                                    Title = reader["ItemName"].ToString(),
                                    Values = new ChartValues<int> { Convert.ToInt32(reader["TotalQuantitySold"]) },
                                    DataLabels = true
                                });
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка загрузки топ напитков: {ex.Message}", "Ошибка БД", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        private void LoadKpiData(DateTime startDate, DateTime endDate)
        {
            int totalOrders = 0;
            int canceledOrders = 0;
            decimal totalRevenue = 0;

            // !!! ЗАМЕНИТЕ 'Отменен' НА ВАШ СТАТУС ОТМЕНЕННОГО ЗАКАЗА В БД !!!
            string query = @"
                SELECT
                    COUNT(o.Orders_ID) AS KpiTotalOrders,
                    SUM(CASE WHEN o.Orders_Status = 'Отменен' THEN 1 ELSE 0 END) AS KpiCanceledOrders,
                    SUM(CASE WHEN o.Orders_Status != 'Отменен' THEN o.Orders_Bill ELSE 0 END) AS KpiTotalRevenue
                FROM Orders o
                WHERE o.Orders_Date BETWEEN @StartDate AND @EndDate;";

            using (SqlConnection connection = new SqlConnection(ConfigurationManager.ConnectionStrings["constr"].ConnectionString))
            {
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@StartDate", startDate);
                    command.Parameters.AddWithValue("@EndDate", endDate);
                    try
                    {
                        connection.Open();
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                totalOrders = reader["KpiTotalOrders"] == DBNull.Value ? 0 : Convert.ToInt32(reader["KpiTotalOrders"]);
                                canceledOrders = reader["KpiCanceledOrders"] == DBNull.Value ? 0 : Convert.ToInt32(reader["KpiCanceledOrders"]);
                                totalRevenue = reader["KpiTotalRevenue"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["KpiTotalRevenue"]);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка загрузки KPI: {ex.Message}", "Ошибка БД", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            KpiTotalOrdersText.Text = totalOrders.ToString();
            KpiCanceledOrdersText.Text = canceledOrders.ToString();
            KpiTotalRevenueText.Text = totalRevenue.ToString("C", russianCulture);
        }

        // --- ЭКСПОРТ В PDF ---
        // Добавьте кнопку в XAML: <Button Content="Экспорт в PDF" Click="ExportToPdfButton_Click" Margin="5"/>
        private void ExportToPdfButton_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.SaveFileDialog saveFileDialog = new Microsoft.Win32.SaveFileDialog();
            saveFileDialog.Filter = "PDF Document (*.pdf)|*.pdf";
            saveFileDialog.FileName = $"Отчет_По_Заказам_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";

            if (saveFileDialog.ShowDialog() == true)
            {
                try
                {
                    // Передаем корневой Grid дашборда для рендеринга
                    ExportDashboardToPdf((FrameworkElement)this.Content, saveFileDialog.FileName);
                    MessageBox.Show($"Отчет сохранен в {saveFileDialog.FileName}", "Экспорт завершен", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка экспорта в PDF: {ex.Message}", "Ошибка экспорта", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        public void ExportDashboardToPdf(FrameworkElement dashboardControl, string filePath)
        {
            // Убедимся, что контрол отрисован для корректных размеров
            //dashboardControl.UpdateLayout();

            //double actualWidth = dashboardControl.ActualWidth;
            //double actualHeight = dashboardControl.ActualHeight;

            //if (actualWidth == 0 || actualHeight == 0) // Если размеры все еще 0 (например, окно свернуто)
            //{
            //    // Попытка измерить, если контрол не на экране
            //    dashboardControl.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            //    dashboardControl.Arrange(new Rect(dashboardControl.DesiredSize));
            //    actualHeight = dashboardControl.DesiredSize.Height;
            //    actualWidth = dashboardControl.DesiredSize.Width;
            //    if (actualHeight == 0 || actualWidth == 0)
            //    { // Если все еще 0, задать дефолтные
            //        actualWidth = 1180; actualHeight = 780; // Примерные размеры из XAML минус отступы
            //    }
            //}

            //RenderTargetBitmap rtb = new RenderTargetBitmap((int)actualWidth, (int)actualHeight, 96d, 96d, PixelFormats.Pbgra32);
            //rtb.Render(dashboardControl);

            //PngBitmapEncoder pngEncoder = new PngBitmapEncoder();
            //pngEncoder.Frames.Add(BitmapFrame.Create(rtb));

            //using (MemoryStream ms = new MemoryStream())
            //{
            //    pngEncoder.Save(ms);
            //    ms.Position = 0;

            //    PdfDocument document = new PdfDocument();
            //    document.Info.Title = "Отчет по Заказам";
            //    PdfPage page = document.AddPage();
            //    page.Orientation = PdfSharp.PageOrientation.Landscape;
            //    XGraphics gfx = XGraphics.FromPdfPage(page);
            //    XImage image = XImage.FromStream(ms);

            //    double pageWidth = page.Width.Point;
            //    double pageHeight = page.Height.Point;
            //    double imageWidth = image.PixelWidth;
            //    double imageHeight = image.PixelHeight;

            //    double ratioX = pageWidth / imageWidth;
            //    double ratioY = pageHeight / imageHeight;
            //    double ratio = Math.Min(ratioX, ratioY);

            //    double newWidth = imageWidth * ratio * 0.95; // Небольшой отступ 
            //    double newHeight = imageHeight * ratio * 0.95;

            //    double x = (pageWidth - newWidth) / 2;
            //    double y = (pageHeight - newHeight) / 2;

            //    gfx.DrawImage(image, x, y, newWidth, newHeight);
            //    document.Save(filePath);
            //}
        }
    }   
}

