
namespace HanumanInstitute.MediaPlayer.Avalonia.Mpv;

/// <summary>
/// Represents the status of the player control.
/// </summary>
public enum PlaybackStatus
{
    /// <summary>
    /// No file is playing.
    /// </summary>
    Stopped = 0,
    /// <summary>
    /// Loading media file.
    /// </summary>
    Loading = 1,
    /// <summary>
    /// Playing media file.
    /// </summary>
    Playing = 2,
    /// <summary>
    /// An error occured while loading media file.
    /// </summary>
    Error = 3
}
