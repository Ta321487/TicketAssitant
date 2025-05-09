using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace TA_WPF.Converters
{
    /// <summary>
    /// 判断对象是否为非空并转换为可见性的转换器
    /// </summary>
    public class ObjectNotNullConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool isNotNull = false;
            
            if (value is byte[] byteArray)
            {
                isNotNull = byteArray != null && byteArray.Length > 0;
            }
            else
            {
                isNotNull = value != null;
            }
            
            // 如果指定了inverse参数，反转结果
            if (parameter != null && parameter.ToString().ToLower() == "inverse")
            {
                isNotNull = !isNotNull;
            }
            
            return isNotNull ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // 不需要实现从Visibility转回的逻辑
            return null;
        }
    }
} 