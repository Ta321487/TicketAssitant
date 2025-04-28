using System;
using System.Globalization;
using System.Windows.Data;

namespace TA_WPF.Converters
{
    /// <summary>
    /// 将布尔值转换为按钮启用状态的转换器
    /// </summary>
    public class BoolToEnabledConverter : IMultiValueConverter
    {
        /// <summary>
        /// 将多个布尔值转换为按钮启用状态
        /// </summary>
        /// <param name="values">值数组，第一个是环境就绪状态，第二个是加载状态</param>
        /// <param name="targetType">目标类型</param>
        /// <param name="parameter">转换参数</param>
        /// <param name="culture">区域信息</param>
        /// <returns>按钮是否应该启用</returns>
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values == null || values.Length < 2)
                return false;

            // 第一个值是环境就绪状态
            bool isEnvironmentReady = values[0] is bool ready && ready;
            
            // 第二个值是加载状态
            bool isLoading = values[1] is bool loading && loading;

            // 环境就绪且不在加载中时启用按钮
            return isEnvironmentReady && !isLoading;
        }

        /// <summary>
        /// 将按钮启用状态转换为布尔值数组（不实现）
        /// </summary>
        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
} 