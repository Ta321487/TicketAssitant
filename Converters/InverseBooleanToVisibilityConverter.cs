using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace TA_WPF.Converters
{
    public class InverseBooleanToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                // 反转布尔值，当value为false时返回Visible
                return !boolValue ? Visibility.Visible : Visibility.Collapsed;
            }
            
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Visibility visibility)
            {
                // 反转可见性，当visibility为Visible时返回false
                return visibility != Visibility.Visible;
            }
            
            return true;
        }
    }
} 