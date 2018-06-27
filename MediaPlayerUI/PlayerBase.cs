using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace EmergenceGuardian.MediaPlayerUI {
    public class PlayerBase : Control, INotifyPropertyChanged {
        public event PropertyChangedEventHandler PropertyChanged;
        public void RaisePropertyChanged(string propertyName) {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public virtual TimeSpan Position { get; set; }
        public virtual TimeSpan Duration { get; private set; }
        public virtual bool IsPlaying { get; set; }
        public virtual int Volume { get; set; }
        public virtual float SpeedFloat { get; set; } = 1;
        public virtual int SpeedInt { get; set; }
        public virtual bool Loop { get; set; }
        public virtual FrameworkElement InnerControl { get; }

        public virtual void Load(string source) { }
        public virtual void Stop() {
            isStopping = true;
            stopTimer.Stop();
            stopTimer.Start();
        }
        public virtual void Restart() { }

        // AutoPlay
        public static DependencyProperty AutoPlayProperty = DependencyProperty.Register("AutoPlay", typeof(bool), typeof(PlayerBase),
            new PropertyMetadata(false, AutoPlayChanged));
        public bool AutoPlay { get => (bool)GetValue(AutoPlayProperty); set => SetValue(AutoPlayProperty, value); }
        private static void AutoPlayChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            // AutoPlay can be set AFTER Script, we need to reset IsPlaying in that case.
            // Default value needs to be false otherwise it can cause video to start loading and immediately stop which can cause issues.
            PlayerBase P = d as PlayerBase;
            P.IsPlaying = (bool)e.NewValue;
        }

        // Title
        public static DependencyProperty TitleProperty = DependencyProperty.Register("Title", typeof(string), typeof(PlayerBase));
        public string Title { get => (string)GetValue(TitleProperty); set => SetValue(TitleProperty, value); }

        // IsMediaLoaded
        public static DependencyPropertyKey IsMediaLoadedPropertyKey = DependencyProperty.RegisterReadOnly("IsMediaLoaded", typeof(bool), typeof(PlayerBase),
            new PropertyMetadata(false));
        public static DependencyProperty IsMediaLoadedProperty = IsMediaLoadedPropertyKey.DependencyProperty;
        public bool IsMediaLoaded { get => (bool)GetValue(IsMediaLoadedProperty); private set => SetValue(IsMediaLoadedPropertyKey, value); }


        // Restart won't be triggered after Stop while this timer is running.
        private bool isStopping = false;
        private Timer stopTimer = new Timer(1000);

        public event EventHandler OnMediaLoaded;
        public event EventHandler OnMediaUnloaded;

        public PlayerBase() {
            stopTimer.AutoReset = false;
            stopTimer.Elapsed += (o, e) => isStopping = false;

        }

        protected void MediaLoaded() {
            Position = TimeSpan.Zero;
            RaisePropertyChanged("Duration");
            IsMediaLoaded = true;
            RaisePropertyChanged("IsMediaLoaded");
            IsPlaying = AutoPlay;
            OnMediaLoaded?.Invoke(this, new EventArgs());
        }

        protected void MediaUnloaded() {
            IsMediaLoaded = false;
            RaisePropertyChanged("IsMediaLoaded");
            Duration = TimeSpan.FromSeconds(1);
            if (Loop && !isStopping)
                Restart();
            OnMediaUnloaded?.Invoke(this, new EventArgs());
        }

        protected void PositionChanged() {
            RaisePropertyChanged("Position");
        }
        
        protected int speedInt;
        protected float speedFloat = 1;

        public float GetSpeed() {
            if (SpeedInt == 0)
                return speedFloat;
            else {
                float Factor = speedInt / 8f;
                return Factor < 0 ? 1 / (1 - Factor) : 1 * (1 + Factor);
            }
        }

        private Panel innerControlParentCache;

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
    }
}
