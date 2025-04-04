using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace TA_WPF.Converters
{
    /// <summary>
    /// 用于计算金额单位"元"字位置的转换器
    /// </summary>
    public class MoneyValueToMarginConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            // 处理金额元字位置
            if (values.Length >= 2 && values[0] is double width && values[1] is string)
            {
                // 根据金额字符串的宽度调整元字的位置
                return new Thickness(74 + width, 267, 0, 0);
            }

            // 默认位置
            return new Thickness(122, 267, 0, 0);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}