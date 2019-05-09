using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace EmergenceGuardian.MediaPlayerUI {
    public static class UIProperties {
        // UIObserver
        public static readonly DependencyProperty UIObserverProperty = DependencyProperty.RegisterAttached("UIObserver", typeof(PropertyChangeNotifier),
            typeof(UIProperties), new UIPropertyMetadata());
        public static PropertyChangeNotifier GetUIObserver(MediaPlayerWpf obj) => (PropertyChangeNotifier)obj.GetValue(UIObserverProperty);
        public static void SetUIObserver(MediaPlayerWpf obj, PropertyChangeNotifier value) => obj.SetValue(UIObserverProperty, value);

        /// <summary>
        /// Ensures there is an UIObserver on the MediaPlayerWpf object.
        /// </summary>
        private static void CreateUIObserver(DependencyObject d) {
            if (d is MediaPlayerWpf P && GetUIObserver(P) == null) {
                PropertyChangeNotifier Notifier = new PropertyChangeNotifier(P, MediaPlayerWpf.UIPropertyKey.DependencyProperty);
                Notifier.ValueChanged += Notifier_ValueChanged;
                SetUIObserver(P, Notifier);
            }
        }

        /// <summary>
        /// UI property can be set after attached property is applied, so we must monitor to apply it when set.
        /// </summary>
        private static void Notifier_ValueChanged(object sender, EventArgs e) {
            MediaPlayerWpf P = sender as MediaPlayerWpf;
            if (GetMouseFullscreen(P) != null)
                P.UI.MouseFullscreen = GetMouseFullscreen(P).Value;
            if (GetMousePause(P) != null)
                P.UI.MousePause = GetMousePause(P).Value;
            if (GetChangeVolumeOnMouseWheel(P) != null)
                P.UI.ChangeVolumeOnMouseWheel = GetChangeVolumeOnMouseWheel(P).Value;
            if (GetIsPlayPauseVisible(P) != null)
                P.UI.IsPlayPauseVisible = GetIsPlayPauseVisible(P).Value;
            if (GetIsStopVisible(P) != null)
                P.UI.IsStopVisible = GetIsStopVisible(P).Value;
            if (GetIsLoopVisible(P) != null)
                P.UI.IsLoopVisible = GetIsLoopVisible(P).Value;
            if (GetIsVolumeVisible(P) != null)
                P.UI.IsVolumeVisible = GetIsVolumeVisible(P).Value;
            if (GetIsSpeedVisible(P) != null)
                P.UI.IsSpeedVisible = GetIsSpeedVisible(P).Value;
            if (GetIsSeekBarVisible(P) != null)
                P.UI.IsSeekBarVisible = GetIsSeekBarVisible(P).Value;
            if (GetPositionDisplay(P) != null)
                P.UI.PositionDisplay = GetPositionDisplay(P).Value;
            if (GetPositionBar(P) != null)
                P.UI.PositionBar = GetPositionBar(P).Value;
        }

        // Properties must be nullable so various inherited class types can have different default values. This sets the value only when explicitly set 
        // via attached property, and avoids ValueChanged not triggering if attached property default value doesn't match object's default values.

        // MouseFullscreen
        public static readonly DependencyProperty MouseFullscreenProperty = DependencyProperty.RegisterAttached("MouseFullscreen", typeof(MouseTrigger?),
            typeof(UIProperties), new UIPropertyMetadata(OnMouseFullscreenChanged));
        public static MouseTrigger? GetMouseFullscreen(MediaPlayerWpf obj) => (MouseTrigger?)obj.GetValue(MouseFullscreenProperty);
        public static void SetMouseFullscreen(MediaPlayerWpf obj, MouseTrigger? value) => obj.SetValue(MouseFullscreenProperty, value);
        private static void OnMouseFullscreenChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            CreateUIObserver(d);
            if (d is MediaPlayerWpf P && P.UI != null && e.NewValue != null)
                P.UI.MouseFullscreen = (MouseTrigger)e.NewValue;
        }

        // MousePause
        public static readonly DependencyProperty MousePauseProperty = DependencyProperty.RegisterAttached("MousePause", typeof(MouseTrigger?),
            typeof(UIProperties), new UIPropertyMetadata(OnMousePauseChanged));
        public static MouseTrigger? GetMousePause(MediaPlayerWpf obj) => (MouseTrigger?)obj.GetValue(MousePauseProperty);
        public static void SetMousePause(MediaPlayerWpf obj, MouseTrigger? value) => obj.SetValue(MousePauseProperty, value);
        private static void OnMousePauseChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            CreateUIObserver(d);
            if (d is MediaPlayerWpf P && P.UI != null && e.NewValue != null)
                P.UI.MousePause = (MouseTrigger)e.NewValue;
        }

        // ChangeVolumeOnMouseWheel
        public static readonly DependencyProperty ChangeVolumeOnMouseWheelProperty = DependencyProperty.RegisterAttached("ChangeVolumeOnMouseWheel", typeof(bool?),
            typeof(UIProperties), new UIPropertyMetadata(OnChangeVolumeOnMouseWheelChanged));
        public static bool? GetChangeVolumeOnMouseWheel(MediaPlayerWpf obj) => (bool?)obj.GetValue(ChangeVolumeOnMouseWheelProperty);
        public static void SetChangeVolumeOnMouseWheel(MediaPlayerWpf obj, bool? value) => obj.SetValue(ChangeVolumeOnMouseWheelProperty, value);
        private static void OnChangeVolumeOnMouseWheelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            CreateUIObserver(d);
            if (d is MediaPlayerWpf P && P.UI != null && e.NewValue != null)
                P.UI.ChangeVolumeOnMouseWheel = (bool)e.NewValue;
        }

        // IsPlayPauseVisible
        public static readonly DependencyProperty IsPlayPauseVisibleProperty = DependencyProperty.RegisterAttached("IsPlayPauseVisible", typeof(bool?),
            typeof(UIProperties), new UIPropertyMetadata(OnIsPlayPauseVisibleChanged));
        public static bool? GetIsPlayPauseVisible(MediaPlayerWpf obj) => (bool?)obj.GetValue(IsPlayPauseVisibleProperty);
        public static void SetIsPlayPauseVisible(MediaPlayerWpf obj, bool? value) => obj.SetValue(IsPlayPauseVisibleProperty, value);
        private static void OnIsPlayPauseVisibleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            CreateUIObserver(d);
            if (d is MediaPlayerWpf P && P.UI != null && e.NewValue != null)
                P.UI.IsPlayPauseVisible = (bool)e.NewValue;
        }

        // IsStopVisible
        public static readonly DependencyProperty IsStopVisibleProperty = DependencyProperty.RegisterAttached("IsStopVisible", typeof(bool?),
            typeof(UIProperties), new UIPropertyMetadata(OnIsStopVisibleChanged));
        public static bool? GetIsStopVisible(MediaPlayerWpf obj) => (bool?)obj.GetValue(IsStopVisibleProperty);
        public static void SetIsStopVisible(MediaPlayerWpf obj, bool? value) => obj.SetValue(IsStopVisibleProperty, value);
        private static void OnIsStopVisibleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            CreateUIObserver(d);
            if (d is MediaPlayerWpf P && P.UI != null && e.NewValue != null)
                P.UI.IsStopVisible = (bool)e.NewValue;
        }

        // IsLoopVisible
        public static readonly DependencyProperty IsLoopVisibleProperty = DependencyProperty.RegisterAttached("IsLoopVisible", typeof(bool?),
            typeof(UIProperties), new UIPropertyMetadata(OnIsLoopVisibleChanged));
        public static bool? GetIsLoopVisible(MediaPlayerWpf obj) => (bool?)obj.GetValue(IsLoopVisibleProperty);
        public static void SetIsLoopVisible(MediaPlayerWpf obj, bool? value) => obj.SetValue(IsLoopVisibleProperty, value);
        private static void OnIsLoopVisibleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            CreateUIObserver(d);
            if (d is MediaPlayerWpf P && P.UI != null && e.NewValue != null)
                P.UI.IsLoopVisible = (bool)e.NewValue;
        }

        // IsVolumeVisible
        public static readonly DependencyProperty IsVolumeVisibleProperty = DependencyProperty.RegisterAttached("IsVolumeVisible", typeof(bool?),
            typeof(UIProperties), new UIPropertyMetadata(OnIsVolumeVisibleChanged));
        public static bool? GetIsVolumeVisible(MediaPlayerWpf obj) => (bool?)obj.GetValue(IsVolumeVisibleProperty);
        public static void SetIsVolumeVisible(MediaPlayerWpf obj, bool? value) => obj.SetValue(IsVolumeVisibleProperty, value);
        private static void OnIsVolumeVisibleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            CreateUIObserver(d);
            if (d is MediaPlayerWpf P && P.UI != null && e.NewValue != null)
                P.UI.IsVolumeVisible = (bool)e.NewValue;
        }

        // IsSpeedVisible
        public static readonly DependencyProperty IsSpeedVisibleProperty = DependencyProperty.RegisterAttached("IsSpeedVisible", typeof(bool?),
            typeof(UIProperties), new UIPropertyMetadata(OnIsSpeedVisibleChanged));
        public static bool? GetIsSpeedVisible(MediaPlayerWpf obj) => (bool?)obj.GetValue(IsSpeedVisibleProperty);
        public static void SetIsSpeedVisible(MediaPlayerWpf obj, bool? value) => obj.SetValue(IsSpeedVisibleProperty, value);
        private static void OnIsSpeedVisibleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            CreateUIObserver(d);
            if (d is MediaPlayerWpf P && P.UI != null && e.NewValue != null)
                P.UI.IsSpeedVisible = (bool)e.NewValue;
        }

        // IsSeekBarVisible
        public static readonly DependencyProperty IsSeekBarVisibleProperty = DependencyProperty.RegisterAttached("IsSeekBarVisible", typeof(bool?),
            typeof(UIProperties), new UIPropertyMetadata(OnIsSeekBarVisibleChanged));
        public static bool? GetIsSeekBarVisible(MediaPlayerWpf obj) => (bool?)obj.GetValue(IsSeekBarVisibleProperty);
        public static void SetIsSeekBarVisible(MediaPlayerWpf obj, bool? value) => obj.SetValue(IsSeekBarVisibleProperty, value);
        private static void OnIsSeekBarVisibleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            CreateUIObserver(d);
            if (d is MediaPlayerWpf P && P.UI != null && e.NewValue != null)
                P.UI.IsSeekBarVisible = (bool)e.NewValue;
        }

        // PositionDisplay
        public static readonly DependencyProperty PositionDisplayProperty = DependencyProperty.RegisterAttached("PositionDisplay", typeof(TimeDisplayStyles?),
            typeof(UIProperties), new UIPropertyMetadata(OnPositionDisplayChanged));
        public static TimeDisplayStyles? GetPositionDisplay(MediaPlayerWpf obj) => (TimeDisplayStyles?)obj.GetValue(PositionDisplayProperty);
        public static void SetPositionDisplay(MediaPlayerWpf obj, TimeDisplayStyles? value) => obj.SetValue(PositionDisplayProperty, value);
        private static void OnPositionDisplayChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            CreateUIObserver(d);
            if (d is MediaPlayerWpf P && P.UI != null && e.NewValue != null)
                P.UI.PositionDisplay = (TimeDisplayStyles)e.NewValue;
        }

        // PositionBar
        public static readonly DependencyProperty PositionBarProperty = DependencyProperty.RegisterAttached("PositionBar", typeof(TimeSpan?),
            typeof(UIProperties), new UIPropertyMetadata(OnPositionBarChanged));
        public static TimeSpan? GetPositionBar(MediaPlayerWpf obj) => (TimeSpan?)obj.GetValue(PositionBarProperty);
        public static void SetPositionBar(MediaPlayerWpf obj, TimeSpan? value) => obj.SetValue(PositionBarProperty, value);
        private static void OnPositionBarChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            CreateUIObserver(d);
            if (d is MediaPlayerWpf P && P.UI != null && e.NewValue != null)
                P.UI.PositionBar = (TimeSpan)e.NewValue;
        }
    }
}
