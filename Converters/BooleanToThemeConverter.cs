using System.Globalization;
using System.Windows.Data;
using MaterialDesignThemes.Wpf;

namespace TA_WPF.Converters
{
    /// <summary>
    /// 将布尔值转换为MaterialDesign主题的转换器
    /// </summary>
    public class BooleanToThemeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isDarkMode)
            {
                return isDarkMode ? BaseTheme.Dark : BaseTheme.Light;
            }
            return BaseTheme.Light; // 默认返回浅色主题
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is BaseTheme theme)
            {
                return theme == BaseTheme.Dark;
            }
            return false; // 默认返回浅色主题（false）
        }
    }
} 