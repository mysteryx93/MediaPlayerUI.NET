using System;
using System.Globalization;
using System.Windows.Data;

namespace HanumanInstitute.MediaPlayer.Wpf;

/// <summary>
/// Converts a TimeSpan into seconds, and optionally adds specified amount of seconds.
/// </summary>
[ValueConversion(typeof(TimeSpan), typeof(double))]
public class TimeSpanToDoubleConverter : IValueConverter
{
    /// <inheritdoc />
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var valueAdd = System.Convert.ToDouble(parameter, CultureInfo.InvariantCulture);
        return ((TimeSpan)value).TotalSeconds + valueAdd;
    }

    /// <inheritdoc />
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var valueAdd = System.Convert.ToDouble(parameter, CultureInfo.InvariantCulture);
        return TimeSpan.FromSeconds((double)value - valueAdd);
    }
}
