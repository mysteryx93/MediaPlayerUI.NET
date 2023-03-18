using Avalonia.Interactivity;

namespace HanumanInstitute.MediaPlayer.Avalonia.Helpers;

/// <summary>
/// Holds a typed value as an event argument.
/// </summary>
/// <typeparam name="T">The type of value to store.</typeparam>
public class ValueEventArgs<T> : RoutedEventArgs
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
    public ValueEventArgs(RoutedEvent routedEvent, T value) : base(routedEvent)
    {
        Value = value;
    }
}
