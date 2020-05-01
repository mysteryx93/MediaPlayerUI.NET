using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using HanumanInstitute.MediaPlayerUI.Mvvm;

namespace HanumanInstitute.MediaPlayerUI
{
    /// <summary>
    /// Handles the business logic of MediaPlayer.
    /// MediaPlayer derives from this and handles only the UI.
    /// </summary>
    public abstract class MediaPlayerBase : ContentControl, IDisposable
    {
        public MediaPlayerBase()
        { }

        public event EventHandler? PlayCommandExecuted;
        public event EventHandler? PauseCommandExecuted;
        public event EventHandler? StopCommandExecuted;
        public event EventHandler<ValueEventArgs<int>>? SeekCommandExecuted;
        public event EventHandler<ValueEventArgs<int>>? ChangeVolumeExecuted;

        protected bool IsSeekBarPressed { get; set; } = false;
        private PropertyChangeNotifier? _positionChangedNotifier;

        private PlayerHostBase? _playerHost; // Cache to avoid constant casting.
        public PlayerHostBase? PlayerHost => _playerHost ?? (_playerHost = Content as PlayerHostBase);

        protected static void OnContentChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var p = (MediaPlayerBase)d.CheckNotNull(nameof(d));
            if (e.OldValue is PlayerHostBase oldValue)
            {
                oldValue.MediaLoaded -= p.Player_MediaLoaded;
                oldValue.MediaUnloaded -= p.Player_MediaUnloaded;
                p._positionChangedNotifier!.ValueChanged -= p.Player_PositionChanged;
                p._positionChangedNotifier = null;
            }
            if (e.NewValue is PlayerHostBase newValue)
            {
                newValue.MediaLoaded += p.Player_MediaLoaded;
                newValue.MediaUnloaded += p.Player_MediaUnloaded;
                p._positionChangedNotifier = new PropertyChangeNotifier(newValue, "Position");
                p._positionChangedNotifier.ValueChanged += p.Player_PositionChanged;
            }
            p.OnContentChanged(e);
        }

        // Allow derived class to bind to new host.
        protected virtual void OnContentChanged(DependencyPropertyChangedEventArgs e) { }


        public ICommand PlayPauseCommand => CommandHelper.InitCommand(ref _playPauseCommand, PlayPause, CanPlayPause);
        private RelayCommand? _playPauseCommand;
        private bool CanPlayPause() => PlayerHost?.IsMediaLoaded ?? false;
        private void PlayPause()
        {
            if (PlayerHost == null) { return; }

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

        public ICommand StopCommand => CommandHelper.InitCommand(ref _stopCommand, Stop, CanStop);
        private RelayCommand? _stopCommand;
        private bool CanStop() => PlayerHost?.IsMediaLoaded ?? false;
        private void Stop()
        {
            if (PlayerHost == null) { return; }

            PlayerHost.Stop();
            StopCommandExecuted?.Invoke(this, new EventArgs());
        }

        public ICommand SeekCommand => CommandHelper.InitCommand<int>(ref _seekCommand, Seek, CanSeek);
        private RelayCommand<int>? _seekCommand;
        private bool CanSeek(int seconds) => PlayerHost?.IsMediaLoaded ?? false;
        private void Seek(int seconds)
        {
            if (PlayerHost == null) { return; }

            var newPos = PlayerHost.Position.Add(TimeSpan.FromSeconds(seconds));
            if (newPos < TimeSpan.Zero)
            {
                newPos = TimeSpan.Zero;
            }
            else if (newPos > PlayerHost.Duration)
            {
                newPos = PlayerHost.Duration;
            }

            if (newPos != PlayerHost.Position)
            {
                PositionBar = newPos;
                PlayerHost.Position = newPos;
            }
            SeekCommandExecuted?.Invoke(this, new ValueEventArgs<int>(seconds));
        }

        public ICommand ChangeVolumeCommand => CommandHelper.InitCommand<int>(ref _changeVolumeCommand, ChangeVolume, CanChangeVolume);
        private RelayCommand<int>? _changeVolumeCommand;
        private bool CanChangeVolume(int value) => true;
        private void ChangeVolume(int value)
        {
            if (PlayerHost == null) { return; }

            PlayerHost.Volume += value;
            ChangeVolumeExecuted?.Invoke(this, new ValueEventArgs<int>(value));
        }

        // PositionBar
        public static readonly DependencyProperty PositionBarProperty = DependencyProperty.Register("PositionBar", typeof(TimeSpan), typeof(MediaPlayerBase),
            new PropertyMetadata(TimeSpan.Zero, null, CoercePositionBar));
        public TimeSpan PositionBar { get => (TimeSpan)GetValue(PositionBarProperty); set => SetValue(PositionBarProperty, value); }
        private static object CoercePositionBar(DependencyObject d, object value)
        {
            var p = (MediaPlayerBase)d;
            var pos = (TimeSpan)value;
            if (p.PlayerHost == null)
            {
                return DependencyProperty.UnsetValue;
            }

            if (pos < TimeSpan.Zero)
            {
                pos = TimeSpan.Zero;
            }

            if (pos > p.PlayerHost.Duration)
            {
                pos = p.PlayerHost.Duration;
            }

            return pos;
        }

        // PositionDisplay
        public static readonly DependencyProperty PositionDisplayProperty = DependencyProperty.Register("PositionDisplay", typeof(TimeDisplayStyle), typeof(MediaPlayerBase),
            new PropertyMetadata(TimeDisplayStyle.Standard));
        public TimeDisplayStyle PositionDisplay { get => (TimeDisplayStyle)GetValue(PositionDisplayProperty); set => SetValue(PositionDisplayProperty, value); }

        // PositionText
        public static readonly DependencyPropertyKey PositionTextPropertyKey = DependencyProperty.RegisterReadOnly("PositionText", typeof(string), typeof(MediaPlayerBase),
            new PropertyMetadata(null));
        private static readonly DependencyProperty s_positionTextProperty = PositionTextPropertyKey.DependencyProperty;
        public string PositionText { get => (string)GetValue(s_positionTextProperty); private set => SetValue(PositionTextPropertyKey, value); }


        private void Player_PositionChanged(object? sender, EventArgs e)
        {
            UpdatePositionText();
            if (!IsSeekBarPressed && PlayerHost != null)
            {
                PositionBar = PlayerHost.Position;
            }
        }

        private void UpdatePositionText()
        {
            if (PlayerHost?.IsMediaLoaded == true && PositionDisplay != TimeDisplayStyle.None)
            {
                PositionText = string.Format(CultureInfo.InvariantCulture, "{0} / {1}",
                    FormatTime(PlayerHost.Position),
                    FormatTime(PlayerHost.Duration));
            }
            else
            {
                PositionText = "";
            }
        }

        private void Player_MediaLoaded(object? sender, EventArgs e)
        {
            Player_PositionChanged(sender, e);
            CommandManager.InvalidateRequerySuggested();
        }

        private void Player_MediaUnloaded(object? sender, EventArgs e)
        {
            UpdatePositionText();
            CommandManager.InvalidateRequerySuggested();
            PositionBar = TimeSpan.Zero;
        }

        private string FormatTime(TimeSpan t)
        {
            if (PositionDisplay == TimeDisplayStyle.Standard)
            {
                if (t.TotalHours >= 1)
                {
                    return t.ToString("h\\:mm\\:ss", CultureInfo.InvariantCulture);
                }
                else
                {
                    return t.ToString("m\\:ss", CultureInfo.InvariantCulture);
                }
            }
            else if (PositionDisplay == TimeDisplayStyle.Seconds)
            {
                return t.TotalSeconds.ToString(CultureInfo.InvariantCulture);
            }
            else
            {
                return string.Empty;
            }
        }

        private bool _disposedValue = false;
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    _positionChangedNotifier?.Dispose();
                }
                _disposedValue = true;
            }
        }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
