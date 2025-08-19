using LiveCharts;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Net.Mail;
using System.Net;
using System.Security;
using System.Security.Cryptography;
using System.Security.Policy;
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
    public static class WatermarkService
    {
        public static readonly DependencyProperty WatermarkProperty =
            DependencyProperty.RegisterAttached("Watermark",
                                                typeof(string),
                                                typeof(WatermarkService),
                                                new UIPropertyMetadata(string.Empty, OnWatermarkChanged));

        public static string GetWatermark(DependencyObject obj)
        {
            return (string)obj.GetValue(WatermarkProperty);
        }

        public static void SetWatermark(DependencyObject obj, string value)
        {
            obj.SetValue(WatermarkProperty, value);
        }

        private static void OnWatermarkChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is TextBox textBox)
            {
                if (string.IsNullOrEmpty(textBox.Text))
                {
                    textBox.Text = GetWatermark(textBox);
                }

                textBox.GotFocus += (sender, args) =>
                {
                    if (textBox.Text == GetWatermark(textBox))
                    {
                        textBox.Text = string.Empty;
                    }
                };

                textBox.LostFocus += (sender, args) =>
                {
                    if (string.IsNullOrEmpty(textBox.Text))
                    {
                        textBox.Text = GetWatermark(textBox);
                    }
                };
            }
        }
    }
  
    public partial class loginPage : Window
    {
        public loginPage()
        {
            InitializeComponent();
        }
        private IntPtr tmpPss {get;set;}
        private protected static byte[] сomputeHash(string input)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                return sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
            }
        }
        private protected static bool verifyPassword(string username, string password)
        {
            using (SqlConnection connection = new SqlConnection(ConfigurationManager.ConnectionStrings["constr"].ConnectionString))
            {
                try
                {
                    connection.Open();
                    SqlCommand cmd = new SqlCommand($"SELECT Login_password FROM Login WHERE Login_Username = '{username}'", connection);
                    byte[] passwordHashFromDatabase = (byte[])cmd.ExecuteScalar();
                    if (passwordHashFromDatabase == null)
                    {
                        return false;
                    }
                    byte[] inputPasswordHash = сomputeHash(password);
                    return StructuralComparisons.StructuralEqualityComparer.Equals(passwordHashFromDatabase, inputPasswordHash);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error: " + ex.Message);
                }
                finally
                {
                    connection.Close();
                }
                return false;
            }
        }
        private static SecureString generatePassword()
        {
            Random random = new Random();
            int length = 6;
            const string chars = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            SecureString password = new SecureString();
            for (int i = 0; i < length; i++)
            {
                if (i % 2 == 0)
                {
                    password.AppendChar((char)('0' + random.Next(10)));
                }
                else
                {
                    password.AppendChar(chars[random.Next(chars.Length)]);
                }
            }
            return password;
        }
        
        private protected void sendMessage(string userEmail)
        {
            string smtpServer = "smtp.mail.ru"; 
            int smtpPort = 587; 
            string smtpUsername = "no-reply-cafesharp@mail.ru";
            string smtpPassword = "AdER6iaxDh29dXf1dgi1";

            using (SmtpClient smtpClient = new SmtpClient(smtpServer, smtpPort))
            {
                smtpClient.Credentials = new NetworkCredential(smtpUsername, smtpPassword);
                smtpClient.EnableSsl = true;

                using (MailMessage mailMessage = new MailMessage())
                {
                    mailMessage.From = new MailAddress(smtpUsername);
                    mailMessage.To.Add(userEmail);
                    mailMessage.Subject = "Восстановление пароля Claude Monet";
                    tmpPss = System.Runtime.InteropServices.Marshal.SecureStringToBSTR(generatePassword());
                    string outBSTR = System.Runtime.InteropServices.Marshal.PtrToStringBSTR(tmpPss);
                    mailMessage.Body = $"Ваш код пароль: {outBSTR} ";

                    try
                    {
                        smtpClient.Send(mailMessage);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Ошибка отправки сообщения: {ex.Message}");
                    }
                }
            }
        }
        private async void loginButton_Click(object sender, RoutedEventArgs e)
        {
            if (loginTextBox.Text == "Введите логин")
            {
                loginTextBox.Background = new SolidColorBrush(Color.FromRgb(239, 142, 142));
                await Task.Delay(1000);
                loginTextBox.Background = Brushes.White;
            }
            else if (passwordTextBox.Text == "Какойтотекст")
            {
                passwordTextBox.Background = new SolidColorBrush(Color.FromRgb(239, 142, 142));
                await Task.Delay(1000);
                passwordTextBox.Background = Brushes.White;
            }
            if (loginTextBox.Text != "Введите логин"  && passwordTextBox.Text != "Какойтотекст")
            {
                bool passwordMatch = verifyPassword(loginTextBox.Text, passwordTextBox.Text);
                if (passwordMatch)
                {
                    MainWindow secondWindow = new MainWindow();
                    Application.Current.MainWindow = secondWindow;
                    secondWindow.Show();
                    this.Close();
                }
                else
                {
                    MessageBox.Show("Пользователь не найден", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
        private async void forgetButton_Click(object sender, RoutedEventArgs e)
        {
           
            if (loginTextBox.Text == "Введите логин")
            {
                loginTextBox.Background = new SolidColorBrush(Color.FromRgb(239, 142, 142));
                await Task.Delay(1000);
                loginTextBox.Background = Brushes.White;
            }
            if (loginTextBox.Text != "Введите логин")
            {
                using (SqlConnection connection = new SqlConnection(ConfigurationManager.ConnectionStrings["constr"].ConnectionString))
                {
                    try
                    {
                        connection.Open();
                        SqlCommand cmd = new SqlCommand($"SELECT Login_email FROM Login WHERE Login_username = '{loginTextBox.Text}'", connection);
                        if (cmd.ExecuteScalar()==null)
                        {
                            MessageBox.Show("Пользователь не найден", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                        else
                        {
                            string email = cmd.ExecuteScalar().ToString();
                            sendMessage(email);
                            int atIndex = email.IndexOf('@');
                            if (atIndex != -1)
                            {
                                int halfLength = (atIndex + 1) / 2; 
                                string censoredEmail = email.Substring(0, halfLength).PadRight(atIndex, '*') + email.Substring(halfLength); 
                                Console.WriteLine(censoredEmail);
                                MessageBox.Show($"На вашу электронную почту {censoredEmail} будет отправленно письмо с временным код - паролем.", "Восстановление пароля", MessageBoxButton.OK, MessageBoxImage.Information);
                                mainStackPanel.Visibility = Visibility.Collapsed;
                                forgetSpareStackPanel.Visibility = Visibility.Collapsed;
                                forgetStackPanel.Visibility = Visibility.Visible;
                            }
                            else
                            {
                                MessageBox.Show($"На вашу электронную почту {email} будет отправленно письмо с временным код - паролем.", "Восстановление пароля", MessageBoxButton.OK, MessageBoxImage.Information);
                                mainStackPanel.Visibility = Visibility.Collapsed;
                                forgetSpareStackPanel.Visibility = Visibility.Collapsed;
                                forgetStackPanel.Visibility = Visibility.Visible;
                            }
                           
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
        }

        private void backButton_Click(object sender, RoutedEventArgs e)
        {
            forgetSpareStackPanel.Visibility = Visibility.Collapsed;
            mainStackPanel.Visibility = Visibility.Visible;
            forgetStackPanel.Visibility = Visibility.Collapsed;
        }
        private void passwordTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if ((Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.C))
            {
                e.Handled = true;
            }
            if ((Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.X))
            {
                e.Handled = true;
            }
        }
        private void passwordTextBox_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed && e.ClickCount == 2)
            {
                e.Handled = true;
            }
        }
        private void passwordTextBox_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
        }

        private async void recoveryButton_Click(object sender, RoutedEventArgs e)
        {
            string outBSTR = System.Runtime.InteropServices.Marshal.PtrToStringBSTR(tmpPss);
            if (recoveryTextBox.Text=="Введите код из письма")
            {
                recoveryTextBox.Background = new SolidColorBrush(Color.FromRgb(239, 142, 142));
                await Task.Delay(1000);
                recoveryTextBox.Background = Brushes.White;
            }
            else if (recoveryTextBox.Text!=outBSTR)
            {
                MessageBox.Show("Неверный код", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            if (recoveryTextBox.Text == outBSTR)
            {
                MainWindow secondWindow = new MainWindow();
                Application.Current.MainWindow = secondWindow;
                secondWindow.Show();
                this.Close();
            }
            
        }

        private  void noCodeButton_Click(object sender, RoutedEventArgs e)
        {
            recoveryTextBox2.Visibility = Visibility.Visible;
            recoveryTextBox.Visibility = Visibility.Collapsed;
            noCodeButton.Visibility = Visibility.Hidden;
            recoveryButton.Visibility = Visibility.Collapsed;
            recoveryButton2.Visibility = Visibility.Visible;
        }

        private async void recoveryButton2_Click(object sender, RoutedEventArgs e)
        {
            using (SqlConnection connection = new SqlConnection(ConfigurationManager.ConnectionStrings["constr"].ConnectionString))
            {
                try
                {
                    connection.Open();
                    SqlCommand cmd = new SqlCommand($"SELECT Login_forgot_code FROM Login WHERE Login_login = '{loginTextBox.Text}'", connection);
                    if (cmd.ExecuteScalar() == null)
                    {
                        MessageBox.Show("Пользователь не найден", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                    else
                    {
                        string code = cmd.ExecuteScalar().ToString();
                        if (recoveryTextBox2.Text == code)
                        {
                            MainWindow secondWindow = new MainWindow();
                            Application.Current.MainWindow = secondWindow;
                            secondWindow.Show();
                            this.Close();
                        }
                        else if (recoveryTextBox.Text != code)
                        {
                            MessageBox.Show("Неверный код", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                        if (recoveryTextBox2.Text == "Введите резервный код")
                        {
                            recoveryTextBox2.Background = new SolidColorBrush(Color.FromRgb(239, 142, 142));
                            await Task.Delay(1000);
                            recoveryTextBox2.Background = Brushes.White;
                        }
                        

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
    }
}
