using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Interactivity;
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
        VisualChildren.Add(PlayerView);
    }

    /// <summary>
    /// Releases MPV resources.
    /// </summary>
    ~MpvPlayerHost()
    {
        Dispose(false);
    }
    
    // MpvContext property
    public static readonly DirectProperty<MpvView, MpvContext?> MpvContextProperty = AvaloniaProperty.RegisterDirect<MpvView, MpvContext?>(
        nameof(MpvContext), o => o.MpvContext, defaultBindingMode: BindingMode.OneWayToSource);
    public MpvContext? MpvContext => PlayerView.MpvContext;

    /// <summary>
    /// Gets the MpvView class instance.
    /// </summary>
    public MpvView PlayerView { get; private set; }

    // /// <summary>
    // /// Occurs after the media player is initialized.
    // /// </summary>
    // public event EventHandler? MediaPlayerInitialized;

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
            MpvContext?.Stop().Invoke();
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

    protected override async void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        await Task.Delay(100); // Fails to load if we don't give a slight delay.
        
        MpvContext!.FileLoaded += Player_FileLoaded;
        MpvContext!.EndFile += Player_EndFile;

        MpvContext.TimePos.Changed += Player_PositionChanged;

        var options = new MpvAsyncOptions { WaitForResponse = false };
        await MpvContext.Volume.SetAsync(Volume, options);
        await MpvContext.Speed.SetAsync(GetSpeed(), options);
        await MpvContext.LoopFile.SetAsync(Loop ? "yes" : "no", options);

        if (!string.IsNullOrEmpty(Source) && !_initLoaded)
        {
            await LoadMediaAsync();
        }
    }

    private void Player_FileLoaded(object sender, EventArgs e)
    {
        Dispatcher.UIThread.Post(() =>
        {
            Status = PlaybackStatus.Playing;
            Duration = TimeSpan.FromSeconds(MpvContext!.Duration.Get()!.Value);
            OnMediaLoaded();
        });
    }

    private void Player_EndFile(object sender, MpvEndFileEventArgs e)
    {
        Dispatcher.UIThread.Post(() =>
        {
            if (e.Reason == MpvEndFileReason.Error)
            {
                Status = PlaybackStatus.Error;
            }
            OnMediaUnloaded();
        });
    }

    private void Player_PositionChanged(object sender, MpvValueChangedEventArgs<double, double> e)
    {
        Dispatcher.UIThread.Post(() => base.SetPositionNoSeek(TimeSpan.FromSeconds(e.NewValue!.Value)));
    }

    /// <inheritdoc />
    protected override void SetDisplayText()
    {
        Text = Status switch
        {
            PlaybackStatus.Loading => Properties.Resources.Loading,
            PlaybackStatus.Playing => Title ?? Path.GetFileName(Source),
            PlaybackStatus.Error => Properties.Resources.MediaError,
            _ => ""
        };
    }

    /// <inheritdoc />
    protected override void PositionChanged(TimeSpan value, bool isSeeking)
    {
        base.PositionChanged(value, isSeeking);
        lock (MpvContext!)
        {
            if (IsMediaLoaded && isSeeking)
            {
                MpvContext.TimePos.Set(value.TotalSeconds);
            }
        }
    }

    /// <inheritdoc />
    protected override void IsPlayingChanged(bool value)
    {
        base.IsPlayingChanged(value);
        MpvContext?.Pause.Set(!value);
    }

    /// <inheritdoc />
    protected override void VolumeChanged(int value)
    {
        base.VolumeChanged(value);
        MpvContext?.Volume.Set(value);
    }

    /// <inheritdoc />
    protected override void SpeedChanged(double value)
    {
        base.SpeedChanged(value);
        MpvContext?.Speed.Set(value);
    }

    /// <inheritdoc />
    protected override void LoopChanged(bool value)
    {
        base.LoopChanged(value);
        MpvContext?.LoopFile.Set(value ? "yes" : "no");
    }

    private async Task LoadMediaAsync()
    {
        if (!IsLoaded || Design.IsDesignMode) { return; }
        
        MpvContext!.Stop().Invoke();
        if (!string.IsNullOrEmpty(Source))
        {
            _initLoaded = true;
            Thread.Sleep(10);
            MpvContext.Pause.Set(!AutoPlay);
            await MpvContext.LoadFile(Source!).InvokeAsync();
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
            MpvContext?.Dispose();

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
