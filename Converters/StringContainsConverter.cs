using System.Globalization;
using System.Windows.Data;

namespace TA_WPF.Converters
{
    /// <summary>
    /// 检查字符串是否包含指定子字符串的转换器
    /// </summary>
    public class StringContainsConverter : IValueConverter
    {
        /// <summary>
        /// 将字符串值转换为布尔值，检查是否包含参数指定的子字符串
        /// </summary>
        /// <param name="value">要检查的字符串</param>
        /// <param name="targetType">目标类型</param>
        /// <param name="parameter">要搜索的子字符串</param>
        /// <param name="culture">区域信息</param>
        /// <returns>如果字符串包含指定的子字符串，则返回true；否则返回false</returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || parameter == null)
                return false;

            string stringValue = value.ToString();
            string searchString = parameter.ToString();

            return stringValue.Contains(searchString);
        }

        /// <summary>
        /// 反向转换（未实现）
        /// </summary>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
} 