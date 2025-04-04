using System.Globalization;
using System.Windows.Data;

namespace TA_WPF.Converters
{
    /// <summary>
    /// 字体大小增加转换器，用于将基础字体大小增加指定的值
    /// </summary>
    public class FontSizeIncreaseConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double fontSize && parameter is string increment && double.TryParse(increment, out double incrementValue))
            {
                return fontSize + incrementValue;
            }

            // 如果参数未指定，默认增加2
            if (value is double size)
            {
                return size + 2;
            }

            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}