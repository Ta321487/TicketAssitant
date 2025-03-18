using System.Globalization;
using System.Windows.Data;

namespace TA_WPF.Converters
{
    public class StationNameConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string stationName)
            {
                return stationName.Replace("站", "");
            }
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value;
        }
    }
} 