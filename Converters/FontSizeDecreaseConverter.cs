using System.Globalization;
using System.Windows.Data;

namespace TA_WPF.Converters
{
    /// <summary>
    /// 字体大小减小转换器，将给定的字体大小减小一定比例或固定值
    /// </summary>
    public class FontSizeDecreaseConverter : IValueConverter
    {
        /// <summary>
        /// 将源字体大小减小
        /// </summary>
        /// <param name="value">源字体大小</param>
        /// <param name="targetType">目标类型</param>
        /// <param name="parameter">参数，可以是减小的固定值或比例，默认减小2</param>
        /// <param name="culture">区域信息</param>
        /// <returns>减小后的字体大小</returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double fontSize)
            {
                // 默认减小2
                double decreaseAmount = 2;

                // 如果提供了参数，尝试将其解析为减小值
                if (parameter != null)
                {
                    if (double.TryParse(parameter.ToString(), out double paramValue))
                    {
                        decreaseAmount = paramValue;
                    }
                }

                // 确保字体大小不小于最小可读值
                return Math.Max(10, fontSize - decreaseAmount);
            }

            return value;
        }

        /// <summary>
        /// 将目标字体大小增大（反向转换）
        /// </summary>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double fontSize)
            {
                // 默认增加2
                double increaseAmount = 2;

                // 如果提供了参数，尝试将其解析为增加值
                if (parameter != null)
                {
                    if (double.TryParse(parameter.ToString(), out double paramValue))
                    {
                        increaseAmount = paramValue;
                    }
                }

                return fontSize + increaseAmount;
            }

            return value;
        }
    }
}