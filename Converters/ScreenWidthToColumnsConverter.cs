using System.Globalization;
using System.Windows.Data;

namespace TA_WPF.Converters
{
    /// <summary>
    /// 屏幕宽度到列数的转换器，用于根据屏幕宽度动态调整列数
    /// </summary>
    public class ScreenWidthToColumnsConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double screenWidth && parameter is string threshold)
            {
                // 尝试解析参数作为临界值
                if (double.TryParse(threshold, out double thresholdValue))
                {
                    // 如果屏幕宽度小于阈值，返回true以触发样式触发器
                    return screenWidth < thresholdValue;
                }
            }
            
            // 默认返回false
            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
} 