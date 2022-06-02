using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace HanumanInstitute.MediaPlayer.Avalonia;

/// <summary>
/// Converts a TimeSpan into seconds, and optionally adds specified amount of seconds.
/// </summary>
public class TimeSpanToDoubleConverter : IValueConverter
{
    /// <inheritdoc />
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var valueAdd = System.Convert.ToDouble(parameter, CultureInfo.InvariantCulture);
        return ((TimeSpan?) value)?.TotalSeconds + valueAdd ?? 0.0;
    }

    /// <inheritdoc />
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var valueAdd = System.Convert.ToDouble(parameter, CultureInfo.InvariantCulture);
        return value != null ? TimeSpan.FromSeconds((double)value - valueAdd) : TimeSpan.Zero;
    }
}
