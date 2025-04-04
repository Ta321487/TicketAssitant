using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace TA_WPF.Converters
{
    /// <summary>
    /// 将整数值与参数比较，相等时返回Visible，否则返回Collapsed
    /// </summary>
    public class IntEqualityToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || parameter == null)
                return Visibility.Collapsed;

            // 尝试将value和parameter转换为int进行比较
            if (int.TryParse(value.ToString(), out int intValue) &&
                int.TryParse(parameter.ToString(), out int compareValue))
            {
                return intValue == compareValue ? Visibility.Visible : Visibility.Collapsed;
            }

            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}