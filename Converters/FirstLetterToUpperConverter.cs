using System.Globalization;
using System.Windows.Data;

namespace TA_WPF.Converters
{
    /// <summary>
    /// 将字符串首字母转换为大写的转换器
    /// </summary>
    public class FirstLetterToUpperConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || !(value is string))
                return value;

            string str = (string)value;
            if (string.IsNullOrEmpty(str))
                return str;

            // 将第一个字符转换为大写，其余保持不变
            if (str.Length == 1)
                return char.ToUpper(str[0]).ToString();

            return char.ToUpper(str[0]) + str.Substring(1);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // 反向转换通常不需要，直接返回原值
            return value;
        }
    }
}