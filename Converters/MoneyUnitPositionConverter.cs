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

                // 基础X坐标（金额数字的起始位置）
                double baseX = 104;
                
                // 计算整数部分的宽度（每个数字约11像素宽）
                double integerWidth = integerPart.Length * 11;
                
                // 如果有小数部分，添加小数点和小数的宽度
                double decimalWidth = 0;
                if (decimalPart.Length > 0)
                {
                    decimalWidth = 4 + (decimalPart.Length * 8); // 小数点4像素，每个小数8像素
                }
                
                // 计算"元"字的最终X坐标
                double finalX = baseX + integerWidth + decimalWidth + 2; // 加2像素的间距
                
                // 返回最终位置
                return new Thickness(finalX, 190, 0, 0);
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