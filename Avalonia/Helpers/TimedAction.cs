using System;
using Avalonia.Threading;

namespace HanumanInstitute.MediaPlayer.Avalonia.Helpers;

/// <summary>
/// Executes an action no more than once per specified minimum interval.
/// </summary>
public class TimedAction<T>
{
    /// <summary>
    /// Initializes a new instance of the TimedAction class.
    /// </summary>
    /// <param name="minInterval">The minimum interval between action invocations.</param>
    /// <param name="action">The action to invoke.</param>
    /// <param name="priority">The thread priority.</param>
    public TimedAction(TimeSpan minInterval, Action<T> action, DispatcherPriority? priority = null)
    {
        priority ??= DispatcherPriority.Normal;
        MinInterval = minInterval;
        Action = action;
        _timer = new DispatcherTimer(minInterval, priority.Value, Timer_Elapsed);
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

    /// <summary>
    /// The minimum interval between action invocations.
    /// </summary>
    public TimeSpan MinInterval { get; set; }
    /// <summary>
    /// The action to invoke.
    /// </summary>
    public Action<T> Action { get; private set; }
    private readonly DispatcherTimer _timer;
    private bool _hasWaitingAction;
    private T _waitingParam = default!;

    /// <summary>
    /// Invokes the action if it hasn't been invoked within the minimum interval.
    /// </summary>
    /// <param name="param">A parameter to send to the action.</param>
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
