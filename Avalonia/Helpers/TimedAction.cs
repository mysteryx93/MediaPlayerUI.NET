using System;
using Avalonia.Threading;

namespace HanumanInstitute.MediaPlayer.Avalonia.Helpers;

/// <summary>
/// Executes an action no more than once per specified minimum interval.
/// </summary>
public class TimedAction<T>
{
    public TimedAction(TimeSpan minInterval, Action<T> action, DispatcherPriority priority = DispatcherPriority.Normal)
    {
        MinInterval = minInterval;
        Action = action;
        _timer = new DispatcherTimer(minInterval, priority, Timer_Elapsed);
    }
    private void Timer_Elapsed(object? sender, EventArgs e)
    {
        _timer.Stop();
        if (_hasWaitingAction)
        {
            _hasWaitingAction = false;
            Action(_waitingParam);
        }
    }

    public TimeSpan MinInterval { get; set; }
    public Action<T> Action { get; private set; }
    private readonly DispatcherTimer _timer;
    private bool _hasWaitingAction;
    private T _waitingParam = default!;

    public void ExecuteAtInterval(T param)
    {
        if (!_timer.IsEnabled)
        {
            Action.Invoke(param);
            _timer.Start();
        }
        else
        {
            _hasWaitingAction = true;
            _waitingParam = param;
        }
    }
}