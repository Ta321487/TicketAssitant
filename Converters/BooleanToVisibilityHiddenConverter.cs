using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace TA_WPF.Converters
{
    /// <summary>
    /// 将布尔值转换为Visibility，true为Hidden而不是Collapsed
    /// </summary>
    public class BooleanToVisibilityHiddenConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                // 是否反转结果
                bool invert = false;
                if (parameter is string paramString)
                {
                    bool.TryParse(paramString, out invert);
                }

                if (invert)
                {
                    return boolValue ? Visibility.Visible : Visibility.Hidden;
                }
                else
                {
                    return boolValue ? Visibility.Hidden : Visibility.Visible;
                }
            }

            return Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Visibility visibility)
            {
                // 是否反转结果
                bool invert = false;
                if (parameter is string paramString)
                {
                    bool.TryParse(paramString, out invert);
                }

                if (invert)
                {
                    return visibility == Visibility.Visible;
                }
                else
                {
                    return visibility != Visibility.Visible;
                }
            }

            return false;
        }
    }
}