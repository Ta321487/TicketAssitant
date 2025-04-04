using System.Globalization;
using System.Windows.Data;

namespace TA_WPF.Converters
{
    /// <summary>
    /// 将布尔值转换为字符串的转换器
    /// </summary>
    public class BooleanToStringConverter : IValueConverter
    {
        /// <summary>
        /// 将布尔值转换为字符串
        /// </summary>
        /// <param name="value">布尔值</param>
        /// <param name="targetType">目标类型</param>
        /// <param name="parameter">转换参数，格式为"trueValue|falseValue"</param>
        /// <param name="culture">区域信息</param>
        /// <returns>如果为true则返回trueValue，否则返回falseValue</returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(parameter is string parameterString))
                return value;

            var parts = parameterString.Split('|');
            if (parts.Length != 2)
                return value;

            if (value is bool boolValue)
            {
                return boolValue ? parts[0] : parts[1];
            }

            return value;
        }

        /// <summary>
        /// 将字符串转换为布尔值
        /// </summary>
        /// <param name="value">字符串值</param>
        /// <param name="targetType">目标类型</param>
        /// <param name="parameter">转换参数，格式为"trueValue|falseValue"</param>
        /// <param name="culture">区域信息</param>
        /// <returns>如果字符串等于trueValue则返回true，否则返回false</returns>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(parameter is string parameterString) || !(value is string valueString))
                return false;

            var parts = parameterString.Split('|');
            if (parts.Length != 2)
                return false;

            return valueString == parts[0];
        }
    }
}