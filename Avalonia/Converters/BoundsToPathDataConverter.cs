using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace HanumanInstitute.MediaPlayer.Avalonia;

/// <summary>
/// Creates Path segments for the volume control.
/// </summary>
public class BoundsToPathDataConverter : IMultiValueConverter
{
    /// <inheritdoc />
    public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values[0] is null || values[0] is UnsetValueType) { return null; }
        if (values[1] is null || values[1] is UnsetValueType) { return null; }
        var segment = parameter != null ? System.Convert.ToInt32(parameter) : 0;

        var bounds = (Rect)values[0]!;
        var val = System.Convert.ToDouble(values[1]) / 100;
        var height = bounds.Height;
        var width = bounds.Width;

        var path = segment switch
        {
            0 => new PathSegments() // Decrease button
            {
                Point(0, height),
                Point(width * val, height),
                Point(width * val, height * .9 * (1 - val)),
                Point(0, height * .9),
            },
            1 => new PathSegments() // Increase button, right-aligned
            {
                Point(0, height * .9),
                Point(0, height),
                Point(width - 1, height),
                Point(width - 1, 0),
            },
            _ => null
        };
        return new PathGeometry()
        {
            Figures = new PathFigures()
            {
                new PathFigure()
                {
                    StartPoint = ((LineSegment)path![0]).Point,
                    Segments = path
                }
            }
        };
    }

    private LineSegment Point(double x, double y) => new LineSegment() { Point = new Point(x, y) };
}
