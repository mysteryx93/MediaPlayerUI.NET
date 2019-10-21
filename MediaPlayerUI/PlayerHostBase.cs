using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace EmergenceGuardian.MediaPlayerUI {
    /// <summary>
    /// Represents the base class for Host controls. Each player implementation inherits from this class to manage commmon properties of a player.
    /// </summary>
    public abstract class PlayerHostBase : Control {
        static PlayerHostBase() {
            FocusableProperty.OverrideMetadata(typeof(PlayerHostBase), new FrameworkPropertyMetadata(false));
        }

        #region Declarations / Constructor

        private bool isSettingPosition = false;
        private Panel innerControlParentCache;

        // Restart won't be triggered after Stop while this timer is running.
        private bool isStopping = false;
        private DispatcherTimer stopTimer;

        /// <summary>
        /// Occurs after a media file is loaded.
        /// </summary>
        public event EventHandler MediaLoaded;
        /// <summary>
        /// Occurs after a media file is unloaded.
        /// </summary>
        public event EventHandler MediaUnloaded;

        public PlayerHostBase() {
        }

        public override void OnApplyTemplate() {
            base.OnApplyTemplate();
            stopTimer = new DispatcherTimer(TimeSpan.FromSeconds(1), DispatcherPriority.Background, delegate { stopTimer.Stop(); isStopping = false; }, Dispatcher);
        }

        #endregion


        #region Properties

        // Position
        public static readonly DependencyProperty PositionProperty = DependencyProperty.Register("Position", typeof(TimeSpan), typeof(PlayerHostBase),
            new PropertyMetadata(TimeSpan.Zero, PositionChanged, CoercePosition));
        public TimeSpan Position { get => (TimeSpan)GetValue(PositionProperty); set => SetValue(PositionProperty, value); }
        private static void PositionChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            PlayerHostBase P = d as PlayerHostBase;
            P.PositionChanged((TimeSpan)e.NewValue, !P.isSettingPosition);
        }
        private static object CoercePosition(DependencyObject d, object baseValue) {
            PlayerHostBase P = d as PlayerHostBase;
            return P.CoercePosition((TimeSpan)baseValue);
        }
        protected virtual void PositionChanged(TimeSpan value, bool isSeeking) { }
        protected virtual TimeSpan CoercePosition(TimeSpan value) {
            if (value < TimeSpan.Zero)
                return TimeSpan.Zero;
            else if (value > Duration)
                return Duration;
            else
                return value;
        }

        // Duration
        public static readonly DependencyPropertyKey DurationPropertyKey = DependencyProperty.RegisterReadOnly("Duration", typeof(TimeSpan), typeof(PlayerHostBase),
            new PropertyMetadata(TimeSpan.FromSeconds(1), DurationChanged));
        private static readonly DependencyProperty DurationProperty = DurationPropertyKey.DependencyProperty;
        public TimeSpan Duration { get => (TimeSpan)GetValue(DurationProperty); protected set => SetValue(DurationPropertyKey, value); }
        private static void DurationChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            PlayerHostBase P = d as PlayerHostBase;
            P.DurationChanged((TimeSpan)e.NewValue);
        }
        protected virtual void DurationChanged(TimeSpan value) { }

        // IsPlaying
        public static readonly DependencyProperty IsPlayingProperty = DependencyProperty.Register("IsPlaying", typeof(bool), typeof(PlayerHostBase),
            new PropertyMetadata(false, IsPlayingChanged, CoerceIsPlaying));
        public bool IsPlaying { get => (bool)GetValue(IsPlayingProperty); set => SetValue(IsPlayingProperty, value); }
        private static void IsPlayingChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            PlayerHostBase P = d as PlayerHostBase;
            P.IsPlayingChanged((bool)e.NewValue);
        }
        private static object CoerceIsPlaying(DependencyObject d, object baseValue) {
            PlayerHostBase P = d as PlayerHostBase;
            return P.CoerceIsPlaying((bool)baseValue);
        }
        protected virtual void IsPlayingChanged(bool value) { }
        protected virtual bool CoerceIsPlaying(bool value) => value;

        // Volume
        public static readonly DependencyProperty VolumeProperty = DependencyProperty.Register("Volume", typeof(int), typeof(PlayerHostBase),
            new PropertyMetadata(50, VolumeChanged, CoerceVolume));
        public int Volume { get => (int)GetValue(VolumeProperty); set => SetValue(VolumeProperty, value); }
        private static void VolumeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            PlayerHostBase P = d as PlayerHostBase;
            P.VolumeChanged((int)e.NewValue);
        }
        private static object CoerceVolume(DependencyObject d, object baseValue) {
            PlayerHostBase P = d as PlayerHostBase;
            return P.CoerceVolume((int)baseValue);
        }
        protected virtual void VolumeChanged(int value) { }
        protected virtual int CoerceVolume(int value) {
            if (value < 0)
                return 0;
            else if (value > 100)
                return 100;
            else
                return value;
        }

        // SpeedFloat
        public static readonly DependencyProperty SpeedFloatProperty = DependencyProperty.Register("SpeedFloat", typeof(float), typeof(PlayerHostBase),
            new PropertyMetadata(1f, SpeedChanged, CoerceSpeedFloat));
        public float SpeedFloat { get => (float)GetValue(SpeedFloatProperty); set => SetValue(SpeedFloatProperty, value); }
        private static void SpeedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            PlayerHostBase P = d as PlayerHostBase;
            P.SpeedChanged(P.GetSpeed());
        }
        private static object CoerceSpeedFloat(DependencyObject d, object baseValue) {
            PlayerHostBase P = d as PlayerHostBase;
            return P.CoerceSpeedFloat((float)baseValue);
        }
        protected virtual void SpeedChanged(float value) { }
        protected virtual float CoerceSpeedFloat(float value) => value == 0 ? 1 : value;

        // SpeedInt
        public static readonly DependencyProperty SpeedIntProperty = DependencyProperty.Register("SpeedInt", typeof(int), typeof(PlayerHostBase),
            new PropertyMetadata(0, SpeedChanged, CoerceSpeedInt));
        private static object CoerceSpeedInt(DependencyObject d, object baseValue) {
            PlayerHostBase P = d as PlayerHostBase;
            return P.CoerceSpeedInt((int)baseValue);
        }
        public int SpeedInt { get => (int)GetValue(SpeedIntProperty); set => SetValue(SpeedIntProperty, value); }
        protected virtual int CoerceSpeedInt(int value) => value;

        // Loop
        public static readonly DependencyProperty LoopProperty = DependencyProperty.Register("Loop", typeof(bool), typeof(PlayerHostBase),
            new PropertyMetadata(false, LoopChanged, CoerceLoop));
        public bool Loop { get => (bool)GetValue(LoopProperty); set => SetValue(LoopProperty, value); }
        private static void LoopChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            PlayerHostBase P = d as PlayerHostBase;
            P.LoopChanged((bool)e.NewValue);
        }
        private static object CoerceLoop(DependencyObject d, object baseValue) {
            PlayerHostBase P = d as PlayerHostBase;
            return P.CoerceLoop((bool)baseValue);
        }
        protected virtual void LoopChanged(bool value) { }
        protected virtual bool CoerceLoop(bool value) => value;

        // AutoPlay
        public static readonly DependencyProperty AutoPlayProperty = DependencyProperty.Register("AutoPlay", typeof(bool), typeof(PlayerHostBase),
            new PropertyMetadata(true, AutoPlayChanged, CoerceAutoPlay));
        public bool AutoPlay { get => (bool)GetValue(AutoPlayProperty); set => SetValue(AutoPlayProperty, value); }
        private static void AutoPlayChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            PlayerHostBase P = d as PlayerHostBase;
            P.AutoPlayChanged((bool)e.NewValue);
        }
        private static object CoerceAutoPlay(DependencyObject d, object baseValue) {
            PlayerHostBase P = d as PlayerHostBase;
            return P.CoerceAutoPlay((bool)baseValue);
        }
        protected virtual void AutoPlayChanged(bool value) {
            // AutoPlay can be set AFTER Script, we need to reset IsPlaying in that case.
            // Default value needs to be false otherwise it can cause video to start loading and immediately stop which can cause issues.
            IsPlaying = value;
        }
        protected virtual bool CoerceAutoPlay(bool value) => value;

        // Text
        public static readonly DependencyPropertyKey TextProperty = DependencyProperty.RegisterReadOnly("Text", typeof(string), typeof(PlayerHostBase), null);
        public string Text { get => (string)GetValue(TextProperty.DependencyProperty); protected set => SetValue(TextProperty, value); }

        // IsMediaLoaded
        public static readonly DependencyPropertyKey IsMediaLoadedPropertyKey = DependencyProperty.RegisterReadOnly("IsMediaLoaded", typeof(bool), typeof(PlayerHostBase),
            new PropertyMetadata(false));
        private static readonly DependencyProperty IsMediaLoadedProperty = IsMediaLoadedPropertyKey.DependencyProperty;
        public bool IsMediaLoaded { get => (bool)GetValue(IsMediaLoadedProperty); private set => SetValue(IsMediaLoadedPropertyKey, value); }

        // IsVideoVisible
        public static readonly DependencyProperty IsVideoVisibleProperty = DependencyProperty.Register("IsVideoVisible", typeof(bool), typeof(PlayerHostBase),
            new PropertyMetadata(false, IsVideoVisibleChanged, CoerceIsVideoVisible));
        public bool IsVideoVisible { get => (bool)GetValue(IsVideoVisibleProperty); set => SetValue(IsVideoVisibleProperty, value); }
        private static void IsVideoVisibleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            PlayerHostBase P = d as PlayerHostBase;
            P.IsVideoVisibleChanged((bool)e.NewValue);
        }
        private static object CoerceIsVideoVisible(DependencyObject d, object baseValue) {
            PlayerHostBase P = d as PlayerHostBase;
            return P.CoerceIsVideoVisible((bool)baseValue);
        }
        protected virtual void IsVideoVisibleChanged(bool value) {
            if (InnerControl != null)
                InnerControl.Visibility = value ? Visibility.Visible : Visibility.Hidden;
        }
        protected virtual bool CoerceIsVideoVisible(bool value) => value;


        #endregion


        #region Virtual Methods

        /// <summary>
        /// Sets the position without raising PositionChanged.
        /// </summary>
        /// <param name="pos">The position value to set.</param>
        protected virtual void SetPositionNoSeek(TimeSpan pos) {
            isSettingPosition = true;
            Position = pos;
            isSettingPosition = false;
        }

        /// <summary>
        /// Returns the media player control UI. This object will be transferred during full-screen mode.
        /// </summary>
        public virtual FrameworkElement InnerControl { get; }

        /// <summary>
        /// Stops playback and unloads media file.
        /// </summary>
        public virtual void Stop() {
            isStopping = true;
            // Use timer for Loop feature if player doesn't support it natively, but not after pressing Stop.
            stopTimer?.Stop();
            stopTimer?.Start();
        }

        /// <summary>
        /// Restarts playback.
        /// </summary>
        public virtual void Restart() { }

        #endregion


        #region Other Methods

        /// <summary>
        /// Must be called by the derived class when media is loaded.
        /// </summary>
        protected void RaiseMediaLoaded() {
            SetPositionNoSeek(TimeSpan.Zero);
            IsMediaLoaded = true;
            IsPlaying = AutoPlay;
            MediaLoaded?.Invoke(this, new EventArgs());
        }

        /// <summary>
        /// Must be called by the derived class when media is unloaded.
        /// </summary>
        protected void RaiseMediaUnloaded() {
            if (Loop && !isStopping)
                Restart();
            else {
                IsMediaLoaded = false;
                Duration = TimeSpan.FromSeconds(1);
                MediaUnloaded?.Invoke(this, new EventArgs());
            }
        }

        /// <summary>
        /// Returns the playback speed. If SpeedInt is defined (0-based), it will calculate a float value based on it, otherwise SpeedFloat (1-based) is returned.
        /// </summary>
        /// <returns></returns>
        public float GetSpeed() {
            if (SpeedInt == 0)
                return SpeedFloat;
            else {
                float Factor = SpeedInt / 8f;
                return Factor < 0 ? 1 / (1 - Factor) : 1 * (1 + Factor);
            }
        }

        /// <summary>
        /// Returns the container of InnerControl the first time it is called and maintain reference to that container.
        /// </summary>
        public Panel InnerControlParentCache {
            get {
                if (innerControlParentCache == null) {
                    if (InnerControl.Parent is Panel)
                        innerControlParentCache = InnerControl.Parent as Panel;
                    else
                        throw new InvalidCastException(string.Format("PlayerBase must be within a Panel or Grid control. Container is of type '{0}'.", InnerControl?.Parent?.GetType()));
                }
                if (innerControlParentCache == null)
                    throw new ArgumentNullException("ParentCache returned null.");
                return innerControlParentCache;
            }
        }

        #endregion

    }
}
