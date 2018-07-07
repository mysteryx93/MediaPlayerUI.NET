using System;
using System.Windows.Forms;
using System.Windows.Forms.Integration;
using System.Windows.Input;

namespace EmergenceGuardian.MediaPlayerUI {
    public class ExtendedWindowsFormsHost : WindowsFormsHost {
        public ExtendedWindowsFormsHost() {
            ChildChanged += OnChildChanged;
        }

        private void OnChildChanged(object sender, ChildChangedEventArgs childChangedEventArgs) {
            if (childChangedEventArgs.PreviousChild is Control previousChild) {
                previousChild.MouseDown -= OnMouseDown;
                previousChild.MouseWheel -= OnMouseWheel;
            }
            if (Child != null) {
                Child.MouseDown += OnMouseDown;
                Child.MouseWheel += OnMouseWheel;
            }
        }

        private void OnMouseWheel(object sender, System.Windows.Forms.MouseEventArgs e) {
            RaiseEvent(new MouseWheelEventArgs(Mouse.PrimaryDevice, (int)DateTime.Now.Ticks, e.Delta) {
                RoutedEvent = Mouse.MouseWheelEvent,
                Source = this
            });
        }

        private void OnMouseDown(object sender, System.Windows.Forms.MouseEventArgs e) {
            MouseButton? wpfButton = ConvertToWpf(e.Button);
            if (!wpfButton.HasValue)
                return;

            // Double-clicks aren't propogating.
            //if (e.Clicks == 2) {
            //    RaiseEvent(new MouseButtonEventArgs(Mouse.PrimaryDevice, (int)DateTime.Now.Ticks, wpfButton.Value) {
            //        RoutedEvent = System.Windows.Controls.Control.MouseDoubleClickEvent,
            //        Source = this
            //    });
            //} else {
            RaiseEvent(new MouseButtonEventArgs(Mouse.PrimaryDevice, (int)DateTime.Now.Ticks, wpfButton.Value) {
                RoutedEvent = Mouse.MouseDownEvent,
                Source = this
            });
            //}
        }

        private MouseButton? ConvertToWpf(MouseButtons winformButton) {
            switch (winformButton) {
                case MouseButtons.Left:
                    return MouseButton.Left;
                case MouseButtons.None:
                    return null;
                case MouseButtons.Right:
                    return MouseButton.Right;
                case MouseButtons.Middle:
                    return MouseButton.Middle;
                case MouseButtons.XButton1:
                    return MouseButton.XButton1;
                case MouseButtons.XButton2:
                    return MouseButton.XButton2;
                default:
                    throw new ArgumentOutOfRangeException("winformButton");
            }
        }
    }
}
