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

            // 使用传入的基础位置，但根据车站名称长度进行微调
            // 使用传入的left和top作为基础位置
            double baseLeft = left;
            double baseTop = top;
            
            // 判断是否是红色车票 - 基于传入的位置参数推断
            bool isRedTicket = baseTop > 115; // 红色车票的top值通常大于115
            
            // 出发站(左侧大站名)
            if (baseLeft < 400)
            {
                // 根据字数精确调整水平位置，使"站"字紧贴在最后一个字后面
                // 但保留传入的参数作为偏移量
                if (isRedTicket)
                {
                    // 红色车票 - 需要更大的偏移量
                    switch (length)
                    {
                        case 1:
                            left = baseLeft + 45;  // 单字站名
                            break;
                        case 2:
                            left = baseLeft + 145; // 双字站名 
                            break;
                        case 3:
                            left = baseLeft + 170; // 三字站名
                            break;
                        case 4:
                            left = baseLeft + 210; // 四字站名 - 增加偏移
                            break;
                        case 5:
                            left = baseLeft + 240; // 五字站名 - 增加偏移
                            break;
                        default:
                            left = baseLeft + (35 * length) + 10; // 其他情况
                            break;
                    }
                }
                else
                {
                    // 蓝色车票 - 使用原来的偏移量但为四字和五字站名微调
                    switch (length)
                    {
                        case 1:
                            left = baseLeft + 40;  // 单字站名
                            break;
                        case 2:
                            left = baseLeft + 140; // 双字站名 
                            break;
                        case 3:
                            left = baseLeft + 165; // 三字站名
                            break;
                        case 4:
                            left = baseLeft + 195; // 四字站名 - 微调
                            break;
                        case 5:
                            left = baseLeft + 225; // 五字站名 - 微调
                            break;
                        default:
                            left = baseLeft + (35 * length); // 其他情况
                            break;
                    }
                }
            }
            // 到达站(右侧小站名)
            else
            {
                // 根据字数精确调整水平位置，使"站"字紧贴在最后一个字后面
                // 但保留传入的参数作为偏移量
                if (isRedTicket)
                {
                    // 红色车票 - 需要更大的偏移量
                    switch (length)
                    {
                        case 1:
                            left = baseLeft + 50;  // 单字站名
                            break;
                        case 2:
                            left = baseLeft + 120; // 双字站名
                            break;
                        case 3:
                            left = baseLeft + 165; // 三字站名
                            break;
                        case 4:
                            left = baseLeft + 200; // 四字站名 - 增加偏移
                            break;
                        case 5:
                            left = baseLeft + 230; // 五字站名 - 增加偏移
                            break;
                        default:
                            left = baseLeft + (30 * length) + 10; // 其他情况
                            break;
                    }
                }
                else
                {
                    // 蓝色车票 - 使用原来的偏移量但为四字和五字站名微调
                    switch (length)
                    {
                        case 1:
                            left = baseLeft + 45;  // 单字站名
                            break;
                        case 2:
                            left = baseLeft + 115; // 双字站名
                            break;
                        case 3:
                            left = baseLeft + 155; // 三字站名
                            break;
                        case 4:
                            left = baseLeft + 190; // 四字站名 - 微调
                            break;
                        case 5:
                            left = baseLeft + 220; // 五字站名 - 微调
                            break;
                        default:
                            left = baseLeft + (30 * length); // 其他情况
                            break;
                    }
                }
            }

            // 保持传入的top值，不做任何调整
            top = baseTop;

            // 返回的right和bottom值保持原样
            return new Thickness(left, top, right, bottom);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}