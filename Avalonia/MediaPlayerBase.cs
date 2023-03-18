using System;
using System.Globalization;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using HanumanInstitute.MediaPlayer.Avalonia.Helpers;
using HanumanInstitute.MediaPlayer.Avalonia.Helpers.Mvvm;

// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable MemberCanBeProtected.Global

namespace HanumanInstitute.MediaPlayer.Avalonia;

/// <summary>
/// Handles the business logic of MediaPlayer.
/// MediaPlayer derives from this and handles only the UI.
/// </summary>
public abstract class MediaPlayerBase : ContentControl
{
    private readonly TimedAction<TimeSpan> _positionBarTimedUpdate;

    /// <summary>
    /// Registration for the <see cref="PlayCommandExecuted"/> routed event.
    /// </summary>
    public static readonly RoutedEvent<RoutedEventArgs> PlayCommandExecutedEvent =
        RoutedEvent.Register<MediaPlayerBase, RoutedEventArgs>(nameof(PlayCommandExecuted), RoutingStrategies.Direct);
    /// <summary>
    /// Occurs when the Play command is executed.
    /// </summary>
    public event EventHandler<RoutedEventArgs>? PlayCommandExecuted
    {
        add => AddHandler(PlayCommandExecutedEvent, value);
        remove => RemoveHandler(PlayCommandExecutedEvent, value);
    }

    /// <summary>
    /// Registration for the <see cref="PauseCommandExecuted"/> routed event.
    /// </summary>
    public static readonly RoutedEvent<RoutedEventArgs> PauseCommandExecutedEvent =
        RoutedEvent.Register<MediaPlayerBase, RoutedEventArgs>(nameof(PauseCommandExecuted), RoutingStrategies.Direct);
    /// <summary>
    /// Occurs when the Pause command is executed.
    /// </summary>
    public event EventHandler<RoutedEventArgs>? PauseCommandExecuted
    {
        add => AddHandler(PauseCommandExecutedEvent, value);
        remove => RemoveHandler(PauseCommandExecutedEvent, value);
    }

    /// <summary>
    /// Registration for the <see cref="StopCommandExecuted"/> routed event.
    /// </summary>
    public static readonly RoutedEvent<RoutedEventArgs> StopCommandExecutedEvent =
        RoutedEvent.Register<MediaPlayerBase, RoutedEventArgs>(nameof(StopCommandExecuted), RoutingStrategies.Direct);
    /// <summary>
    /// Occurs when the Stop command is executed.
    /// </summary>
    public event EventHandler<RoutedEventArgs>? StopCommandExecuted
    {
        add => AddHandler(StopCommandExecutedEvent, value);
        remove => RemoveHandler(StopCommandExecutedEvent, value);
    }

    /// <summary>
    /// Registration for the <see cref="SeekCommandExecuted"/> routed event.
    /// </summary>
    public static readonly RoutedEvent<ValueEventArgs<int>> SeekCommandExecutedEvent =
        RoutedEvent.Register<MediaPlayerBase, ValueEventArgs<int>>(nameof(SeekCommandExecuted), RoutingStrategies.Direct);
    /// <summary>
    /// Occurs when the Seek command is executed.
    /// </summary>
    public event EventHandler<ValueEventArgs<int>>? SeekCommandExecuted
    {
        add => AddHandler(SeekCommandExecutedEvent, value);
        remove => RemoveHandler(SeekCommandExecutedEvent, value);
    }

    /// <summary>
    /// Registration for the <see cref="ChangeVolumeExecuted"/> routed event.
    /// </summary>
    public static readonly RoutedEvent<ValueEventArgs<int>> ChangeVolumeExecutedEvent =
        RoutedEvent.Register<MediaPlayerBase, ValueEventArgs<int>>(nameof(ChangeVolumeExecuted), RoutingStrategies.Direct);
    /// <summary>
    /// Occurs when the ChangeVolume command is executed.
    /// </summary>
    public event EventHandler<ValueEventArgs<int>>? ChangeVolumeExecuted
    {
        add => AddHandler(ChangeVolumeExecutedEvent, value);
        remove => RemoveHandler(ChangeVolumeExecutedEvent, value);
    }

    /// <summary>
    /// Gets or sets whether the user is dragging the seek bar.
    /// </summary>
    protected bool IsSeekBarPressed { get; set; }

    /// <summary>
    /// Initializes a new instance of the MediaPlayerBase class.
    /// </summary>
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

    /// <summary>
    /// Defines the PlayerHost property. 
    /// </summary>
    public static readonly DirectProperty<MediaPlayerBase, PlayerHostBase?> PlayerHostProperty =
        AvaloniaProperty.RegisterDirect<MediaPlayerBase, PlayerHostBase?>(nameof(PlayerHost), o => o.PlayerHost);
    private PlayerHostBase? _playerHost;
    /// <summary>
    /// Gets the PlayerHostBase contained within the MediaPlayer.
    /// </summary>
    public PlayerHostBase? PlayerHost
    {
        get => _playerHost;
        protected set => SetAndRaise(PlayerHostProperty, ref _playerHost, value);
    }

    /// <summary>
    /// Called by derived class when a PlayerHostBase is set as content. 
    /// </summary>
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
    /// <summary>
    /// Allow derived class to bind to new host.
    /// </summary>
    protected virtual void OnContentChanged(AvaloniaPropertyChangedEventArgs e) { }

    /// <summary>
    /// Defines the IsPlaying property.
    /// </summary>
    public static readonly DirectProperty<MediaPlayer, bool> IsPlayingProperty =
        AvaloniaProperty.RegisterDirect<MediaPlayer, bool>(nameof(IsPlaying), o => o.IsPlaying);
    /// <summary>
    /// Exposes IsPlaying from PlayerHost so that it can be bound to styles.
    /// </summary>
    public bool IsPlaying => PlayerHost?.IsPlaying ?? false;

    private void Player_IsPlayingChanged(bool value) =>
        RaisePropertyChanged(IsPlayingProperty, !value, value);

    /// <summary>
    /// Toggle between play and pause status.
    /// </summary>
    public ICommand PlayPauseCommand => _playPauseCommand ??= new RelayCommand(PlayPause, CanPlayPause);
    private RelayCommand? _playPauseCommand;
    private bool CanPlayPause() => PlayerHost?.IsMediaLoaded ?? false;
    private void PlayPause()
    {
        if (PlayerHost == null) { return; }

        PlayerHost.IsPlaying = !PlayerHost.IsPlaying;
        if (PlayerHost.IsPlaying)
        {
            RaiseEvent(new RoutedEventArgs(PlayCommandExecutedEvent));
        }
        else
        {
            RaiseEvent(new RoutedEventArgs(PauseCommandExecutedEvent));
        }
    }

    /// <summary>
    /// Stops playback.
    /// </summary>
    public ICommand StopCommand => _stopCommand ??= new RelayCommand(Stop, CanStop);
    private RelayCommand? _stopCommand;
    private bool CanStop() => PlayerHost?.IsMediaLoaded ?? false;
    private void Stop()
    {
        if (PlayerHost == null) { return; }

        PlayerHost.Stop();
        RaiseEvent(new RoutedEventArgs(StopCommandExecutedEvent));
    }

    /// <summary>
    /// Seeks to specified position.
    /// </summary>
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
        RaiseEvent(new ValueEventArgs<int>(SeekCommandExecutedEvent, seconds));
    }

    /// <summary>
    /// Adds specified value to the volume. Volume is an integer between 0 and 100. Calling this with a value of 5 will increase volume by 5. 
    /// </summary>
    public ICommand ChangeVolumeCommand => _changeVolumeCommand ??= new RelayCommand<int>(ChangeVolume, CanChangeVolume);
    private RelayCommand<int>? _changeVolumeCommand;
    private bool CanChangeVolume(int value) => true;
    private void ChangeVolume(int value)
    {
        if (PlayerHost == null) { return; }

        PlayerHost.Volume += value;
        RaiseEvent(new ValueEventArgs<int>(ChangeVolumeExecutedEvent, value));
    }

    /// <summary>
    /// Defines the PositionBar property.
    /// </summary>
    public static readonly DirectProperty<MediaPlayerBase, TimeSpan> PositionBarProperty =
        AvaloniaProperty.RegisterDirect<MediaPlayerBase, TimeSpan>(nameof(PositionBar),
            o => o.PositionBar, (o, v) => o.PositionBar = v);
    private TimeSpan _positionBar = TimeSpan.Zero;
    /// <summary>
    /// Gets or sets the visual position of the seek bar.
    /// </summary>
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

    /// <summary>
    /// Defines the PositionDisplay property.
    /// </summary>
    public static readonly StyledProperty<TimeDisplayStyle> PositionDisplayProperty =
        AvaloniaProperty.Register<MediaPlayerBase, TimeDisplayStyle>(nameof(PositionDisplay),
            TimeDisplayStyle.Standard);
    /// <summary>
    /// Gets or sets how time is displayed within the player.
    /// </summary>
    public TimeDisplayStyle PositionDisplay
    {
        get => GetValue(PositionDisplayProperty);
        set => SetValue(PositionDisplayProperty, value);
    }

    /// <summary>
    /// Defines the PositionText property.
    /// </summary>
    public static readonly DirectProperty<MediaPlayerBase, string> PositionTextProperty =
        AvaloniaProperty.RegisterDirect<MediaPlayerBase, string>(nameof(PositionText), o => o.PositionText);
    private string _positionText = string.Empty;
    /// <summary>
    /// Gets the position text. 
    /// </summary>
    public string PositionText
    {
        get => _positionText;
        private set => SetAndRaise(PositionTextProperty, ref _positionText, value);
    }

    /// <summary>
    /// Defines the SeekMinInterval property.
    /// </summary>
    public static readonly DirectProperty<MediaPlayerBase, int> SeekMinIntervalProperty =
        AvaloniaProperty.RegisterDirect<MediaPlayerBase, int>(nameof(SeekMinInterval),
            o => o.SeekMinInterval);
    private int _seekMinInterval = 500;
    /// <summary>
    /// Gets or sets the interval in milliseconds between consecutive seeks.
    /// </summary>
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
        _playPauseCommand?.RaiseCanExecuteChanged();
        _stopCommand?.RaiseCanExecuteChanged();
        _seekCommand?.RaiseCanExecuteChanged();
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
}
