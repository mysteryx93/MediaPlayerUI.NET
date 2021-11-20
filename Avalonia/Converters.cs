using System;
using System.Globalization;

namespace HanumanInstitute.MediaPlayer.Avalonia
{
    /// <summary>
    /// Converts a TimeSpan into seconds, and optionally adds specified amount of seconds.
    /// </summary>
    [ValueConversion(typeof(TimeSpan), typeof(double))]
    public class TimeSpanToDoubleConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var ValueAdd = System.Convert.ToDouble(parameter, CultureInfo.InvariantCulture);
            return ((TimeSpan)value).TotalSeconds + ValueAdd;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var ValueAdd = System.Convert.ToDouble(parameter, CultureInfo.InvariantCulture);
            return TimeSpan.FromSeconds((double)value - ValueAdd);
        }
    }
}
