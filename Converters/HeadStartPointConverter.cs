using System;
using System.Globalization;
using System.Windows.Data;

namespace TA_WPF.Converters
{
    public class HeadStartPointConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return 0;

            double width = System.Convert.ToDouble(value);
            
            // 计算箭头头部起始位置，从末端往回20单位
            return Math.Max(0, width - 20);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
} 