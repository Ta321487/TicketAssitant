using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace TA_WPF.Converters
{
    public class MoneyUnitPositionConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                // 提取金额和参数
                string moneyValue = values[0] as string;
                string paramString = values[1] as string;

                if (string.IsNullOrEmpty(moneyValue) || string.IsNullOrEmpty(paramString))
                {
                    return new Thickness(0);
                }

                // 清理金额字符串 - 去掉前缀符号、空格和千位分隔符
                string cleanValue = moneyValue.Replace("¥", "").Replace(" ", "").Replace(",", "").Trim();

                // 获取整数和小数部分
                string[] numberParts = cleanValue.Split('.');
                string integerPart = numberParts[0];
                string decimalPart = numberParts.Length > 1 ? numberParts[1] : "";

                // 计算整数部分的宽度（每个数字约11像素宽）
                // 120.0 = 3个数字 + 小数点 + 1个小数
                double digitWidth = 11; // 数字宽度调整
                double integerWidth = integerPart.Length * digitWidth;

                // 如果有小数部分，添加小数点和小数的宽度
                double decimalWidth = 0;
                if (decimalPart.Length > 0)
                {
                    decimalWidth = 4 + (decimalPart.Length * 8); // 小数点4像素，每个小数8像素
                }

                // 解析参数字符串
                string[] parts = paramString.Split(',');
                double baseX = 74; // 默认起始X坐标 - MoneyValueMargin的X值
                double baseY = 185; // 默认起始Y坐标 - MoneyValueMargin的Y值

                // 如果参数提供了自定义坐标，则使用参数中的值
                if (parts.Length >= 1 && double.TryParse(parts[0], out double customX))
                {
                    baseX = customX;
                }

                if (parts.Length >= 2 && double.TryParse(parts[1], out double customY))
                {
                    baseY = customY;
                }

                // 计算"元"字的最终X坐标
                double finalX = baseX + integerWidth + decimalWidth + 45; // 增加间距从2改为45像素

                // "元"字的Y坐标与金额值相同，确保在同一水平线上
                double finalY = baseY;

                // 微调Y值，使元字更好地对齐
                finalY += 10; // 稍微下移元字，使它与数字更好地对齐

                // 返回最终位置
                return new Thickness(finalX, finalY, 0, 0);
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