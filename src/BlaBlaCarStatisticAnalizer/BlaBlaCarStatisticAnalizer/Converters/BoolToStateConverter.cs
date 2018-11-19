using System;
using System.Globalization;
using System.Windows.Data;

namespace BlaBlaCarStatisticAnalizer.Converters
{
    public class BoolToStateConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) return 0;
            var t = (bool) value;
            return t ? 1 : 0;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
