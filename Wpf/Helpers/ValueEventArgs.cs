using System;

namespace HanumanInstitute.MediaPlayer.Wpf;

/// <summary>
/// Holds a typed value as an event argument.
/// </summary>
/// <typeparam name="T">The type of value to store.</typeparam>
public class ValueEventArgs<T> : EventArgs
{
    /// <summary>
    /// The event value.
    /// </summary>
    public T Value { get; set; }

    //public ValueEventArgs()
    //{ }

    /// <summary>
    /// Initializes a new instance of ValueEventArgs with specified value.
    /// </summary>
    /// <param name="value">The event value.</param>
    public ValueEventArgs(T value)
    {
        Value = value;
    }
}
