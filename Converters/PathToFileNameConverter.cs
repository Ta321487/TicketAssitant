using System.Globalization;
using System.IO;
using System.Windows.Data;

namespace TA_WPF.Converters
{
    /// <summary>
    /// 将完整路径转换为文件名的转换器
    /// </summary>
    public class PathToFileNameConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string path && !string.IsNullOrEmpty(path))
            {
                return Path.GetFileName(path);
            }
            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}