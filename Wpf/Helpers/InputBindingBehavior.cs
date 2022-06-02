using System.Windows;

namespace HanumanInstitute.MediaPlayer.Wpf;
// ReSharper disable once InconsistentNaming

/// <summary>
/// Provides an attached property to move InputBindings from a control to its parent window.
/// </summary>
public static class InputBindingBehavior
{
    /// <summary>
    /// Gets or sets whether to move all InputBindings to the parent Window.
    /// </summary>
    public static readonly DependencyProperty PropagateInputBindingsToWindowProperty =
        DependencyProperty.RegisterAttached("PropagateInputBindingsToWindow", typeof(bool), typeof(InputBindingBehavior),
            new PropertyMetadata(false, OnPropagateInputBindingsToWindowChanged));
    /// <summary>
    /// Gets whether to move all InputBindings to the parent Window.
    /// </summary>
    public static bool GetPropagateInputBindingsToWindow(FrameworkElement d) => (bool)(d.CheckNotNull(nameof(d)).GetValue(PropagateInputBindingsToWindowProperty) ?? false);
    /// <summary>
    /// Sets whether to move all InputBindings to the parent Window.
    /// </summary>
    public static void SetPropagateInputBindingsToWindow(FrameworkElement d, bool value) => d.CheckNotNull(nameof(d)).SetValue(PropagateInputBindingsToWindowProperty, value);

    private static void OnPropagateInputBindingsToWindowChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if ((bool)e.OldValue == false && (bool)e.NewValue)
        {
            if (d is FrameworkElement elem)
            {
                elem.Loaded += FrameworkElement_Loaded;
            }
        }
    }

    /// <summary>
    /// Initiates the InputBindings transfer when the window is loaded.
    /// </summary>
    private static void FrameworkElement_Loaded(object sender, RoutedEventArgs e)
    {
        var frameworkElement = (FrameworkElement)sender;
        frameworkElement.Loaded -= FrameworkElement_Loaded;

        var window = Window.GetWindow(frameworkElement);
        if (window != null)
        {
            // Move input bindings from the FrameworkElement to the window.
            TransferBindingsToWindow(frameworkElement, window, true);
        }
    }

    /// <summary>
    /// Transfers all InputBindings from an abject to another.
    /// </summary>
    /// <param name="src">The object to detach the InputBindings from.</param>
    /// <param name="dst">The object to attach the InputBindings to.</param>
    /// <param name="remove">Whether to remove InputBindings from the source.</param>
    public static void TransferBindingsToWindow(FrameworkElement src, FrameworkElement dst, bool remove)
    {
        src.CheckNotNull(nameof(src));
        dst.CheckNotNull(nameof(dst));

        for (var i = src.InputBindings.Count - 1; i >= 0; i--)
        {
            var inputBinding = src.InputBindings[i];
            dst.InputBindings.Add(inputBinding);
            if (remove)
            {
                src.InputBindings.Remove(inputBinding);
            }
        }

    }
}
