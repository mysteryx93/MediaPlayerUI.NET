using System;
using System.Collections.Generic;

namespace HanumanInstitute.MediaPlayer.Avalonia.Bass;

/// <summary>
/// Provides functions related to mathematical fractions.
/// </summary>
public static class Fraction
{
    /// <summary>
    /// Rounds a value to the nearest fraction, with specified maximum rounding accuracy error. 
    /// </summary>
    /// <param name="value">The value to round.</param>
    /// <param name="accuracy">The maximum accuracy error acceptable in percentage, between 0 and 1.
    /// Lower number gives more precise fraction. Higher number gives simpler fraction.</param>
    /// <returns>The rounded value.</returns>
    public static double RoundToFraction(double value, double accuracy = 0.005)
    {
        var frac = GetFraction(value, accuracy);
        var result = (double)frac.Key / frac.Value;
        return result;
    }
    
    /// <summary>
    /// Rounds a value to the nearest fraction, with specified maximum rounding accuracy error. 
    /// </summary>
    /// <param name="value">The value to round.</param>
    /// <param name="accuracy">The maximum accuracy error acceptable in percentage, between 0 and 1.
    /// Lower number gives more precise fraction. Higher number gives simpler fraction.</param>
    /// <param name="roundingError">The difference between the rounded value and the original value.</param>
    /// <returns>The rounded value.</returns>
    public static double RoundToFraction(double value, double accuracy, out double roundingError)
    {
        var frac = GetFraction(value, accuracy);
        var result = (double)frac.Key / frac.Value;
        roundingError = result - value;
        return result;
    }
    
    /// <summary>
    /// Returns a fraction that closely represents specified value, with specified maximum accuracy error. 
    /// </summary>
    /// <param name="value">The value to convert to a fraction.</param>
    /// <param name="accuracy">The maximum accuracy error acceptable in percentage, between 0 and 1.
    /// Lower number gives more precise fraction. Higher number gives simpler fraction.</param>
    /// <returns>A KeyValuePair of Numerator/Denominator.</returns>
    public static KeyValuePair<int, int> GetFraction(double value, double accuracy = 0.005)
    {
        // Richards algorithm
        // https://stackoverflow.com/a/42085412/3960200
        if (accuracy <= 0.0 || accuracy >= 1.0)
        {
            throw new ArgumentOutOfRangeException(nameof(accuracy), "Must be > 0 and < 1.");
        }

        var sign = Math.Sign(value);

        if (sign == -1)
        {
            value = Math.Abs(value);
        }

        // Accuracy is the maximum relative error; convert to absolute maxError
        var maxError = sign == 0 ? accuracy : value * accuracy;

        var n = (int) Math.Floor(value);
        value -= n;

        if (value < maxError)
        {
            return new KeyValuePair<int, int>(sign * n, 1);
        }
        
        if (1 - maxError < value)
        {
            return new KeyValuePair<int, int>(sign * (n + 1), 1);
        }

        var z = value;
        var previousDenominator = 0;
        var denominator = 1;
        int numerator;

        do
        {
            z = 1.0 / (z - (int)z);
            var temp = denominator;
            denominator = denominator * (int) z + previousDenominator;
            previousDenominator = temp;
            numerator = Convert.ToInt32(value * denominator);
        }
        while (Math.Abs(value - (double) numerator / denominator) > maxError && z != (int)z);

        return new KeyValuePair<int, int>((n * denominator + numerator) * sign, denominator);
    }
}
