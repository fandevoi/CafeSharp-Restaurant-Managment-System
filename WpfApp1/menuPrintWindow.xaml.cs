using LiveCharts;
using LiveCharts.Defaults;
using LiveCharts.Wpf;
using PdfSharpCore.Drawing;
using PdfSharpCore.Pdf;
using QRCoder;
using SixLabors;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;


namespace WpfApp1
{
    public class MenuItemReport
    {
        public int MenuId { get; set; }
        public string DishName { get; set; }
        public string DishType { get; set; }
        public decimal Price { get; set; }
        public int? Discount { get; set; }
        public bool IsActive { get; set; }
        public string IsActiveText => IsActive ? "Да" : "Нет"; 
        public int OrdersCount { get; set; } 
    }
    public partial class menuPrintWindow : Window
    {
        public menuPrintWindow()
        {
            InitializeComponent();
            StartDatePicker.SelectedDate = DateTime.Today.AddMonths(-1);
            EndDatePicker.SelectedDate = DateTime.Today;

            GenerateReport(); 

        }
        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            GenerateReport(); 
        }
       
        private void GenerateReport()
        {
            List<MenuItemReport> reportData = new List<MenuItemReport>();

            DateTime? startDate = StartDatePicker.SelectedDate;
            DateTime? endDate = EndDatePicker.SelectedDate;

            if (!startDate.HasValue || !endDate.HasValue || startDate.Value > endDate.Value)
            {
                MessageBox.Show("Пожалуйста, выберите корректный диапазон дат.", "Ошибка ввода дат", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            reportDateTimeTextBlock.Text = $"Отчёт сформирован: {DateTime.Now:dd.MM.yyyy HH:mm:ss} (за период с {startDate.Value:dd.MM.yyyy} по {endDate.Value:dd.MM.yyyy})";

            try
            {
                using (SqlConnection connection = new SqlConnection(ConfigurationManager.ConnectionStrings["constr"].ConnectionString))
                {
                    connection.Open();

                    string query = @"
                        SELECT
                            m.Menu_ID,
                            m.Menu_Name AS Dish_Name,
                            m.Menu_Type AS Dish_Type,
                            m.Menu_Price AS Price,
                            m.Menu_Discount AS Discount,
                            m.Menu_IsStop AS Is_Active, -- Присваиваем Menu_IsStop в Is_Active (инвертируем логику в C#)
                            ISNULL(SUM(CASE WHEN o.Orders_Date >= @StartDate AND o.Orders_Date <= @EndDate THEN od.Quantity ELSE 0 END), 0) AS OrdersCount
                        FROM
                            Menu AS m
                        LEFT JOIN
                            Order_Dishes AS od ON m.Menu_ID = od.Menu_ID
                        LEFT JOIN
                            Orders AS o ON od.Orders_ID = o.Orders_ID
                        GROUP BY
                            m.Menu_ID, m.Menu_Name, m.Menu_Type, m.Menu_Price, m.Menu_Discount, m.Menu_IsStop
                        ORDER BY
                            OrdersCount DESC, m.Menu_Name ASC;";

                    SqlCommand command = new SqlCommand(query, connection);
                    command.Parameters.AddWithValue("@StartDate", startDate.Value.Date);
                    command.Parameters.AddWithValue("@EndDate", endDate.Value.Date);

                    SqlDataReader reader = command.ExecuteReader();

                    while (reader.Read())
                    {
                        reportData.Add(new MenuItemReport
                        {
                            MenuId = Convert.ToInt32(reader["Menu_ID"]), 
                            DishName = reader["Dish_Name"].ToString(),
                            DishType = reader["Dish_Type"].ToString(),
                            Price = reader["Price"] != DBNull.Value ? Convert.ToDecimal(reader["Price"]) : 0.00m,
                            Discount = reader["Discount"] != DBNull.Value ? (int?)Convert.ToInt32(reader["Discount"]) : null,
                            IsActive = reader["Is_Active"] != DBNull.Value ? Convert.ToBoolean(reader["Is_Active"]) : false, 
                            OrdersCount = Convert.ToInt32(reader["OrdersCount"]) 
                        });
                    }
                    reader.Close();
                }

                reportDataGrid.ItemsSource = null;
                reportDataGrid.ItemsSource = reportData;

                UpdateCharts(reportData);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при генерации отчёта: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateCharts(List<MenuItemReport> data)
        {
            if (popularityChart.Series == null || !popularityChart.Series.Any())
            {
                MessageBox.Show("Ошибка: Серия для графика популярности не инициализирована в XAML.", "Ошибка графика", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var top10Dishes = data.OrderByDescending(d => d.OrdersCount).Take(10).ToList();
            var mainColumnSeriesPopularity = (ColumnSeries)popularityChart.Series[0];

            if (top10Dishes.Any())
            {
                mainColumnSeriesPopularity.Values = new ChartValues<int>(top10Dishes.Select(d => d.OrdersCount));
                popularityChartAxisX.Labels = top10Dishes.Select(d => d.DishName).ToList();
            }
            else
            {
                mainColumnSeriesPopularity.Values = new ChartValues<int>();
                popularityChartAxisX.Labels = null;
            }

            if (typesCartesianChart.Series == null || !typesCartesianChart.Series.Any())
            {
                MessageBox.Show("Ошибка: Серия для графика типов не инициализирована в XAML.", "Ошибка графика", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var typesDistribution = data
                .GroupBy(d => d.DishType)
                .Select(g => new
                {
                    DishType = g.Key,
                    TotalOrders = g.Sum(d => d.OrdersCount)
                })
                .Where(x => x.TotalOrders > 0)
                .OrderByDescending(x => x.TotalOrders)
                .ToList();

            var mainColumnSeriesTypes = (ColumnSeries)typesCartesianChart.Series[0]; 

            if (typesDistribution.Any())
            {
                mainColumnSeriesTypes.Values = new ChartValues<int>(typesDistribution.Select(t => t.TotalOrders)); 
                typesAxisX.Labels = typesDistribution.Select(t => t.DishType).ToList();
            }
            else
            {
                mainColumnSeriesTypes.Values = new ChartValues<int>(); 
                typesAxisX.Labels = null;
            }
        }
        MemoryStream _cachedImageStream;
        private void ExportPdfButton_Click(object sender, RoutedEventArgs e)
        {
            MainGrid.RowDefinitions[2].Height = new GridLength(0);
            reportDataGrid.VerticalScrollBarVisibility = ScrollBarVisibility.Hidden;

            Microsoft.Win32.SaveFileDialog saveFileDialog = new Microsoft.Win32.SaveFileDialog
            {
                FileName = $"Отчет_по_меню_{DateTime.Now:yyyyMMdd_HHmmss}.pdf",
                DefaultExt = ".pdf",
                Filter = "PDF документы (.pdf)|*.pdf"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                try
                {
                    if (this.ActualWidth == 0 || this.ActualHeight == 0)
                    {
                        MessageBox.Show("Окно не полностью отрисовано. Попробуйте еще раз.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    // Шаг 1: сделать скриншот окна
                    RenderTargetBitmap rtb = new RenderTargetBitmap(
                        (int)this.ActualWidth,
                        (int)this.ActualHeight,
                        96d, 96d,
                        PixelFormats.Pbgra32);
                    rtb.Render(this);

                    // Преобразуем в GDI Bitmap (через MemoryStream -> Bitmap)
                    BitmapEncoder encoder = new PngBitmapEncoder();
                    encoder.Frames.Add(BitmapFrame.Create(rtb));

                    using (MemoryStream ms = new MemoryStream())
                    {
                        encoder.Save(ms);
                        using (Bitmap bitmap = new Bitmap(ms))
                        {
                            using (PdfDocument pdf = new PdfDocument())
                            {
                                PdfPage page = pdf.AddPage();
                                page.Orientation = (PdfSharpCore.PageOrientation)PdfSharp.PageOrientation.Portrait;

                                XGraphics gfx = XGraphics.FromPdfPage(page);

                                // Рассчитываем размеры
                                double pageWidth = page.Width;
                                double pageHeight = page.Height;

                                double imgWidth = bitmap.Width;
                                double imgHeight = bitmap.Height;

                                double ratioX = pageWidth / imgWidth;
                                double ratioY = pageHeight / imgHeight;
                                double ratio = Math.Min(ratioX, ratioY);

                                double scaledWidth = imgWidth * ratio;
                                double scaledHeight = imgHeight * ratio;
                             
                                using (MemoryStream imgStream2 = new MemoryStream())
                                {
                                    bitmap.Save(imgStream2, ImageFormat.Png);
                                    _cachedImageStream.Position = 0;

                                    XImage xImg = XImage.FromStream(() => imgStream2);
                                    gfx.DrawImage(xImg, (pageWidth - scaledWidth) / 2, (pageHeight - scaledHeight) / 2, scaledWidth, scaledHeight);
                                }

                                pdf.Save(saveFileDialog.FileName);
                            }
                        }
                    }

                    MessageBox.Show($"Отчёт успешно экспортирован в PDF: {saveFileDialog.FileName}", "Экспорт завершён", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при экспорте в PDF: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }

            MainGrid.RowDefinitions[2].Height = GridLength.Auto;
            reportDataGrid.VerticalScrollBarVisibility = ScrollBarVisibility.Visible;
        }


        private void DataGridRow_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
        }
    }
}
