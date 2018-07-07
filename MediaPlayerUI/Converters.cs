using System;
using System.Globalization;
using System.Windows.Data;

namespace EmergenceGuardian.MediaPlayerUI {
    /// <summary>
    /// Converts a TimeSpan into seconds, and optionally adds specified amount of seconds.
    /// </summary>
    [ValueConversion(typeof(TimeSpan), typeof(double))]
    public class TimeSpanToDoubleConverter : IValueConverter {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            double ValueAdd = System.Convert.ToDouble(parameter);
            return ((TimeSpan)value).TotalSeconds + ValueAdd;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            double ValueAdd = System.Convert.ToDouble(parameter);
            return TimeSpan.FromSeconds((double)value - ValueAdd);
        }
    }
}
