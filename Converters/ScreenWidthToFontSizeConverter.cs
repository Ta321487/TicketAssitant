using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace TA_WPF.Converters
{
    /// <summary>
    /// 屏幕宽度到字体大小的转换器，用于根据屏幕宽度和用户字体大小设置来计算字体大小
    /// </summary>
    public class ScreenWidthToFontSizeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // 获取用户设置的字体大小
            double baseFontSize = 16; // 默认字体大小
            if (Application.Current.Resources.Contains("MaterialDesignFontSize"))
            {
                if (Application.Current.Resources["MaterialDesignFontSize"] is double fontSize)
                {
                    baseFontSize = fontSize;
                }
            }

            // 获取屏幕宽度
            double screenWidth = 1920; // 默认屏幕宽度
            if (value is double width)
            {
                screenWidth = width;
            }

            // 获取比例参数
            double scaleFactor = 0.02; // 默认比例因子
            if (parameter is string paramStr && double.TryParse(paramStr, out double factor))
            {
                scaleFactor = factor;
            }

            // 计算字体大小：基础字体大小 + (屏幕宽度 * 比例因子)
            // 这样可以确保字体大小随着用户设置和屏幕大小而变化
            return baseFontSize + (screenWidth * scaleFactor);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}