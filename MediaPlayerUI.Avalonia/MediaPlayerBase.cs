using System;
using System.Globalization;
using System.Windows.Input;
using HanumanInstitute.MediaPlayerUI.Avalonia.Helpers;
using HanumanInstitute.MediaPlayerUI.Avalonia.Helpers.Mvvm;
using Avalonia;
using Avalonia.Controls;

namespace HanumanInstitute.MediaPlayerUI.Avalonia
{
    /// <summary>
    /// Handles the business logic of MediaPlayer.
    /// MediaPlayer derives from this and handles only the UI.
    /// </summary>
    public abstract class MediaPlayerBase : ContentControl, IDisposable
    {
        public event EventHandler? PlayCommandExecuted;
        public event EventHandler? PauseCommandExecuted;
        public event EventHandler? StopCommandExecuted;
        public event EventHandler<ValueEventArgs<int>>? SeekCommandExecuted;
        public event EventHandler<ValueEventArgs<int>>? ChangeVolumeExecuted;

        protected bool IsSeekBarPressed { get; set; }

        public static readonly DirectProperty<MediaPlayerBase, PlayerHostBase?> PlayerHostProperty =
            AvaloniaProperty.RegisterDirect<MediaPlayerBase, PlayerHostBase?>(nameof(PlayerHost), o => o.PlayerHost);
        public PlayerHostBase? PlayerHost { get; private set; }
        
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
                PlayerHostBase.PositionProperty.Changed.Subscribe(_ => Player_PositionChanged());
            }
            OnContentChanged(e);
        }

        // Allow derived class to bind to new host.
        protected virtual void OnContentChanged(AvaloniaPropertyChangedEventArgs e) { }
        
        public ICommand PlayPauseCommand => _playPauseCommand ??= new RelayCommand(PlayPause, CanPlayPause);
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

        public ICommand StopCommand => _stopCommand ??= new RelayCommand(Stop, CanStop);
        private RelayCommand? _stopCommand;
        private bool CanStop() => PlayerHost?.IsMediaLoaded ?? false;
        private void Stop()
        {
            if (PlayerHost == null) { return; }

            PlayerHost.Stop();
            StopCommandExecuted?.Invoke(this, EventArgs.Empty);
        }

        public ICommand SeekCommand => _seekCommand ??= new RelayCommand<int>(Seek, CanSeek);
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
        public static readonly StyledProperty<TimeSpan> PositionBarProperty =
            AvaloniaProperty.Register<MediaPlayerBase, TimeSpan>(nameof(PositionBar), TimeSpan.Zero, coerce: CoercePositionBar);
        public TimeSpan PositionBar { get => GetValue(PositionBarProperty); set => SetValue(PositionBarProperty, value); }
        private static TimeSpan CoercePositionBar(IAvaloniaObject d, TimeSpan value)
        {
            var p = (MediaPlayerBase)d;
            p.PlayerHost.CheckNotNull((nameof(p.PlayerHost)));

            if (value < TimeSpan.Zero)
            {
                value = TimeSpan.Zero;
            }

            if (value > p.PlayerHost!.Duration)
            {
                value = p.PlayerHost.Duration;
            }

            return value;
        }

        // PositionDisplay
        public static readonly StyledProperty<TimeDisplayStyle> PositionDisplayProperty =
            AvaloniaProperty.Register<MediaPlayerBase, TimeDisplayStyle>(nameof(PositionDisplay),
                TimeDisplayStyle.Standard);
        public TimeDisplayStyle PositionDisplay { get => GetValue(PositionDisplayProperty); set => SetValue(PositionDisplayProperty, value); }

        // PositionText
        public static readonly DirectProperty<MediaPlayerBase, string> PositionTextProperty =
            AvaloniaProperty.RegisterDirect<MediaPlayerBase, string>(nameof(PositionText), o => o.PositionText);

        public string PositionText { get; private set; } = string.Empty;

        private void Player_PositionChanged()
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
            Player_PositionChanged();
            // CommandManager.InvalidateRequerySuggested();
        }

        private void Player_MediaUnloaded(object? sender, EventArgs e)
        {
            UpdatePositionText();
            // CommandManager.InvalidateRequerySuggested();
            PositionBar = TimeSpan.Zero;
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
