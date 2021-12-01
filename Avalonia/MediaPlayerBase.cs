using System;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Threading;
using HanumanInstitute.MediaPlayer.Avalonia.Helpers;
using HanumanInstitute.MediaPlayer.Avalonia.Helpers.Mvvm;

namespace HanumanInstitute.MediaPlayer.Avalonia
{
    /// <summary>
    /// Handles the business logic of MediaPlayer.
    /// MediaPlayer derives from this and handles only the UI.
    /// </summary>
    public abstract class MediaPlayerBase : ContentControl, IDisposable
    {
        private readonly TimedAction<TimeSpan> _positionBarTimedUpdate;

        public event EventHandler? PlayCommandExecuted;
        public event EventHandler? PauseCommandExecuted;
        public event EventHandler? StopCommandExecuted;
        public event EventHandler<ValueEventArgs<int>>? SeekCommandExecuted;
        public event EventHandler<ValueEventArgs<int>>? ChangeVolumeExecuted;

        protected bool IsSeekBarPressed { get; set; }

        protected MediaPlayerBase()
        {
            _positionBarTimedUpdate = new TimedAction<TimeSpan>(TimeSpan.FromMilliseconds(SeekMinInterval), (v) =>
            {
                if (PlayerHost != null)
                {
                    var old = IsSeekBarPressed;
                    IsSeekBarPressed = true;
                    PlayerHost.Position = v;
                    IsSeekBarPressed = old;
                }
            }, DispatcherPriority.Input);
        }

        public static readonly DirectProperty<MediaPlayerBase, PlayerHostBase?> PlayerHostProperty =
            AvaloniaProperty.RegisterDirect<MediaPlayerBase, PlayerHostBase?>(nameof(PlayerHost), o => o.PlayerHost);
        private PlayerHostBase? _playerHost;
        public PlayerHostBase? PlayerHost
        {
            get => _playerHost;
            protected set => SetAndRaise(PlayerHostProperty, ref _playerHost, value);
        }

        public void ContentHasChanged(AvaloniaPropertyChangedEventArgs e)
        {
            PlayerHost = e.NewValue as PlayerHostBase;
            if (e.OldValue is PlayerHostBase oldValue)
            {
                oldValue.MediaLoaded -= Player_MediaLoaded;
                oldValue.MediaUnloaded -= Player_MediaUnloaded;
            }
            if (e.NewValue is PlayerHostBase newValue)
            {
                newValue.MediaLoaded += Player_MediaLoaded;
                newValue.MediaUnloaded += Player_MediaUnloaded;
                newValue.GetObservable(PlayerHostBase.PositionProperty).Subscribe(Player_PositionChanged);
                newValue.GetObservable(PlayerHostBase.IsPlayingProperty).Subscribe(Player_IsPlayingChanged);
            }
            OnContentChanged(e);
        }
        // Allow derived class to bind to new host.
        protected virtual void OnContentChanged(AvaloniaPropertyChangedEventArgs e) { }

        // Expose IsPlaying from PlayerHost so that it can be bound to styles.
        public static readonly DirectProperty<MediaPlayer, bool> IsPlayingProperty =
            AvaloniaProperty.RegisterDirect<MediaPlayer, bool>(nameof(IsPlaying), o => o.IsPlaying);
        public bool IsPlaying =>  PlayerHost?.IsPlaying ?? false;

        private void Player_IsPlayingChanged(bool value) => RaisePropertyChanged(IsPlayingProperty, !value, value, BindingPriority.LocalValue);
        
        public RelayCommand PlayPauseCommand => _playPauseCommand ??= new RelayCommand(PlayPause, CanPlayPause);
        private RelayCommand? _playPauseCommand;
        private bool CanPlayPause() => PlayerHost?.IsMediaLoaded ?? false;
        private void PlayPause()
        {
            if (PlayerHost == null) { return; }

            PlayerHost.IsPlaying = !PlayerHost.IsPlaying;
            if (PlayerHost.IsPlaying)
            {
                PlayCommandExecuted?.Invoke(this, EventArgs.Empty);
            }
            else
            {
                PauseCommandExecuted?.Invoke(this, EventArgs.Empty);
            }
        }

        public RelayCommand StopCommand => _stopCommand ??= new RelayCommand(Stop, CanStop);
        private RelayCommand? _stopCommand;
        private bool CanStop() => PlayerHost?.IsMediaLoaded ?? false;
        private void Stop()
        {
            if (PlayerHost == null) { return; }

            PlayerHost.Stop();
            StopCommandExecuted?.Invoke(this, EventArgs.Empty);
        }

        public RelayCommand<int> SeekCommand => _seekCommand ??= new RelayCommand<int>(Seek, CanSeek);
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

        public ICommand ChangeVolumeCommand => _changeVolumeCommand ??= new RelayCommand<int>(ChangeVolume, CanChangeVolume);
        private RelayCommand<int>? _changeVolumeCommand;
        private bool CanChangeVolume(int value) => true;
        private void ChangeVolume(int value)
        {
            if (PlayerHost == null) { return; }

            PlayerHost.Volume += value;
            ChangeVolumeExecuted?.Invoke(this, new ValueEventArgs<int>(value));
        }

        // PositionBar
        public static readonly DirectProperty<MediaPlayerBase, TimeSpan> PositionBarProperty =
            AvaloniaProperty.RegisterDirect<MediaPlayerBase, TimeSpan>(nameof(PositionBar),
                o => o.PositionBar, (o, v) => o.PositionBar = v);
        private TimeSpan _positionBar = TimeSpan.Zero;
        public TimeSpan PositionBar
        {
            get => _positionBar;
            set
            {
                SetAndRaise(PositionBarProperty, ref _positionBar, CoercePositionBar(value));
                if (IsSeekBarPressed && PlayerHost != null)
                {
                    // _positionBarTimedUpdate.ExecuteAtInterval(PositionBar);
                    PlayerHost.Position = PositionBar;
                }
            }
        }
        private TimeSpan CoercePositionBar(TimeSpan value)
        {
            var dur = PlayerHost?.Duration ?? TimeSpan.Zero;
            return TimeSpan.FromTicks(Math.Max(0, Math.Min(dur.Ticks, value.Ticks)));
        }

        // PositionDisplay
        public static readonly StyledProperty<TimeDisplayStyle> PositionDisplayProperty =
            AvaloniaProperty.Register<MediaPlayerBase, TimeDisplayStyle>(nameof(PositionDisplay),
                TimeDisplayStyle.Standard);
        public TimeDisplayStyle PositionDisplay
        {
            get => GetValue(PositionDisplayProperty);
            set => SetValue(PositionDisplayProperty, value);
        }

        // PositionText
        public static readonly DirectProperty<MediaPlayerBase, string> PositionTextProperty =
            AvaloniaProperty.RegisterDirect<MediaPlayerBase, string>(nameof(PositionText), o => o.PositionText);
        private string _positionText = string.Empty;
        public string PositionText
        {
            get => _positionText;
            private set => SetAndRaise(PositionTextProperty, ref _positionText, value);
        }

        // SeekMinInterval
        public static readonly DirectProperty<MediaPlayerBase, int> SeekMinIntervalProperty =
            AvaloniaProperty.RegisterDirect<MediaPlayerBase, int>(nameof(SeekMinInterval),
                o => o.SeekMinInterval);
        private int _seekMinInterval = 500;
        public int SeekMinInterval
        {
            get => _seekMinInterval;
            set
            {
                _seekMinInterval = value < 1 ? 1 : value;
                _positionBarTimedUpdate.MinInterval = TimeSpan.FromMilliseconds(_seekMinInterval);
            }
        }

        private void Player_PositionChanged(TimeSpan value)
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
            MediaLoadedChanged();
            Player_PositionChanged(TimeSpan.Zero);
        }

        private void Player_MediaUnloaded(object? sender, EventArgs e)
        {
            MediaLoadedChanged();
            UpdatePositionText();
            PositionBar = TimeSpan.Zero;
        }

        private void MediaLoadedChanged()
        {
            PlayPauseCommand.RaiseCanExecuteChanged();
            StopCommand.RaiseCanExecuteChanged();
            SeekCommand.RaiseCanExecuteChanged();
        }

        private string FormatTime(TimeSpan t)
        {
            return PositionDisplay switch
            {
                TimeDisplayStyle.Standard when t.TotalHours >= 1 => t.ToString("h\\:mm\\:ss",
                    CultureInfo.InvariantCulture),
                TimeDisplayStyle.Standard => t.ToString("m\\:ss", CultureInfo.InvariantCulture),
                TimeDisplayStyle.Seconds => t.TotalSeconds.ToString(CultureInfo.InvariantCulture),
                _ => string.Empty
            };
        }

        private bool _disposedValue;
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
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
