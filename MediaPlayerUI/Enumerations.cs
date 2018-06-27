using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EmergenceGuardian.MediaPlayerUI {
    /// <summary>
    /// Specifies how time is displayed within the player.
    /// </summary>
    public enum TimeDisplayStyles {
        None,
        Standard,
        Seconds
    }

    /// <summary>
    /// Specifies which mouse click triggers an action.
    /// </summary>
    public enum MouseTrigger {
        None,
        LeftClick,
        MiddleClick,
        RightClick
        //LeftDoubleClick, // doesn't work properly
        //MiddleDoubleClick,
        //RightDoubleClick
    }
}
