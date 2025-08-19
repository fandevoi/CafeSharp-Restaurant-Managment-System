using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows;
using System.Windows.Markup;
using System.Windows.Media.Media3D;

namespace WpfApp1.Services.Converters
{
    abstract class ConverterBase : MarkupExtension, IValueConverter
    {
        public abstract object Convert(object value, Type targetType, object parameter, CultureInfo culture);
        public virtual object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
        public override object ProvideValue(IServiceProvider serviceProvider) => this;
    }
    class StringToBrushConverter : ConverterBase
    {
        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value?.ToString() == "Подан")
            {
                return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFFFFF"));
            }
            else if (value?.ToString() == "Отменён")
            {
                return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#D87C7C"));
            }
            else
            {
                return DependencyProperty.UnsetValue;
            }
        }
    }
    class StatusToBrushConverter : ConverterBase
    {
        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value?.ToString() == "Активна")
            {
                return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFFFFF"));
            }
            else if (value?.ToString() == "Закрыта")
            {
                return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#D87C7C"));
            }
            else
            {
                return DependencyProperty.UnsetValue;
            }
        }
    }
    class DoubleToVisibilityConverter : ConverterBase
    {
        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int d)
            {
                return d > 0 ? Visibility.Visible : Visibility.Collapsed;
            }
            return DependencyProperty.UnsetValue;
        }
    }
    class WindowSizeToFontSizeConverter : ConverterBase
    {
        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return 0;

            if (parameter == null)
                parameter = 1;

            double number;
            double coefficient;

            if (double.TryParse(value.ToString(), out number) && double.TryParse(parameter.ToString(), out coefficient))
            {
                double fontSize = number * 0.05 - coefficient*1.1;
                return fontSize;
            }
            return null;
        }
    }
    
}


