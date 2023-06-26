using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;
using HanumanInstitute.LibMpv;
using HanumanInstitute.LibMpv.Avalonia;
using HanumanInstitute.LibMpv.Core;

// ReSharper disable CompareOfFloatsByEqualityOperator
// ReSharper disable InconsistentNaming

namespace HanumanInstitute.MediaPlayer.Avalonia.Mpv;

/// <summary>
/// MPV media player to be displayed within <see cref="MediaPlayer"/>.
/// </summary>
public class MpvPlayerHost : PlayerHostBase, IDisposable
{
    /// <summary>
    /// Initializes a new instance of the MpvPlayerHost class.
    /// </summary>
    public MpvPlayerHost()
    {
        PlayerView = new MpvView();
        this.VisualChildren.Add(PlayerView);
    }

    /// <summary>
    /// Releases MPV resources.
    /// </summary>
    ~MpvPlayerHost()
    {
        Dispose(false);
    }

    /// <summary>
    /// Gets the MpvView class instance.
    /// </summary>
    public MpvView PlayerView { get; private set; }

    /// <summary>
    /// Gets the MpvContext class instance.
    /// </summary>
    public MpvContext? Player { get; private set; }

    public bool IsMediaLoaded { get; private set; }

    // /// <summary>
    // /// Occurs after the media player is initialized.
    // /// </summary>
    // public event EventHandler? MediaPlayerInitialized;

    /// <summary>
    /// Occurs when a file playback ends. Reason contains the reason.  
    /// </summary>
    public event EventHandler<MpvEndFileEventArgs>? MediaEndFile;

    private bool _initLoaded;

    /// <inheritdoc />
    protected override void SourceChanged(string? value)
    {
        if (!IsLoaded || Design.IsDesignMode) { return; }
        
        if (!string.IsNullOrEmpty(value))
        {
            Status = PlaybackStatus.Loading;
            var _ = LoadMediaAsync();
        }
        else
        {
            Status = PlaybackStatus.Stopped;
            Player?.Stop().Invoke();
        }
    }

    /// <summary>
    /// Defines the Status property.
    /// </summary>
    public static readonly DirectProperty<MpvPlayerHost, PlaybackStatus> StatusProperty =
        AvaloniaProperty.RegisterDirect<MpvPlayerHost, PlaybackStatus>(nameof(Status), o => o.Status,
            (o, v) => o.Status = v);
    private PlaybackStatus _status = PlaybackStatus.Stopped;
    /// <summary>
    /// Gets the playback status of the media player.
    /// </summary>
    public PlaybackStatus Status
    {
        get => _status;
        protected set
        {
            SetAndRaise(StatusProperty, ref _status, value);
            SetDisplayText();
        }
    }

    protected override async void OnLoaded()
    {
        base.OnLoaded();

        await Task.Delay(100); // Fails to load if we don't give a slight delay.
        
        Player = PlayerView.MpvContext!;
        Player.FileLoaded += Player_FileLoaded;
        Player.EndFile += Player_EndFile;

        Player.TimePos.Changed += Player_PositionChanged;

        var options = new MpvAsyncOptions { WaitForResponse = false };
        await Player.Volume.SetAsync(base.Volume, options);
        await Player.Speed.SetAsync(base.GetSpeed(), options);
        await Player.LoopFile.SetAsync(base.Loop ? "yes" : "no", options);

        if (!string.IsNullOrEmpty(Source) && !_initLoaded)
        {
            await LoadMediaAsync();
        }
    }

    private void Player_FileLoaded(object sender, EventArgs e)
    {
        Dispatcher.UIThread.Post(() =>
        {
            IsMediaLoaded = true;
            Status = PlaybackStatus.Playing;
            base.Duration = TimeSpan.FromSeconds(Player!.Duration.Get()!.Value);
            base.OnMediaLoaded();
        });
    }

    private void Player_EndFile(object sender, MpvEndFileEventArgs e)
    {
        Dispatcher.UIThread.Post(() =>
        {
            IsMediaLoaded = false;
            if (e.Reason == MpvEndFileReason.Error)
            {
                Status = PlaybackStatus.Error;
            }
            base.OnMediaUnloaded();
        });
    }

    private void Player_PositionChanged(object sender, MpvValueChangedEventArgs<double, double> e)
    {
        Dispatcher.UIThread.Post(new Action(() => base.SetPositionNoSeek(TimeSpan.FromSeconds(e.NewValue!.Value))));
    }

    /// <inheritdoc />
    protected override void SetDisplayText()
    {
        Text = Status switch
        {
            PlaybackStatus.Loading => Properties.Resources.Loading,
            PlaybackStatus.Playing => Title ?? System.IO.Path.GetFileName(Source),
            PlaybackStatus.Error => Properties.Resources.MediaError,
            _ => ""
        };
    }

    /// <inheritdoc />
    protected override void PositionChanged(TimeSpan value, bool isSeeking)
    {
        base.PositionChanged(value, isSeeking);
        lock (Player!)
        {
            if (IsMediaLoaded && isSeeking)
            {
                Player.TimePos.Set(value.TotalSeconds);
            }
        }
    }

    /// <inheritdoc />
    protected override void IsPlayingChanged(bool value)
    {
        base.IsPlayingChanged(value);
        Player?.Pause.Set(!value);
    }

    /// <inheritdoc />
    protected override void VolumeChanged(int value)
    {
        base.VolumeChanged(value);
        Player?.Volume.Set(value);
    }

    /// <inheritdoc />
    protected override void SpeedChanged(double value)
    {
        base.SpeedChanged(value);
        Player?.Speed.Set(value);
    }

    /// <inheritdoc />
    protected override void LoopChanged(bool value)
    {
        base.LoopChanged(value);
        Player?.LoopFile.Set(value ? "yes" : "no");
    }

    private async Task LoadMediaAsync()
    {
        if (!IsLoaded || Design.IsDesignMode) { return; }
        
        Player!.Stop().Invoke();
        if (!string.IsNullOrEmpty(Source))
        {
            _initLoaded = true;
            Thread.Sleep(10);
            Player.Pause.Set(!base.AutoPlay);
            await Player.LoadFile(Source!).InvokeAsync();
        }
    }

    /// <inheritdoc />
    public override void Stop()
    {
        base.Stop();
        Source = string.Empty;
    }

    private bool _disposed;
    /// <summary>
    /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
    /// </summary>
    /// <param name="disposing">The disposing parameter should be false when called from a finalizer, and true when called from the IDisposable.Dispose method.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                // Managed resources.
            }

            // Unmanaged resources.
            Player?.Dispose();

            _disposed = true;
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}
