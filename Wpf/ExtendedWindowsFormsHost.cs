using System;
using System.Windows.Forms;
using System.Windows.Forms.Integration;
using System.Windows.Input;

namespace HanumanInstitute.MediaPlayer.Wpf;

/// <summary>
/// A WindowsFormHost extension that allows intercepting mouse events.
/// </summary>
public class ExtendedWindowsFormsHost : WindowsFormsHost
{
    /// <summary>
    /// Initializes a new instance of the ExtendedWindowsFormsHost class.
    /// </summary>
    public ExtendedWindowsFormsHost()
    {
        ChildChanged += OnChildChanged;
    }

    private void OnChildChanged(object? sender, ChildChangedEventArgs childChangedEventArgs)
    {
        if (childChangedEventArgs.PreviousChild is Control previousChild)
        {
            previousChild.MouseDown -= OnMouseDown;
            previousChild.MouseWheel -= OnMouseWheel;
        }
        if (Child != null)
        {
            Child.MouseDown += OnMouseDown;
            Child.MouseWheel += OnMouseWheel;
        }
    }

    private void OnMouseWheel(object sender, System.Windows.Forms.MouseEventArgs e)
    {
        RaiseEvent(new MouseWheelEventArgs(Mouse.PrimaryDevice, (int)DateTime.Now.Ticks, e.Delta)
        {
            RoutedEvent = Mouse.MouseWheelEvent,
            Source = this
        });
    }

    private void OnMouseDown(object sender, System.Windows.Forms.MouseEventArgs e)
    {
        var wpfButton = ConvertToWpf(e.Button);
        if (!wpfButton.HasValue)
        {
            return;
        }

        // Double-clicks aren't propagating.
        //if (e.Clicks == 2) {
        //    RaiseEvent(new MouseButtonEventArgs(Mouse.PrimaryDevice, (int)DateTime.Now.Ticks, wpfButton.Value) {
        //        RoutedEvent = System.Windows.Controls.Control.MouseDoubleClickEvent,
        //        Source = this
        //    });
        //} else {
        RaiseEvent(new MouseButtonEventArgs(Mouse.PrimaryDevice, (int)DateTime.Now.Ticks, wpfButton.Value)
        {
            RoutedEvent = Mouse.MouseDownEvent,
            Source = this
        });
        //}
    }

    private static MouseButton? ConvertToWpf(MouseButtons winFormButton)
    {
        return winFormButton switch
        {
            MouseButtons.Left => MouseButton.Left,
            MouseButtons.None => null,
            MouseButtons.Right => MouseButton.Right,
            MouseButtons.Middle => MouseButton.Middle,
            MouseButtons.XButton1 => MouseButton.XButton1,
            MouseButtons.XButton2 => MouseButton.XButton2,
            _ => throw new ArgumentOutOfRangeException(nameof(winFormButton))
        };
    }
}
