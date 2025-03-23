using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace TA_WPF.Converters
{
    public class StationWordPositionConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values == null || values.Length < 2 || values[0] == null || values[1] == null)
                return new Thickness(0, 0, 0, 0);

            string stationName = values[0].ToString();
            string defaultMargin = values[1].ToString();

            // 解析默认的Margin值
            string[] parts = defaultMargin.Split(',');
            if (parts.Length != 4)
                return new Thickness(0, 0, 0, 0);

            double left = double.Parse(parts[0]);
            double top = double.Parse(parts[1]);
            double right = double.Parse(parts[2]);
            double bottom = double.Parse(parts[3]);

            // 获取站名字数
            int length = stationName.Length;

            // 出发站(左侧大站名)
            if (left < 600)
            {
                // 调整垂直位置使站字位于站名底部
                top = 116;

                // 根据字数精确调整水平位置，使"站"字紧贴在最后一个字后面
                switch (length)
                {
                    case 1:
                        left = 126;  // 单字站名
                        break;
                    case 2:
                        left = 210; // 双字站名 
                        break;
                    case 3:
                        left = 235; // 三字站名
                        break;
                    case 4:
                        left = 270; // 四字站名
                        break;
                    case 5:
                        left = 295; // 五字站名
                        break;
                    default:
                        left = 20 + (35 * length); // 其他情况
                        break;
                }
            }
            // 到达站(右侧小站名)
            else
            {
                // 调整垂直位置使站字位于站名底部
                top = 116;

                // 根据字数精确调整水平位置，使"站"字紧贴在最后一个字后面
                switch (length)
                {
                    case 1:
                        left = 560;  // 单字站名
                        break;
                    case 2:
                        left = 665; // 双字站名
                        break;
                    case 3:
                        left = 705; // 三字站名
                        break;
                    case 4:
                        left = 735; // 四字站名
                        break;
                    case 5:
                        left = 765; // 五字站名 (需要更紧凑的间距)
                        break;
                    default:
                        left = 525 + (30 * length); // 其他情况
                        break;
                }
            }

            // 返回的right和bottom值设置为0，这样不会影响TextBlock的显示区域
            return new Thickness(left, top, 0, 0);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}