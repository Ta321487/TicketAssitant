using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace TA_WPF.Converters
{
    public class MoneyUnitPositionConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values == null || values.Length < 2 || values[0] == null || values[1] == null)
                return new Thickness(0, 0, 0, 0);

            string moneyValue = values[0].ToString();
            string defaultMargin = values[1].ToString();

            // 解析默认的Margin值
            string[] parts = defaultMargin.Split(',');
            if (parts.Length != 4)
                return new Thickness(0, 0, 0, 0);

            double left = double.Parse(parts[0]);
            double top = double.Parse(parts[1]);
            double right = double.Parse(parts[2]);
            double bottom = double.Parse(parts[3]);

            // 保存原始的基础位置
            double baseLeft = left;
            double baseTop = top;

            // 解析金额数值，得到整数和小数部分，以更精确计算宽度
            // 首先分离掉逗号和空格等格式化字符
            string cleanValue = moneyValue.Replace(",", "").Replace(" ", "");

            // 然后尝试分离整数和小数部分
            string[] numberParts = cleanValue.Split('.');
            int integerLength = numberParts[0].Length;  // 整数部分长度
            int decimalLength = numberParts.Length > 1 ? numberParts[1].Length : 0; // 小数部分长度

            // 计算不同部分的宽度（根据图片中的情况进行微调）
            double digitWidth = 10.0;  // 单个数字的宽度
            double integerWidth = integerLength * digitWidth;  // 整数部分宽度
            double decimalWidth = decimalLength > 0 ? 3.0 + (decimalLength * digitWidth * 0.8) : 0; // 小数点宽度较小

            // 计算整体宽度
            double totalWidth = integerWidth + decimalWidth;

            // 设置元字的位置，使其紧跟在数字后面
            // 这里不再使用硬编码的120，而是使用传入的基础位置作为起点
            left = baseLeft + totalWidth - 45; // 微调以匹配实际显示效果

            // 垂直位置使用传入的top值作为基础
            top = baseTop;

            // 特殊情况处理
            if (cleanValue == "0.0")
            {
                left = baseLeft - 45; // 对于0.0这种特殊情况单独处理
            }
            else if (integerLength == 1 && decimalLength <= 1)
            {
                left = left - 2; // 对于个位数加小数点后一位的情况微调
            }

            return new Thickness(left, top, right, bottom);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}