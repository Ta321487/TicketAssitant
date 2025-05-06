using System;
using System.Globalization;
using System.Windows.Data;
using TA_WPF.Utils;

namespace TA_WPF.Converters
{
    /// <summary>
    /// 车站等级转换器
    /// </summary>
    public class StationLevelConverter : IMultiValueConverter
    {
        /// <summary>
        /// 将车站等级整数值转换为对应的文本显示
        /// </summary>
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values == null || values.Length == 0 || values[0] == null)
                return string.Empty;
            
            if (values[0] is int stationLevel)
            {
                return StationLevelHelper.GetStationLevelText(stationLevel);
            }
            
            return string.Empty;
        }

        /// <summary>
        /// 将车站等级文本转换回整数值（不实现）
        /// </summary>
        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
} 