using System;
using System.Windows.Input;

namespace EmergenceGuardian.MediaPlayerUI.Mvvm {
    public static class CommandHelper {
        public static ICommand InitCommand(ref ICommand cmd, Action execute, Func<bool> canExecute) {
            if (cmd == null)
                cmd = new RelayCommand(execute, canExecute);
            return cmd;
        }

        public static ICommand InitCommand<T>(ref ICommand cmd, Action<T> execute, Predicate<T> canExecute) {
            if (cmd == null)
                cmd = new RelayCommand<T>(execute, canExecute);
            return cmd;
        }
    }
}
