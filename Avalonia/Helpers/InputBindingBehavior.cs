//using System;
//using Avalonia;
//using Avalonia.Controls;
//using Avalonia.Data;
//using Avalonia.Input;
//using Avalonia.VisualTree;

//namespace HanumanInstitute.MediaPlayer.Avalonia.Helpers;

//public class InputBindingBehavior : AvaloniaObject
//{
//    static InputBindingBehavior()
//    {
//        PropagateInputBindingsToWindowProperty.Changed.Subscribe(OnPropagateInputBindingsToWindowChanged);
//    }

//    public static readonly AttachedProperty<bool> PropagateInputBindingsToWindowProperty =
//        AvaloniaProperty.RegisterAttached<InputBindingBehavior, Control, bool>("PropagateInputBindingsToWindow",
//            false, false, BindingMode.OneTime);

//    public static bool GetPropagateInputBindingsToWindow(AvaloniaObject d) =>
//        d.CheckNotNull(nameof(d)).GetValue(PropagateInputBindingsToWindowProperty);

//    public static void SetPropagateInputBindingsToWindow(AvaloniaObject d, bool value) =>
//        d.CheckNotNull(nameof(d)).SetValue(PropagateInputBindingsToWindowProperty, value);

//    private static void OnPropagateInputBindingsToWindowChanged(AvaloniaPropertyChangedEventArgs<bool> e)
//    {
//        if (!e.OldValue.GetValueOrDefault() && e.NewValue.GetValueOrDefault())
//        {
//            if (e.Sender is StyledElement elem)
//            {
//                elem.Initialized += FrameworkElement_Initialized;
//            }
//        }
//    }

//    private static void FrameworkElement_Initialized(object? sender, EventArgs e)
//    {
//        var frameworkElement = (Control)sender!;
//        frameworkElement.Initialized -= FrameworkElement_Initialized;

//        var window = frameworkElement.FindAncestorOfType<TopLevel>();
//        if (window != null)
//        {
//            // Move input bindings from the FrameworkElement to the window.
//            TransferBindingsToWindow(frameworkElement, window, true);
//        }
//    }

//    public static void TransferBindingsToWindow(IInputElement src, IInputElement dst, bool remove)
//    {
//        src.CheckNotNull(nameof(src));
//        dst.CheckNotNull(nameof(dst));

//        for (var i = src.KeyBindings.Count - 1; i >= 0; i--)
//        {
//            var key = src.KeyBindings[i];
//            dst.KeyBindings.Add(key);
//            if (remove)
//            {
//                src.KeyBindings.Remove(key);
//            }
//        }
//    }
//}
