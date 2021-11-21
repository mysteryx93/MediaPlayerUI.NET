using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace HanumanInstitute.MediaPlayer.Avalonia
{
    /// <summary>
    /// Converts a TimeSpan into seconds, and optionally adds specified amount of seconds.
    /// </summary>
    public class TimeSpanToDoubleConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var valueAdd = System.Convert.ToDouble(parameter, CultureInfo.InvariantCulture);
            return ((TimeSpan)value).TotalSeconds + valueAdd;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var valueAdd = System.Convert.ToDouble(parameter, CultureInfo.InvariantCulture);
            return TimeSpan.FromSeconds((double)value - valueAdd);
        }
    }
}
