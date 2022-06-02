using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using HanumanInstitute.MediaPlayer.Wpf.Mvvm;
// ReSharper disable InconsistentNaming

namespace HanumanInstitute.MediaPlayer.Wpf;

/// <summary>
/// Handles the business logic of MediaPlayer.
/// MediaPlayer derives from this and handles only the UI.
/// </summary>
public abstract class MediaPlayerBase : ContentControl, IDisposable
{
    /// <summary>
    /// Occurs when the Play command is executed.
    /// </summary>
    public event EventHandler? PlayCommandExecuted;
    /// <summary>
    /// Occurs when the Pause command is executed.
    /// </summary>
    public event EventHandler? PauseCommandExecuted;
    /// <summary>
    /// Occurs when the Stop command is executed.
    /// </summary>
    public event EventHandler? StopCommandExecuted;
    /// <summary>
    /// Occurs when the Seek command is executed.
    /// </summary>
    public event EventHandler<ValueEventArgs<int>>? SeekCommandExecuted;
    /// <summary>
    /// Occurs when the ChangeVolume command is executed.
    /// </summary>
    public event EventHandler<ValueEventArgs<int>>? ChangeVolumeExecuted;

    /// <summary>
    /// Gets or sets whether the user is dragging the seek bar.
    /// </summary>
    protected bool IsSeekBarPressed { get; set; }
    private PropertyChangeNotifier? _positionChangedNotifier;

    private PlayerHostBase? _playerHost; // Cache to avoid constant casting.
    /// <summary>
    /// Gets the PlayerHostBase contained within the MediaPlayer.
    /// </summary>
    public PlayerHostBase? PlayerHost => _playerHost ??= Content as PlayerHostBase;

    /// <summary>
    /// Called by derived class when a PlayerHostBase is set as content. 
    /// </summary>
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

    /// <summary>
    /// Allow derived class to bind to new host.
    /// </summary>
    protected virtual void OnContentChanged(DependencyPropertyChangedEventArgs e) { }

    /// <summary>
    /// Toggle between play and pause status.
    /// </summary>
    public ICommand PlayPauseCommand => CommandHelper.InitCommand(ref _playPauseCommand, PlayPause, CanPlayPause);
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

    /// <summary>
    /// Stops playback.
    /// </summary>
    public ICommand StopCommand => CommandHelper.InitCommand(ref _stopCommand, Stop, CanStop);
    private RelayCommand? _stopCommand;
    private bool CanStop() => PlayerHost?.IsMediaLoaded ?? false;
    private void Stop()
    {
        if (PlayerHost == null) { return; }

        PlayerHost.Stop();
        StopCommandExecuted?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Seeks to specified position.
    /// </summary>
    public ICommand SeekCommand => CommandHelper.InitCommand(ref _seekCommand, Seek, CanSeek);
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

    /// <summary>
    /// Adds specified value to the volume. Volume is an integer between 0 and 100. Calling this with a value of 5 will increase volume by 5. 
    /// </summary>
    public ICommand ChangeVolumeCommand => CommandHelper.InitCommand(ref _changeVolumeCommand, ChangeVolume, CanChangeVolume);
    private RelayCommand<int>? _changeVolumeCommand;
    private bool CanChangeVolume(int value) => true;
    private void ChangeVolume(int value)
    {
        if (PlayerHost == null) { return; }

        PlayerHost.Volume += value;
        ChangeVolumeExecuted?.Invoke(this, new ValueEventArgs<int>(value));
    }

    /// <summary>
    /// Defines the PositionBar property.
    /// </summary>
    public static readonly DependencyProperty PositionBarProperty = DependencyProperty.Register("PositionBar", typeof(TimeSpan), typeof(MediaPlayerBase),
        new PropertyMetadata(TimeSpan.Zero, null, CoercePositionBar));
    /// <summary>
    /// Gets or sets the visual position of the seek bar.
    /// </summary>
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

    /// <summary>
    /// Defines the PositionDisplay property.
    /// </summary>
    public static readonly DependencyProperty PositionDisplayProperty = DependencyProperty.Register("PositionDisplay", typeof(TimeDisplayStyle), typeof(MediaPlayerBase),
        new PropertyMetadata(TimeDisplayStyle.Standard));
    /// <summary>
    /// Gets or sets how time is displayed within the player.
    /// </summary>
    public TimeDisplayStyle PositionDisplay { get => (TimeDisplayStyle)GetValue(PositionDisplayProperty); set => SetValue(PositionDisplayProperty, value); }

    /// <summary>
    /// Defines the PositionText property.
    /// </summary>
    public static readonly DependencyPropertyKey PositionTextPropertyKey = DependencyProperty.RegisterReadOnly("PositionText", typeof(string), typeof(MediaPlayerBase),
        new PropertyMetadata(null));
    private static readonly DependencyProperty s_positionTextProperty = PositionTextPropertyKey.DependencyProperty;
    /// <summary>
    /// Gets the position text. 
    /// </summary>
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

    private bool _disposedValue;
    /// <summary>
    /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
    /// </summary>
    /// <param name="disposing">The disposing parameter should be false when called from a finalizer, and true when called from the IDisposable.Dispose method.</param>
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

    /// <inheritdoc />
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}
