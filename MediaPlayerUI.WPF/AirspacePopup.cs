using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Interop;

namespace HanumanInstitute.MediaPlayerUI
{
    /// <summary>
    /// Based on
    /// https://stackoverflow.com/questions/6087835/can-i-overlay-a-wpf-window-on-top-of-another/6452940#6452940
    /// </summary>
    public class AirspacePopup : Popup
    {
        private static void OnIsTopmostChanged(DependencyObject source, DependencyPropertyChangedEventArgs e)
        {
            if (source is AirspacePopup popup)
            {
                popup.SetTopmostState(popup.IsTopmost);
            }
        }

        private static void ParentWindowPropertyChanged(DependencyObject source, DependencyPropertyChangedEventArgs e)
        {
            if (source is AirspacePopup popup)
            {
                popup.ParentWindowChanged();
            }
        }

        private bool? _appliedTopMost;
        private bool _alreadyLoaded;
        private Window? _parentWindow;

        public AirspacePopup()
        {
            Loaded += OnPopupLoaded;
            Unloaded += OnPopupUnloaded;

            var descriptor = DependencyPropertyDescriptor.FromProperty(PlacementTargetProperty, typeof(AirspacePopup));
            descriptor.AddValueChanged(this, PlacementTargetChanged);
        }

        // IsTopmost
        public static readonly DependencyProperty IsTopmostProperty = DependencyProperty.Register("IsTopmost", typeof(bool), typeof(AirspacePopup),
            new FrameworkPropertyMetadata(false, OnIsTopmostChanged));
        public bool IsTopmost { get { return (bool)GetValue(IsTopmostProperty); } set { SetValue(IsTopmostProperty, value); } }

        // FollowPlacementTarget
        public static readonly DependencyProperty FollowPlacementTargetProperty = DependencyProperty.RegisterAttached("FollowPlacementTarget", typeof(bool), typeof(AirspacePopup),
            new UIPropertyMetadata(false));
        public bool FollowPlacementTarget { get { return (bool)GetValue(FollowPlacementTargetProperty); } set { SetValue(FollowPlacementTargetProperty, value); } }

        // AllowOutsideScreenPlacement
        public static readonly DependencyProperty AllowOutsideScreenPlacementProperty = DependencyProperty.RegisterAttached("AllowOutsideScreenPlacement", typeof(bool), typeof(AirspacePopup),
            new UIPropertyMetadata(false));
        public bool AllowOutsideScreenPlacement { get { return (bool)GetValue(AllowOutsideScreenPlacementProperty); } set { SetValue(AllowOutsideScreenPlacementProperty, value); } }

        // ParentWindow
        public static readonly DependencyProperty ParentWindowProperty = DependencyProperty.RegisterAttached("ParentWindow", typeof(Window), typeof(AirspacePopup),
            new UIPropertyMetadata(null, ParentWindowPropertyChanged));
        public Window ParentWindow { get { return (Window)GetValue(ParentWindowProperty); } set { SetValue(ParentWindowProperty, value); } }

        private void ParentWindowChanged()
        {
            if (ParentWindow != null)
            {
                ParentWindow.LocationChanged += (s, e) => UpdatePopupPosition();
                ParentWindow.SizeChanged += (s, e) => UpdatePopupPosition();
            }
        }
        private void PlacementTargetChanged(object? sender, EventArgs e)
        {
            if (PlacementTarget is FrameworkElement target)
            {
                target.SizeChanged += (sender2, e2) => UpdatePopupPosition();
            }
        }

        private void UpdatePopupPosition()
        {
            if (AllowOutsideScreenPlacement == true && PlacementTarget is FrameworkElement target && PresentationSource.FromVisual(target) != null)
            {
                if (PresentationSource.FromVisual(target) != null &&
                    AllowOutsideScreenPlacement == true)
                {
                    var leftOffset = CutLeft(target);
                    var topOffset = CutTop(target);
                    var rightOffset = CutRight(target);
                    var bottomOffset = CutBottom(target);
                    Debug.WriteLine(bottomOffset);
                    Width = Math.Max(0, Math.Min(leftOffset, rightOffset) + target.ActualWidth);
                    Height = Math.Max(0, Math.Min(topOffset, bottomOffset) + target.ActualHeight);

                    if (Child is FrameworkElement child)
                    {
                        child.Margin = new Thickness(leftOffset, topOffset, rightOffset, bottomOffset);
                    }
                }
            }
            if (FollowPlacementTarget == true)
            {
                HorizontalOffset += 0.01;
                HorizontalOffset -= 0.01;
            }
        }

        private static double CutLeft(FrameworkElement placementTarget)
        {
            var point = placementTarget.PointToScreen(new Point(0, placementTarget.ActualWidth));
            return Math.Min(0, point.X);
        }

        private static double CutTop(FrameworkElement placementTarget)
        {
            var point = placementTarget.PointToScreen(new Point(placementTarget.ActualHeight, 0));
            return Math.Min(0, point.Y);
        }

        private static double CutRight(FrameworkElement placementTarget)
        {
            var point = placementTarget.PointToScreen(new Point(0, placementTarget.ActualWidth));
            point.X += placementTarget.ActualWidth;
            return Math.Min(0, SystemParameters.VirtualScreenWidth - (Math.Max(SystemParameters.VirtualScreenWidth, point.X)));
        }

        private static double CutBottom(FrameworkElement placementTarget)
        {
            var point = placementTarget.PointToScreen(new Point(placementTarget.ActualHeight, 0));
            point.Y += placementTarget.ActualHeight;
            return Math.Min(0, SystemParameters.VirtualScreenHeight - (Math.Max(SystemParameters.VirtualScreenHeight, point.Y)));
        }

        private void OnPopupLoaded(object sender, RoutedEventArgs e)
        {
            if (_alreadyLoaded) { return; }

            _alreadyLoaded = true;

            if (Child != null)
            {
                Child.AddHandler(PreviewMouseLeftButtonDownEvent, new MouseButtonEventHandler(OnChildPreviewMouseLeftButtonDown), true);
            }

            _parentWindow = Window.GetWindow(this);

            if (_parentWindow != null)
            {
                _parentWindow.Activated += OnParentWindowActivated;
                _parentWindow.Deactivated += OnParentWindowDeactivated;
            }
        }

        private void OnPopupUnloaded(object? sender, RoutedEventArgs e)
        {
            if (_parentWindow == null) { return; }

            _parentWindow.Activated -= OnParentWindowActivated;
            _parentWindow.Deactivated -= OnParentWindowDeactivated;
        }

        private void OnParentWindowActivated(object? sender, EventArgs e)
        {
            SetTopmostState(true);
        }

        private void OnParentWindowDeactivated(object? sender, EventArgs e)
        {
            if (!IsTopmost)
            {
                SetTopmostState(IsTopmost);
            }
        }

        private void OnChildPreviewMouseLeftButtonDown(object? sender, MouseButtonEventArgs e)
        {
            SetTopmostState(true);
            if (_parentWindow?.IsActive == false && !IsTopmost)
            {
                _parentWindow.Activate();
            }
        }

        protected override void OnOpened(EventArgs e)
        {
            SetTopmostState(IsTopmost);
            base.OnOpened(e);
        }

        private void SetTopmostState(bool isTop)
        {
            // Don’t apply state if it’s the same as incoming state
            if (_appliedTopMost.HasValue && _appliedTopMost == isTop) { return; }

            if (Child != null && PresentationSource.FromVisual(Child) is HwndSource hwndSource)
            {
                var hwnd = hwndSource.Handle;
                if (NativeMethods.GetWindowRect(hwnd, out var rect))
                {
                    Debug.WriteLine("setting z-order " + isTop);

                    if (isTop)
                    {
                        NativeMethods.SetWindowPos(hwnd, NativeMethods.HWND_TOPMOST, rect.Left, rect.Top, (int)Width, (int)Height, NativeMethods.TOPMOST_FLAGS);
                    }
                    else
                    {
                        // Z-Order would only get refreshed/reflected if clicking the
                        // the titlebar (as opposed to other parts of the external
                        // window) unless I first set the popup to HWND_BOTTOM
                        // then HWND_TOP before HWND_NOTOPMOST
                        NativeMethods.SetWindowPos(hwnd, NativeMethods.HWND_BOTTOM, rect.Left, rect.Top, (int)Width, (int)Height, NativeMethods.TOPMOST_FLAGS);
                        NativeMethods.SetWindowPos(hwnd, NativeMethods.HWND_TOP, rect.Left, rect.Top, (int)Width, (int)Height, NativeMethods.TOPMOST_FLAGS);
                        NativeMethods.SetWindowPos(hwnd, NativeMethods.HWND_NOTOPMOST, rect.Left, rect.Top, (int)Width, (int)Height, NativeMethods.TOPMOST_FLAGS);
                    }

                    _appliedTopMost = isTop;
                }
            }
        }

        private class NativeMethods
        {
            [SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "We must use fields instead of properties for API calls.")]
            [StructLayout(LayoutKind.Sequential)]
            public struct RECT
            {
                public int Left;
                public int Top;
                public int Right;
                public int Bottom;
            }

            [DllImport("user32.dll")]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

            [DllImport("user32.dll")]
            public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int x, int y, int cx, int cy, uint uFlags);

            public static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
            public static readonly IntPtr HWND_NOTOPMOST = new IntPtr(-2);
            public static readonly IntPtr HWND_TOP = new IntPtr(0);
            public static readonly IntPtr HWND_BOTTOM = new IntPtr(1);

            public const uint SWP_NOSIZE = 0x0001;
            public const uint SWP_NOMOVE = 0x0002;
            public const uint SWP_NOZORDER = 0x0004;
            public const uint SWP_NOREDRAW = 0x0008;
            public const uint SWP_NOACTIVATE = 0x0010;

            public const uint SWP_FRAMECHANGED = 0x0020; /* The frame changed: send WM_NCCALCSIZE */
            public const uint SWP_SHOWWINDOW = 0x0040;
            public const uint SWP_HIDEWINDOW = 0x0080;
            public const uint SWP_NOCOPYBITS = 0x0100;
            public const uint SWP_NOOWNERZORDER = 0x0200; /* Don’t do owner Z ordering */
            public const uint SWP_NOSENDCHANGING = 0x0400; /* Don’t send WM_WINDOWPOSCHANGING */

            public const uint TOPMOST_FLAGS = SWP_NOACTIVATE | SWP_NOOWNERZORDER | SWP_NOSIZE | SWP_NOMOVE | SWP_NOREDRAW | SWP_NOSENDCHANGING;
        }
    }
}
