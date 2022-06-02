namespace HanumanInstitute.MediaPlayer;

/// <summary>
/// Specifies how time is displayed within the player.
/// </summary>
public enum TimeDisplayStyle
{
    /// <summary>
    /// Do not display time.
    /// </summary>
    None,
    /// <summary>
    /// Display 'mm:ss' or 'h:mm:ss'.
    /// </summary>
    Standard,
    /// <summary>
    /// Display the amount of seconds.
    /// </summary>
    Seconds
}

/// <summary>
/// Specifies which mouse click triggers an action.
/// </summary>
public enum MouseTrigger
{
    /// <summary>
    /// Action won't be triggered.
    /// </summary>
    None,
    /// <summary>
    /// Action will be triggered on left mouse click.
    /// </summary>
    LeftClick,
    /// <summary>
    /// Action will be triggered on middle mouse click.
    /// </summary>
    MiddleClick,
    /// <summary>
    /// Action will be triggered on right mouse click.
    /// </summary>
    RightClick
    //LeftDoubleClick, // doesn't work properly
    //MiddleDoubleClick,
    //RightDoubleClick
}
