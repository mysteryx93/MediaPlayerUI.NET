using System;

namespace HanumanInstitute.MediaPlayer.Avalonia.Bass;

/// <summary>
/// Manages the initialization of the BASS output device, ensuring it is initialized once.
/// </summary>
public static class BassDevice
{
    static BassDevice()
    {
        if (!ManagedBass.Bass.Init(-1))
        {
            throw new InvalidOperationException("Failed to initialize BASS audio output.");
        }
    }

    /// <summary>
    /// When called the first time, initialize the BASS device from the static constructor.
    /// </summary>
    public static void Init() {}
}
