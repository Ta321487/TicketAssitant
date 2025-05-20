using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace TA_WPF.Converters
{
    public class StationWordPositionConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                // 提取车站名称和参数
                string stationName = values[0] as string;
                string paramString = values[1] as string;

                if (string.IsNullOrEmpty(stationName) || string.IsNullOrEmpty(paramString))
                {
                    return new Thickness(0);
                }

                // 解析参数 - 格式为 "baseX,baseY,unused1,unused2"
                string[] parts = paramString.Split(',');
                if (parts.Length != 4)
                {
                    return new Thickness(0);
                }

                double baseX = double.Parse(parts[0]);
                double baseY = double.Parse(parts[1]);
                double unused1 = double.Parse(parts[2]); // 用于判断是出发车站还是到达车站

                // 车站名称长度
                int length = stationName.Length;

                // 判断是出发车站还是到达车站
                bool isDepartStation = unused1 > 400; // 出发车站的第三个参数通常大于400

                // 根据站名字数和位置调整"站"字水平位置
                double adjustedX = baseX;

                if (isDepartStation)
                {
                    // 出发车站调整 - 根据实际图片中的位置
                    switch (length)
                    {
                        case 1: // 单字站名 - 参考"宋站"的图片
                            adjustedX = 137;
                            break;
                        case 2: // 双字站名
                            adjustedX = 200;
                            break;
                        case 3: // 三字站名
                            adjustedX = 240;
                            break;
                        case 4: // 四字站名 - 参考"五大连池站"的图片
                            adjustedX = 290;
                            break;
                        case 5: // 五字站名 - 参考"香港西九龙站"的图片
                            adjustedX = 305;
                            break;
                        default: // 其他情况
                            adjustedX = 120 + (length * 40);
                            break;
                    }
                }
                else
                {
                    // 到达车站调整 - 根据实际图片中的位置
                    switch (length)
                    {
                        case 1: // 单字站名 - 参考"宋站"的图片
                            adjustedX = 597;
                            break;
                        case 2: // 双字站名
                            adjustedX = 660;
                            break;
                        case 3: // 三字站名
                            adjustedX = 700;
                            break;
                        case 4: // 四字站名 - 参考"五大连池站"的图片
                            adjustedX = 750;
                            break;
                        case 5: // 五字站名 - 参考"香港西九龙站"的图片
                            adjustedX = 765;
                            break;
                        default: // 其他情况
                            adjustedX = 580 + (length * 45);
                            break;
                    }
                }

                // 返回调整后的位置，使用原始Y坐标
                return new Thickness(adjustedX, baseY, 0, 0);
            }
            catch
            {
                return new Thickness(0);
            }
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}