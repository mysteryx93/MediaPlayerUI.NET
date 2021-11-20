using System;
using System.Windows;
using System.Windows.Input;

namespace HanumanInstitute.MediaPlayer.WPF
{
    public static class InputBindingBehavior
    {
        public static readonly DependencyProperty PropagateInputBindingsToWindowProperty =
            DependencyProperty.RegisterAttached("PropagateInputBindingsToWindow", typeof(bool), typeof(InputBindingBehavior),
            new PropertyMetadata(false, OnPropagateInputBindingsToWindowChanged));
        public static bool GetPropagateInputBindingsToWindow(FrameworkElement d) => (bool)(d.CheckNotNull(nameof(d)).GetValue(PropagateInputBindingsToWindowProperty) ?? false);
        public static void SetPropagateInputBindingsToWindow(FrameworkElement d, bool value) => d.CheckNotNull(nameof(d)).SetValue(PropagateInputBindingsToWindowProperty, value);

        private static void OnPropagateInputBindingsToWindowChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if ((bool)e.OldValue == false && (bool)e.NewValue == true)
            {
                if (d is FrameworkElement elem)
                {
                    elem.Loaded += FrameworkElement_Loaded;
                }
            }
        }

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

        public static void TransferBindingsToWindow(FrameworkElement src, FrameworkElement dst, bool remove)
        {
            src.CheckNotNull(nameof(src));
            dst.CheckNotNull(nameof(dst));

            for (var i = src.InputBindings.Count - 1; i >= 0; i--)
            {
                var inputBinding = (InputBinding)src.InputBindings[i];
                dst.InputBindings.Add(inputBinding);
                if (remove)
                {
                    src.InputBindings.Remove(inputBinding);
                }
            }

        }
    }
}
