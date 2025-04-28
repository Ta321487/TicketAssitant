using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace TA_WPF.Converters
{
    /// <summary>
    /// 将布尔值转换为颜色的转换器
    /// </summary>
    public class BoolToColorConverter : IValueConverter
    {
        /// <summary>
        /// 将布尔值转换为颜色
        /// </summary>
        /// <param name="value">布尔值</param>
        /// <param name="targetType">目标类型</param>
        /// <param name="parameter">转换参数</param>
        /// <param name="culture">区域信息</param>
        /// <returns>如果为true则返回绿色，如果为false则返回红色，如果为null则返回橙色(表示检测中)</returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
            {
                // null值表示检测中状态
                return new SolidColorBrush(Colors.Orange);
            }
            
            if (value is bool boolValue)
            {
                return boolValue ? new SolidColorBrush(Colors.Green) : new SolidColorBrush(Colors.Red);
            }
            
            // 默认返回红色
            return new SolidColorBrush(Colors.Red);
        }

        /// <summary>
        /// 将颜色转换为布尔值（不实现）
        /// </summary>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
} 