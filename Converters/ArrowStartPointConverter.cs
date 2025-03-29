using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace TA_WPF.Converters
{
    public class ArrowStartPointConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return 0;

            // 获取箭头总宽度
            double width = System.Convert.ToDouble(value);
            
            // 计算箭头头部的起始点（从箭头总宽度减去20个单位）
            double arrowHeadPosition = width - 20;
            
            // 如果目标类型是Point，则创建Point对象
            if (targetType == typeof(Point) && parameter != null)
            {
                double y = System.Convert.ToDouble(parameter);
                return new Point(arrowHeadPosition, y);
            }
            
            // 否则返回X坐标
            return arrowHeadPosition;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
} 