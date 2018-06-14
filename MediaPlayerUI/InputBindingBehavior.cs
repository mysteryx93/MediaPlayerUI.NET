using System;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace EmergenceGuardian.MediaPlayerUI {
	public class InputBindingBehavior {
		public static bool GetPropagateInputBindingsToWindow(FrameworkElement obj) {
			return (bool)obj.GetValue(PropagateInputBindingsToWindowProperty);
		}

		public static void SetPropagateInputBindingsToWindow(FrameworkElement obj, bool value) {
			obj.SetValue(PropagateInputBindingsToWindowProperty, value);
		}

		public static readonly DependencyProperty PropagateInputBindingsToWindowProperty =
			DependencyProperty.RegisterAttached("PropagateInputBindingsToWindow", typeof(bool), typeof(InputBindingBehavior),
			new PropertyMetadata(false, OnPropagateInputBindingsToWindowChanged));

		private static void OnPropagateInputBindingsToWindowChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
			if ((bool)e.OldValue == false && (bool)e.NewValue == true)
				((FrameworkElement)d).Loaded += frameworkElement_Loaded;
		}

		private static void frameworkElement_Loaded(object sender, RoutedEventArgs e) {
			var frameworkElement = (FrameworkElement)sender;
			frameworkElement.Loaded -= frameworkElement_Loaded;

			var window = Window.GetWindow(frameworkElement);
			if (window == null) {
				return;
			}

			// Move input bindings from the FrameworkElement to the window.
			TransferBindingsToWindow(frameworkElement, window, true);
		}

		public static void TransferBindingsToWindow(FrameworkElement src, FrameworkElement dst, bool remove) {
			for (int i = src.InputBindings.Count - 1; i >= 0; i--) {
				var inputBinding = (InputBinding)src.InputBindings[i];
				dst.InputBindings.Add(inputBinding);
				if (remove)
					src.InputBindings.Remove(inputBinding);
			}

		}
	}
}
