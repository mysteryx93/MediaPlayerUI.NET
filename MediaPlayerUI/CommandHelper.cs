using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Input;

namespace EmergenceGuardian.MediaPlayerUI {
    public static class CommandHelper {
        public static ICommand InitCommand(ref ICommand cmd, Action execute, Func<bool> canExecute) {
            if (cmd == null)
                cmd = new DelegateCommand(execute, canExecute);
            return cmd;
        }
    }
}
