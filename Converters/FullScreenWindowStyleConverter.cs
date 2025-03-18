using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace TA_WPF.Converters
{
    /// <summary>
    /// 将布尔值转换为WindowStyle的转换器
    /// </summary>
    public class FullScreenWindowStyleConverter : IValueConverter
    {
        /// <summary>
        /// 将布尔值转换为WindowStyle
        /// </summary>
        /// <param name="value">布尔值，表示是否全屏</param>
        /// <param name="targetType">目标类型</param>
        /// <param name="parameter">转换参数</param>
        /// <param name="culture">区域信息</param>
        /// <returns>如果是全屏则返回None（隐藏标题栏），否则返回SingleBorderWindow</returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // 检查是否为全屏模式
            if (value is bool isFullScreen && isFullScreen)
            {
                // 全屏模式下隐藏标题栏
                return WindowStyle.None;
            }
            
            // 非全屏模式下显示标题栏
            return WindowStyle.SingleBorderWindow;
        }

        /// <summary>
        /// 将WindowStyle转换为布尔值
        /// </summary>
        /// <param name="value">WindowStyle值</param>
        /// <param name="targetType">目标类型</param>
        /// <param name="parameter">转换参数</param>
        /// <param name="culture">区域信息</param>
        /// <returns>如果是None则返回true，否则返回false</returns>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is WindowStyle style)
            {
                return style == WindowStyle.None;
            }
            
            return false;
        }
    }
} 