using System;

namespace HanumanInstitute.MediaPlayer.Avalonia.Bass
{
    public static class Extensions
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
                ThrowBassError();
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
                ThrowBassError();
            }
        }

        private static void ThrowBassError()
        {
            var err = ManagedBass.Bass.LastError.ToString();
            var msg = $"BASS threw an error of type {err}. " + 
                      "You can view details about that error at https://managedbass.github.io/api/ManagedBass.Errors.html";
            throw new InvalidOperationException(msg);
        }
    }
}
