using System;
using System.Windows.Input;

namespace HanumanInstitute.MediaPlayerUI.Mvvm
{
    public static class CommandHelper
    {
        public static ICommand InitCommand(ref RelayCommand? cmd, Action execute) => InitCommand(ref cmd, execute, () => true);

        public static ICommand InitCommand(ref RelayCommand? cmd, Action execute, Func<bool> canExecute)
        {
            return cmd ?? (cmd = new RelayCommand(execute, canExecute));
        }

        public static ICommand InitCommand<T>(ref RelayCommand<T>? cmd, Action<T> execute) => InitCommand(ref cmd, execute, (t) => true);

        public static ICommand InitCommand<T>(ref RelayCommand<T>? cmd, Action<T> execute, Predicate<T> canExecute)
        {
            return cmd ?? (cmd = new RelayCommand<T>(execute, canExecute));
        }
    }
}
