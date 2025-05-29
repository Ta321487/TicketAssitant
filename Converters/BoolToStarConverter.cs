using System;
using System.Globalization;
using System.Windows.Data;

namespace TA_WPF.Converters
{
    /// <summary>
    /// 将布尔值转换为实心五角星或空心五角星字符的转换器
    /// </summary>
    public class BoolToStarConverter : IValueConverter
    {
        /// <summary>
        /// 将bool值转换为五角星字符
        /// </summary>
        /// <param name="value">布尔值</param>
        /// <param name="targetType">目标类型</param>
        /// <param name="parameter">参数</param>
        /// <param name="culture">区域信息</param>
        /// <returns>true返回实心五角星"★"，false返回空心五角星"☆"</returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isFavorite)
            {
                return isFavorite ? "★" : "☆";
            }
            
            return "☆"; // 默认返回空心五角星
        }

        /// <summary>
        /// 反向转换（不实现）
        /// </summary>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
} 