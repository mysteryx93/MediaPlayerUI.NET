using EmergenceGuardian.MediaPlayerUI.Mvvm;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace EmergenceGuardian.MediaPlayerUI
{
    public class PlayerUIBase : Control
    {

        #region Declarations / Constructor

        private bool isSeekBarButtonDown = false;
        private PropertyChangeNotifier PositionChangedNotifier;

        public PlayerUIBase()
        {
        }

        #endregion


        #region Commands

        public event EventHandler PlayCommandExecuted;
        public event EventHandler PauseCommandExecuted;
        public event EventHandler StopCommandExecuted;
        public event EventHandler<int> SeekCommandExecuted;
        public event EventHandler<int> ChangeVolumeExecuted;

        public ICommand PlayPauseCommand => CommandHelper.InitCommand(ref playPauseCommand, PlayPause, CanPlayPause);
        private ICommand playPauseCommand;
        private bool CanPlayPause() => PlayerHost?.IsMediaLoaded ?? false;
        private void PlayPause()
        {
            PlayerHost.IsPlaying = !PlayerHost.IsPlaying;
            if (PlayerHost.IsPlaying)
            {
                PlayCommandExecuted?.Invoke(this, new EventArgs());
            }
            else
            {
                PauseCommandExecuted?.Invoke(this, new EventArgs());
            }
        }

        public ICommand StopCommand => CommandHelper.InitCommand(ref stopCommand, Stop, CanStop);
        private ICommand stopCommand;
        private bool CanStop() => PlayerHost?.IsMediaLoaded ?? false;
        private void Stop()
        {
            PlayerHost.Stop();
            StopCommandExecuted?.Invoke(this, new EventArgs());
        }

        public ICommand SeekCommand => CommandHelper.InitCommand<int>(ref seekCommand, Seek, CanSeek);
        private ICommand seekCommand;
        private bool CanSeek(int seconds) => PlayerHost?.IsMediaLoaded ?? false;
        private void Seek(int seconds)
        {
            TimeSpan NewPos = PlayerHost.Position.Add(TimeSpan.FromSeconds(seconds));
            if (NewPos < TimeSpan.Zero)
                NewPos = TimeSpan.Zero;
            else if (NewPos > PlayerHost.Duration)
                NewPos = PlayerHost.Duration;
            if (NewPos != PlayerHost.Position)
            {
                PositionBar = NewPos;
                PlayerHost.Position = NewPos;
            }
            SeekCommandExecuted?.Invoke(this, seconds);
        }

        public ICommand ChangeVolumeCommand => CommandHelper.InitCommand<int>(ref changeVolumeCommand, ChangeVolume, CanChangeVolume);
        private ICommand changeVolumeCommand;
        private bool CanChangeVolume(int value) => true;
        private void ChangeVolume(int value)
        {
            PlayerHost.Volume = PlayerHost.Volume + value;
            ChangeVolumeExecuted?.Invoke(this, value);
        }

        #endregion


        #region Dependency Properties

        // PlayerHost
        public static readonly DependencyProperty PlayerHostProperty = DependencyProperty.Register("PlayerHost", typeof(PlayerBase), typeof(PlayerUIBase),
            new PropertyMetadata(null, OnPlayerHostChanged));
        public PlayerBase PlayerHost { get => (PlayerBase)GetValue(PlayerHostProperty); set => SetValue(PlayerHostProperty, value); }

        // PositionBar
        public static readonly DependencyProperty PositionBarProperty = DependencyProperty.Register("PositionBar", typeof(TimeSpan), typeof(PlayerUIBase),
            new PropertyMetadata(TimeSpan.Zero, null, CoercePositionBar));
        public TimeSpan PositionBar { get => (TimeSpan)GetValue(PositionBarProperty); set => SetValue(PositionBarProperty, value); }
        private static object CoercePositionBar(DependencyObject d, object value)
        {
            PlayerUIBase P = d as PlayerUIBase;
            TimeSpan Pos = (TimeSpan)value;
            if (P.PlayerHost == null)
                return DependencyProperty.UnsetValue;

            if (Pos < TimeSpan.Zero)
                Pos = TimeSpan.Zero;
            if (Pos > P.PlayerHost.Duration)
                Pos = P.PlayerHost.Duration;
            return Pos;
        }

        // PositionDisplay
        public static readonly DependencyProperty PositionDisplayProperty = DependencyProperty.Register("PositionDisplay", typeof(TimeDisplayStyles), typeof(PlayerUIBase),
            new PropertyMetadata(TimeDisplayStyles.Standard));
        public TimeDisplayStyles PositionDisplay { get => (TimeDisplayStyles)GetValue(PositionDisplayProperty); set => SetValue(PositionDisplayProperty, value); }

        // PositionText
        public static readonly DependencyPropertyKey PositionTextPropertyKey = DependencyProperty.RegisterReadOnly("PositionText", typeof(string), typeof(PlayerUIBase),
            new PropertyMetadata(null));
        private static readonly DependencyProperty PositionTextProperty = PositionTextPropertyKey.DependencyProperty;
        public string PositionText { get => (string)GetValue(PositionTextProperty); private set => SetValue(PositionTextPropertyKey, value); }

        #endregion


        #region Events

        private static void OnPlayerHostChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            PlayerUIBase P = d as PlayerUIBase;
            if (e.OldValue is PlayerBase OldValue)
            {
                OldValue.MediaLoaded -= P.Player_MediaLoaded;
                OldValue.MediaUnloaded -= P.Player_MediaUnloaded;
                P.PositionChangedNotifier.ValueChanged -= P.Player_PositionChanged;
                P.PositionChangedNotifier = null;
            }
            if (e.NewValue is PlayerBase NewValue)
            {
                NewValue.MediaLoaded += P.Player_MediaLoaded;
                NewValue.MediaUnloaded += P.Player_MediaUnloaded;
                P.PositionChangedNotifier = new PropertyChangeNotifier(NewValue, "Position");
                P.PositionChangedNotifier.ValueChanged += P.Player_PositionChanged;
            }
            P.OnPlayerHostChanged(e);
        }

        // Allow derived class to bind to new host.
        protected virtual void OnPlayerHostChanged(DependencyPropertyChangedEventArgs e) { }

        private void Player_PositionChanged(object sender, EventArgs e)
        {
            UpdatePositionText();
            if (!isSeekBarButtonDown)
                PositionBar = PlayerHost.Position;
        }

        private void UpdatePositionText()
        {
            if (PlayerHost != null && PlayerHost.IsMediaLoaded && PositionDisplay != TimeDisplayStyles.None)
                PositionText = string.Format("{0} / {1}",
                    FormatTime(PlayerHost.Position),
                    FormatTime(PlayerHost.Duration));
            else
                PositionText = "";
        }

        private void Player_MediaLoaded(object sender, EventArgs e)
        {
            Player_PositionChanged(sender, e);
            CommandManager.InvalidateRequerySuggested();
        }

        private void Player_MediaUnloaded(object sender, EventArgs e)
        {
            UpdatePositionText();
            CommandManager.InvalidateRequerySuggested();
            PositionBar = TimeSpan.Zero;
        }

        public void SeekBar_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Only process event if click is not on thumb.
            Thumb Obj = GetSliderThumb(sender as Slider);
            Point Pos = e.GetPosition(Obj);
            if (Pos.X < 0 || Pos.Y < 0 || Pos.X > Obj.ActualWidth || Pos.Y > Obj.ActualHeight)
            {
                // Immediate seek when clicking elsewhere.
                isSeekBarButtonDown = true;
                PlayerHost.Position = PositionBar;
                isSeekBarButtonDown = false;
            }
        }

        public void SeekBar_DragStarted(object sender, DragStartedEventArgs e)
        {
            isSeekBarButtonDown = true;
        }

        private DateTime lastDragCompleted = DateTime.MinValue;
        public void SeekBar_DragCompleted(object sender, DragCompletedEventArgs e)
        {
            // DragCompleted can trigger multiple times after switching to/from fullscreen. Ignore multiple events within a second.
            if ((DateTime.Now - lastDragCompleted).TotalSeconds < 1)
                return;
            lastDragCompleted = DateTime.Now;

            PlayerHost.Position = PositionBar;
            isSeekBarButtonDown = false;
        }

        #endregion


        #region Helper Functions

        private string FormatTime(TimeSpan t)
        {
            if (PositionDisplay == TimeDisplayStyles.Standard)
            {
                if (t.TotalHours >= 1)
                    return t.ToString("h\\:mm\\:ss");
                else
                    return t.ToString("m\\:ss");
            }
            else if (PositionDisplay == TimeDisplayStyles.Seconds)
            {
                return t.TotalSeconds.ToString();
            }
            else
                return null;
        }

        public Thumb GetSliderThumb(Slider obj)
        {
            var track = obj.Template.FindName("PART_Track", obj) as Track;
            return track?.Thumb;
        }

        #endregion

    }
}
