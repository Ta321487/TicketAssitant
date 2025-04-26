using System.Globalization;
using System.Windows.Data;
using TA_WPF.Utils;

namespace TA_WPF.Converters
{
    public class StationNameConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string stationName)
            {
                return StationNameHelper.RemoveStationSuffix(stationName);
            }
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value;
        }
    }
}