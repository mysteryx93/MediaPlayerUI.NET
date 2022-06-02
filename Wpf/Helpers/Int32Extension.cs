using System;
using System.Windows.Markup;

namespace HanumanInstitute.MediaPlayer.Wpf;

/// <summary>
/// Allows binding int value for command parameters using syntax {ui:Int32 5}
/// </summary>
public sealed class Int32Extension : MarkupExtension
{
    /// <summary>
    /// Initializes a new instance of the Int32Extension class.
    /// </summary>
    /// <param name="value">An integer value.</param>
    public Int32Extension(int value) { Value = value; }
    /// <summary>
    /// The value contained in the extension.
    /// </summary>
    public int Value { get; set; }
    /// <summary>
    /// Returns the value.
    /// </summary>
    public override object ProvideValue(IServiceProvider sp) { return Value; }
};
