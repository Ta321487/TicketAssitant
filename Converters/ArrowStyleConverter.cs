using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace TA_WPF.Converters
{
    public class ArrowStyleConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values == null || values.Length < 3 || values[0] == null || values[1] == null || values[2] == null)
                return new Thickness(0, 160, 0, 0);

            string departStationName = values[0].ToString();
            string arriveStationName = values[1].ToString();
            string trainNo = values[2].ToString();

            // 基础位置
            double top = 100; // 垂直位置
            double horizontalPosition = 0; // 水平位置将根据车次号位置计算

            // 计算出发车站"站"字的位置
            double departStationEndPosition = 0;
            switch (departStationName.Length)
            {
                case 1:
                    departStationEndPosition = 87;
                    break;
                case 2:
                    departStationEndPosition = 175;
                    break;
                case 3:
                    departStationEndPosition = 210;
                    break;
                case 4:
                    departStationEndPosition = 245;
                    break;
                case 5:
                    departStationEndPosition = 260;
                    break;
                default:
                    departStationEndPosition = 40 + (35 * departStationName.Length);
                    break;
            }

            // 计算到达车站第一个字的位置
            double arriveStationStartPosition = 530;

            // 计算中心点位置
            double centerPosition = (departStationEndPosition + arriveStationStartPosition) / 2;

            // 计算车次号的宽度（假设每个字符约30个单位宽度）
            double trainNoWidth = trainNo.Length * 30;

            // 计算最终位置，使箭头居中于车次号下方
            horizontalPosition = centerPosition - (trainNoWidth / 2); // 根据车次号宽度调整居中

            return new Thickness(horizontalPosition, top, 0, 0);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}