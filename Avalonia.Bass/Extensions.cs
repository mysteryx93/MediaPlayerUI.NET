using System;
using ManagedBass;

namespace HanumanInstitute.MediaPlayer.Avalonia.Bass;

/// <summary>
/// Provides extension methods for BASS.
/// </summary>
internal static class Extensions
{
    /// <summary>
    /// Checks whether a BASS handle is valid and throws an exception if it is 0.
    /// </summary>
    /// <param name="handle">The BASS handle to validate.</param>
    /// <returns>The same value.</returns>
    /// <exception cref="InvalidOperationException">BASS handle is null</exception>
    internal static int Valid(this int handle)
    {
        if (handle == 0)
        {
            throw new BassException(ManagedBass.Bass.LastError);
        }

        return handle;
    }

    /// <summary>
    /// Checks whether specified is true and throws an exception if false.
    /// </summary>
    /// <param name="value">The value to validate.</param>
    /// <exception cref="InvalidOperationException">Value is false.</exception>
    internal static void Valid(this bool value)
    {
        if (!value)
        {
            throw new BassException(ManagedBass.Bass.LastError);
        }
    }
}
