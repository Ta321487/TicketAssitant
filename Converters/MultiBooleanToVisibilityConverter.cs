using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace TA_WPF.Converters
{
    /// <summary>
    /// 将多个布尔值转换为可见性的转换器
    /// 当所有布尔值都为true时，返回Visible，否则返回Collapsed
    /// </summary>
    public class MultiBooleanToVisibilityConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            // 检查是否所有值都是布尔值且都为true
            if (values == null || values.Length == 0)
                return Visibility.Collapsed;

            // 检查是否有任何值为null或不是布尔值
            foreach (var value in values)
            {
                if (value == null || !(value is bool))
                    return Visibility.Collapsed;
            }

            // 如果所有值都为true，则返回Visible，否则返回Collapsed
            bool allTrue = values.Cast<bool>().All(b => b);
            return allTrue ? Visibility.Visible : Visibility.Collapsed;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
} 