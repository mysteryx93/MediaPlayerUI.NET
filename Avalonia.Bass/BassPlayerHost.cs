using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.LogicalTree;
using Avalonia.Threading;
using ManagedBass;
using ManagedBass.Fx;

// ReSharper disable ConstantNullCoalescingCondition

namespace HanumanInstitute.MediaPlayer.Avalonia.Bass;

public class BassPlayerHost : PlayerHostBase, IDisposable
{
    /// <summary>
    /// BASS audio stream handle.
    /// </summary>
    private int _chan;
    /// <summary>
    /// Channel information from when the channel was initialized. 
    /// </summary>
    private ChannelInfo _chanInfo;
    /// <summary>
    /// True when Source is currently being set to Empty.
    /// </summary>
    private bool _isStopping;
    /// <summary>
    /// Timer to get position.
    /// </summary>
    private DispatcherTimer? _posTimer;
    /// <summary>
    /// Whether LoadMedia has ever been called.
    /// </summary>
    private bool _initLoaded;
    private readonly object _lock = new object();

    public event EventHandler? MediaError;
    public event EventHandler? MediaFinished;

    protected override void OnInitialized()
    {
        base.OnInitialized();

        if (!Design.IsDesignMode)
        {
            BassDevice.Init();

            this.FindLogicalAncestorOfType<TopLevel>()!.Closed += (_, _) => Dispose();

            _posTimer = new DispatcherTimer(TimeSpan.FromMilliseconds(PositionRefreshMilliseconds),
                DispatcherPriority.Render, Timer_PositionChanged);

            if (!string.IsNullOrEmpty(Source) && !_initLoaded)
            {
                LoadMedia();
            }
        }
    }

    // DllPath
    // public static readonly DirectProperty<BassPlayerHost, string?> DllPathProperty =
    //     AvaloniaProperty.RegisterDirect<BassPlayerHost, string?>(nameof(DllPath), o => o.DllPath);
    // public string? DllPath { get; set; }

    // Source
    public static readonly DirectProperty<BassPlayerHost, string?> SourceProperty =
        AvaloniaProperty.RegisterDirect<BassPlayerHost, string?>(nameof(Source), o => o.Source, (o, v) => o.Source = v);
    private string? _source;
    public string? Source
    {
        get => _source;
        set
        {
            _source = value;
            if (!IsInitialized) { return; }

            if (!string.IsNullOrEmpty(value))
            {
                Status = PlaybackStatus.Loading;
                LoadMedia();
            }
            else
            {
                Status = PlaybackStatus.Stopped;
                _isStopping = true;
                ReleaseChannel();
                _isStopping = false;
            }
        }
    }

    // Title
    public static readonly DirectProperty<BassPlayerHost, string?> TitleProperty =
        AvaloniaProperty.RegisterDirect<BassPlayerHost, string?>(nameof(Title), o => o.Title, (o, v) => o.Title = v);
    private string? _title;
    public string? Title
    {
        get => _title;
        set
        {
            _title = value;
            SetDisplayText();
        }
    }

    // Status
    public static readonly DirectProperty<BassPlayerHost, PlaybackStatus> StatusProperty =
        AvaloniaProperty.RegisterDirect<BassPlayerHost, PlaybackStatus>(nameof(Status), o => o.Status,
            (o, v) => o.Status = v);
    private PlaybackStatus _status = PlaybackStatus.Stopped;
    public PlaybackStatus Status
    {
        get => _status;
        protected set
        {
            _status = value;
            SetDisplayText();
        }
    }

    // PositionRefreshMilliseconds
    public static readonly DirectProperty<BassPlayerHost, int> PositionRefreshMillisecondsProperty =
        AvaloniaProperty.RegisterDirect<BassPlayerHost, int>(nameof(PositionRefreshMilliseconds),
            o => o.PositionRefreshMilliseconds, (o, v) => o.PositionRefreshMilliseconds = v);
    private int _positionRefreshMilliseconds = 200;
    public int PositionRefreshMilliseconds
    {
        get => _positionRefreshMilliseconds;
        set
        {
            _positionRefreshMilliseconds = value < 1 ? 1 : value;
            if (_posTimer != null)
            {
                _posTimer.Interval = TimeSpan.FromMilliseconds(_positionRefreshMilliseconds);
                // _posTimer.Interval = TimeSpan.FromMilliseconds(_positionRefreshMilliseconds);
            }
        }
    }

    // Rate
    public static readonly DirectProperty<BassPlayerHost, double> RateProperty =
        AvaloniaProperty.RegisterDirect<BassPlayerHost, double>(nameof(Rate), o => o.Rate, (o, v) => o.Rate = v);
    private double _rate = 1;
    public double Rate
    {
        get => _rate;
        set
        {
            _rate = CoerceDouble(value);
            AdjustTempo();
        }
    }

    // Pitch
    public static readonly DirectProperty<BassPlayerHost, double> PitchProperty =
        AvaloniaProperty.RegisterDirect<BassPlayerHost, double>(nameof(Pitch), o => o.Pitch, (o, v) => o.Pitch = v);
    private double _pitch = 1;
    public double Pitch
    {
        get => _pitch;
        set
        {
            _pitch = CoerceDouble(value);
            AdjustTempo();
        }
    }

    // VolumeBoost
    public static readonly DirectProperty<BassPlayerHost, double> VolumeBoostProperty =
        AvaloniaProperty.RegisterDirect<BassPlayerHost, double>(nameof(VolumeBoost), o => o.VolumeBoost,
            (o, v) => o.VolumeBoost = v);
    private double _volumeBoost = 1;
    public double VolumeBoost
    {
        get => _volumeBoost;
        set
        {
            _volumeBoost = CoerceDouble(value);
            VolumeChanged(Volume);
        }
    }

    // UseEffects
    public static readonly DirectProperty<BassPlayerHost, bool> UseEffectsProperty =
        AvaloniaProperty.RegisterDirect<BassPlayerHost, bool>(nameof(UseEffects), o => o.UseEffects,
            (o, v) => o.UseEffects = v);
    public bool UseEffects { get; set; }

    private bool BassActive => _chan != 0;

    private TimeSpan BassDuration =>
        TimeSpan.FromSeconds(ManagedBass.Bass.ChannelBytes2Seconds(_chan, ManagedBass.Bass.ChannelGetLength(_chan)));

    private TimeSpan BassPosition =>
        TimeSpan.FromSeconds(ManagedBass.Bass.ChannelBytes2Seconds(_chan, ManagedBass.Bass.ChannelGetPosition(_chan)));

    private void Player_PlaybackStopped(int handle, int channel, int data, IntPtr user)
    {
        // This event should only occur when media ends on its own. Discard when pressing Stop.
        if (_isStopping) { return; }

        Dispatcher.UIThread.InvokeAsync(() =>
        {
            // ReleaseChannel();
            base.OnMediaUnloaded();
            MediaFinished?.Invoke(this, EventArgs.Empty);
        });
    }

    private void Player_MediaLoaded()
    {
        //Debug.WriteLine("MediaLoaded");
        Dispatcher.UIThread.InvokeAsync(() =>
        {
            Status = PlaybackStatus.Playing;
            base.Duration = BassActive ? BassDuration : TimeSpan.Zero;
            base.OnMediaLoaded();
        });
    }

    private void Timer_PositionChanged(object? sender, EventArgs e)
    {
        if (BassActive)
        {
            lock (_lock)
            {
                if (BassActive)
                {
                    base.SetPositionNoSeek(BassPosition);
                }
            }
        }
    }

    private void SetDisplayText()
    {
        Text = Status switch
        {
            PlaybackStatus.Loading => Properties.Resources.Loading,
            PlaybackStatus.Playing => Title ?? System.IO.Path.GetFileName(Source),
            PlaybackStatus.Error => Properties.Resources.MediaError,
            _ => string.Empty
        } ?? string.Empty;
    }

    protected override void PositionChanged(TimeSpan value, bool isSeeking)
    {
        base.PositionChanged(value, isSeeking);
        if (BassActive && isSeeking)
        {
            lock (_lock)
            {
                if (BassActive && isSeeking)
                {
                    ManagedBass.Bass.ChannelSetPosition(_chan,
                        ManagedBass.Bass.ChannelSeconds2Bytes(_chan, value.TotalSeconds));
                }
            }
        }
    }

    protected override void IsPlayingChanged(bool value)
    {
        base.IsPlayingChanged(value);
        if (!BassActive) { return; }

        if (value)
        {
            ManagedBass.Bass.ChannelPlay(_chan).Valid();
            _posTimer?.Start();
        }
        else
        {
            ManagedBass.Bass.ChannelPause(_chan); //.Valid();
            _posTimer?.Stop();
        }
    }

    protected override void VolumeChanged(int value)
    {
        base.VolumeChanged(value);
        if (BassActive)
        {
            AdjustVolume(value);
        }
    }

    protected override void SpeedChanged(double value)
    {
        base.SpeedChanged(value);
        AdjustTempo();
    }

    protected override void LoopChanged(bool value)
    {
        base.LoopChanged(value);
        if (BassActive)
        {
            // _mediaOut.Loop = value;
        }
    }

    public override void Restart()
    {
        base.Restart();
        if (BassActive)
        {
            ManagedBass.Bass.ChannelPlay(_chan, true).Valid();
        }
    }

    [SuppressMessage("ReSharper", "CompareOfFloatsByEqualityOperator")]
    private void LoadMedia()
    {
        ReleaseChannel();

        if (Source != null)
        {
            _initLoaded = true;
            // Store locally because properties can't be accessed from new thread.
            var fileName = Source;
            var volume = Volume;
            var speed = GetSpeed();
            var rate = Rate;
            var pitch = Pitch;
            var autoPlay = AutoPlay;
            var useEffects = UseEffects || speed != 1.0 || rate != 1.0 || pitch != 1.0;
            _ = Task.Run(() =>
            {
                try
                {
                    _chan = ManagedBass.Bass.CreateStream(fileName, Flags: useEffects ? BassFlags.Decode : 0).Valid();
                    _chanInfo = ManagedBass.Bass.ChannelGetInfo(_chan);
                    ManagedBass.Bass.ChannelSetSync(_chan, SyncFlags.End | SyncFlags.Mixtime, 0, Player_PlaybackStopped)
                        .Valid();

                    if (useEffects)
                    {
                        _chan = BassFx.TempoCreate(_chan, BassFlags.FxFreeSource).Valid();
                        ManagedBass.Bass.ChannelSetAttribute(_chan, ChannelAttribute.TempoUseAAFilter, 0);
                        AdjustVolume(volume);
                        AdjustTempo(speed, rate, pitch);
                    }

                    if (autoPlay)
                    {
                        ManagedBass.Bass.ChannelPlay(_chan).Valid();
                        Dispatcher.UIThread.InvokeAsync(() => _posTimer?.Start());
                    }

                    Player_MediaLoaded();
                }
                catch
                {
                    ReleaseChannel();
                    Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        Status = PlaybackStatus.Error;
                        MediaError?.Invoke(this, EventArgs.Empty);
                    });
                }
            }).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Sets the volume on the playback channel. 
    /// </summary>
    /// <param name="volume">The volume to set, between 0 and 100.</param>
    private void AdjustVolume(double volume)
    {
        ManagedBass.Bass.ChannelSetAttribute(_chan, ChannelAttribute.Volume, VolumeBoost * volume / 100);
    }

    /// <summary>
    /// Calculates and sets BASS Tempo and TempoFrequency parameters based on Speed, Rate and Pitch. 
    /// </summary>
    private void AdjustTempo() => AdjustTempo(GetSpeed(), Rate, Pitch);

    /// <summary>
    /// Calculates and sets BASS Tempo and TempoFrequency parameters based on Speed, Rate and Pitch. 
    /// </summary>
    private void AdjustTempo(double speed, double rate, double pitch)
    {
        if (BassActive)
        {
            // In BASS, 2x speed is 100 (+100%), whereas our Speed property is 2. Need to convert.
            // speed 1=0, 2=100, 3=200, 4=300, .5=-100, .25=-300
            speed /= pitch;
            var tempo = speed >= 1 ? -100.0 / speed + 100 : 100.0 * speed - 100;
            var freq = _chanInfo.Frequency * rate * pitch;
            ManagedBass.Bass.ChannelSetAttribute(_chan, ChannelAttribute.Tempo, tempo);
            ManagedBass.Bass.ChannelSetAttribute(_chan, ChannelAttribute.TempoFrequency, freq);
        }
    }

    public override void Stop()
    {
        base.Stop();
        Source = string.Empty;
        ReleaseChannel();
        base.OnMediaUnloaded();
    }

    private void ReleaseChannel()
    {
        if (BassActive)
        {
            ManagedBass.Bass.ChannelStop(_chan).Valid();
            ManagedBass.Bass.StreamFree(_chan).Valid();
            _chan = 0;
        }
    }


    private bool _disposedValue;

    private void Dispose(bool disposing)
    {
        if (!_disposedValue)
        {
            if (disposing)
            {
                ReleaseChannel();
            }

            _disposedValue = true;
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}

