using System;
using System.Globalization;
using System.Linq;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows;
using System.Text.RegularExpressions;

namespace TA_WPF.Converters
{
    public class ArrowPathConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values == null || values.Length < 3 || values[0] == null || values[1] == null || values[2] == null)
                return null;

            double trainNoWidth = System.Convert.ToDouble(values[0]);
            string trainNo = values[1].ToString();
            double centerPosition = System.Convert.ToDouble(values[2]);
            
            // 计算数字位数，用于动态调整箭头长度
            int digitCount = Regex.Matches(trainNo, @"\d").Count;
            
            // 箭头长度调整系数
            double widthFactor;
            
            // 根据数字位数决定长度
            switch (digitCount)
            {
                case 1:
                    widthFactor = 1.2; // 一位数字，长度稍微加长20%
                    break;
                case 2:
                    widthFactor = 1.1; // 两位数字，长度加长10% 
                    break;
                case 3:
                    widthFactor = 1.05; // 三位数字，长度加长5%
                    break;
                case 4:
                case 5:
                    widthFactor = 1.0; // 四位及以上数字，保持原长度
                    break;
                default:
                    widthFactor = 1.0;
                    break;
            }
            
            // 计算扩展后的宽度
            double extendedWidth = trainNoWidth * widthFactor;
            
            // 创建箭头路径
            PathGeometry geometry = new PathGeometry();
            
            // 创建主线段
            PathFigure lineFigure = new PathFigure();
            lineFigure.StartPoint = new Point(0, 5);
            lineFigure.Segments.Add(new LineSegment(new Point(extendedWidth, 5), true));
            geometry.Figures.Add(lineFigure);
            
            // 创建箭头头部，只保留下半部分
            double headStart = Math.Max(0, extendedWidth - 20);
            PathFigure headFigure = new PathFigure();
            headFigure.StartPoint = new Point(extendedWidth, 5);
            headFigure.Segments.Add(new LineSegment(new Point(headStart, 9), true));
            geometry.Figures.Add(headFigure);
            
            return geometry;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
} 