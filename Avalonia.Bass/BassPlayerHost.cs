using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.Threading;
using ManagedBass;
using ManagedBass.Fx;
using ManagedBass.Mix;

// ReSharper disable ConstantNullCoalescingCondition

namespace HanumanInstitute.MediaPlayer.Avalonia.Bass;

/// <summary>
/// BASS audio media player to be displayed within <see cref="MediaPlayer"/>.
/// </summary>
public class BassPlayerHost : PlayerHostBase, IDisposable
{
    /// <summary>
    /// BASS audio source handle.
    /// </summary>
    private int _chanIn;
    /// <summary>
    /// BASS audio output stream handle.
    /// </summary>
    private int _chanOut;
    /// <summary>
    /// BASS audio mix stream handle.
    /// </summary>
    private int _chanMix;
    /// <summary>
    /// Channel information from when the channel was initialized. 
    /// </summary>
    private ChannelInfo _chanInfo;
    /// <summary>
    /// Device information from when the channel was initialized.
    /// </summary>
    private BassInfo _deviceInfo;
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
    private readonly object _lock = new();
    
    /// <summary>
    /// Registration for the <see cref="MediaError"/> routed event.
    /// </summary>
    public static readonly RoutedEvent<RoutedEventArgs> MediaErrorEvent =
        RoutedEvent.Register<BassPlayerHost, RoutedEventArgs>(nameof(MediaError), RoutingStrategies.Direct);
    /// <summary>
    /// Occurs when the player throws an error.
    /// </summary>
    public event EventHandler<RoutedEventArgs>? MediaError
    {
        add => AddHandler(MediaErrorEvent, value);
        remove => RemoveHandler(MediaErrorEvent, value);
    }

    /// <summary>
    /// Registration for the <see cref="MediaFinished"/> routed event.
    /// </summary>
    public static readonly RoutedEvent<RoutedEventArgs> MediaFinishedEvent =
        RoutedEvent.Register<BassPlayerHost, RoutedEventArgs>(nameof(MediaFinished), RoutingStrategies.Direct);
    /// <summary>
    /// Occurs when media playback is finished.
    /// </summary>
    public event EventHandler<RoutedEventArgs>? MediaFinished
    {
        add => AddHandler(MediaFinishedEvent, value);
        remove => RemoveHandler(MediaFinishedEvent, value);
    }

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        base.OnInitialized();

        if (!Design.IsDesignMode)
        {
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

    /// <inheritdoc />
    protected override void SourceChanged(string? value)
    {
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
            PitchError = null;
        }
    }

    /// <summary>
    /// Defines the Status property.
    /// </summary>
    public static readonly DirectProperty<BassPlayerHost, PlaybackStatus> StatusProperty =
        AvaloniaProperty.RegisterDirect<BassPlayerHost, PlaybackStatus>(nameof(Status), o => o.Status,
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

    /// <summary>
    /// Defines the PositionRefreshMilliseconds property.
    /// </summary>
    public static readonly DirectProperty<BassPlayerHost, int> PositionRefreshMillisecondsProperty =
        AvaloniaProperty.RegisterDirect<BassPlayerHost, int>(nameof(PositionRefreshMilliseconds),
            o => o.PositionRefreshMilliseconds, (o, v) => o.PositionRefreshMilliseconds = v);
    private int _positionRefreshMilliseconds = 200;
    /// <summary>
    /// Gets or sets the interval in milliseconds at which the position bar is updated.
    /// </summary>
    public int PositionRefreshMilliseconds
    {
        get => _positionRefreshMilliseconds;
        set
        {
            SetAndRaise(PositionRefreshMillisecondsProperty, ref _positionRefreshMilliseconds, Math.Max(1, value));
            if (_posTimer != null)
            {
                _posTimer.Interval = TimeSpan.FromMilliseconds(_positionRefreshMilliseconds);
            }
        }
    }

    /// <summary>
    /// Defines the Rate property.
    /// </summary>
    public static readonly DirectProperty<BassPlayerHost, double> RateProperty =
        AvaloniaProperty.RegisterDirect<BassPlayerHost, double>(nameof(Rate), o => o.Rate, (o, v) => o.Rate = v);
    private double _rate = 1;
    /// <summary>
    /// Gets or sets the playback rate as a double, where 1.0 is normal speed, 0.5 is half-speed, and 2 is double-speed.
    /// </summary>
    public double Rate
    {
        get => _rate;
        set
        {
            SetAndRaise(RateProperty, ref _rate, value);
            AdjustTempo();
        }
    }

    /// <summary>
    /// Defines the Pitch property.
    /// </summary>
    public static readonly DirectProperty<BassPlayerHost, double> PitchProperty =
        AvaloniaProperty.RegisterDirect<BassPlayerHost, double>(nameof(Pitch), o => o.Pitch, (o, v) => o.Pitch = v);
    private double _pitch = 1;
    /// <summary>
    /// Gets or sets the playback pitch as a double, rising or lowering the pitch by given factor without altering speed. 
    /// </summary>
    public double Pitch
    {
        get => _pitch;
        set
        {
            SetAndRaise(PitchProperty, ref _pitch, CoerceDouble(value));
            AdjustTempo();
        }
    }

    /// <summary>
    /// Defines the VolumeBoost property.
    /// </summary>
    public static readonly DirectProperty<BassPlayerHost, double> VolumeBoostProperty =
        AvaloniaProperty.RegisterDirect<BassPlayerHost, double>(nameof(VolumeBoost), o => o.VolumeBoost,
            (o, v) => o.VolumeBoost = v);
    private double _volumeBoost = 1;
    /// <summary>
    /// Gets or sets a value that will be multiplied to the volume.
    /// </summary>
    public double VolumeBoost
    {
        get => _volumeBoost;
        set
        {
            SetAndRaise(VolumeBoostProperty, ref _volumeBoost, CoerceDouble(value));
            VolumeChanged(Volume);
        }
    }

    /// <summary>
    /// Defines the UseEffects property.
    /// </summary>
    public static readonly DirectProperty<BassPlayerHost, bool> UseEffectsProperty =
        AvaloniaProperty.RegisterDirect<BassPlayerHost, bool>(nameof(UseEffects), o => o.UseEffects,
            (o, v) => o.UseEffects = v);
    /// <summary>
    /// Gets or sets whether to enable pitch-shifting effects.
    /// By default, effects are enabled if Rate, Pitch or Speed are set before loading a media file.
    /// If file is loaded at normal speed and you want to allow changing it later, this property forces initializing the effects module.
    /// This property must be set before playback. 
    /// </summary>
    public bool UseEffects
    {
        get => _useEffects;
        set => SetAndRaise(UseEffectsProperty, ref _useEffects, value);
    }
    private bool _useEffects;

    /// <summary>
    /// Defines the EffectsQuick property.
    /// </summary>
    public static readonly DirectProperty<BassPlayerHost, bool> EffectsQuickProperty =
        AvaloniaProperty.RegisterDirect<BassPlayerHost, bool>(nameof(EffectsQuick), o => o.EffectsQuick,
            (o, v) => o.EffectsQuick = v);
    private bool _effectsQuick;
    /// <summary>
    /// Gets or sets whether to use the quick mode that substantially speeds up the algorithm but may degrade the sound quality by a small amount.
    /// </summary>
    public bool EffectsQuick
    {
        get => _effectsQuick;
        set
        {
            SetAndRaise(UseEffectsProperty, ref _effectsQuick, value);
            AdjustEffects();
        }
    }

    /// <summary>
    /// Defines the EffectsAntiAlias property.
    /// </summary>
    public static readonly DirectProperty<BassPlayerHost, bool> EffectsAntiAliasProperty =
        AvaloniaProperty.RegisterDirect<BassPlayerHost, bool>(nameof(EffectsAntiAlias), o => o.EffectsAntiAlias,
            (o, v) => o.EffectsAntiAlias = v);
    private bool _effectsAntiAlias;
    /// <summary>
    /// Gets or sets whether to enable the Anti-Alias filter.
    /// </summary>
    public bool EffectsAntiAlias
    {
        get => _effectsAntiAlias;
        set
        {
            SetAndRaise(EffectsAntiAliasProperty, ref _effectsAntiAlias, value);
            AdjustEffects();
        }
    }

    /// <summary>
    /// Defines the EffectsAntiAliasLength property. 
    /// </summary>
    public static readonly DirectProperty<BassPlayerHost, int> EffectsAntiAliasLengthProperty =
        AvaloniaProperty.RegisterDirect<BassPlayerHost, int>(nameof(EffectsAntiAliasLength), o => o.EffectsAntiAliasLength,
            (o, v) => o.EffectsAntiAliasLength = v, 32);
    private int _effectsAntiAliasLength = 32;
    /// <summary>
    /// Gets or sets the Anti-Alias filter length. 
    /// </summary>
    public int EffectsAntiAliasLength
    {
        get => _effectsAntiAliasLength;
        set
        {
            value = Math.Max(8, Math.Min(128, value));
            SetAndRaise(EffectsAntiAliasLengthProperty, ref _effectsAntiAliasLength, value);
            AdjustEffects();
        }
    }

    /// <summary>
    /// Defines the EffectsSampleRateConversion property. 
    /// </summary>
    public static readonly DirectProperty<BassPlayerHost, int> EffectsSampleRateConversionProperty =
        AvaloniaProperty.RegisterDirect<BassPlayerHost, int>(nameof(EffectsSampleRateConversion), o => o.EffectsSampleRateConversion,
            (o, v) => o.EffectsSampleRateConversion = v, 4);
    private int _effectsSampleRateConversion = 4;
    /// <summary>
    /// Gets or sets the sample rate conversion quality... 0 = linear interpolation, 1 = 8 point sinc interpolation, 2 = 16 point sinc interpolation, 3 = 32 point sinc interpolation, 4 = 64 point sinc interpolation. 
    /// </summary>
    public int EffectsSampleRateConversion
    {
        get => _effectsSampleRateConversion;
        set
        {
            value = Math.Max(0, Math.Min(4, value));
            SetAndRaise(EffectsSampleRateConversionProperty, ref _effectsSampleRateConversion, value);
            AdjustEffects();
        }
    }

    /// <summary>
    /// Defines the EffectsFloat property. 
    /// </summary>
    public static readonly DirectProperty<BassPlayerHost, bool> EffectsFloatProperty =
        AvaloniaProperty.RegisterDirect<BassPlayerHost, bool>(nameof(EffectsFloat), o => o.EffectsFloat,
            (o, v) => o.EffectsFloat = v, true);
    /// <summary>
    /// Gets or sets whether to process effects in 32-bit. True for 32-bit, False for 16-bit. 
    /// </summary>
    public bool EffectsFloat
    {
        get => _effectsFloat; 
        set => SetAndRaise(EffectsFloatProperty, ref _effectsFloat, value);
    }
    private bool _effectsFloat = true;

    /// <summary>
    /// Defines the EffectsRoundPitch property. 
    /// </summary>
    public static readonly DirectProperty<BassPlayerHost, bool> EffectsRoundPitchProperty =
        AvaloniaProperty.RegisterDirect<BassPlayerHost, bool>(nameof(EffectsRoundPitch), o => o.EffectsRoundPitch,
            (o, v) => o.EffectsRoundPitch = v, true);
    private bool _effectsRoundPitch = true;
    /// <summary>
    /// Gets or sets whether to round the pitch to the nearest fraction when pitch-shifting for improved quality. 
    /// </summary>
    public bool EffectsRoundPitch
    {
        get => _effectsRoundPitch;
        set
        {
            SetAndRaise(EffectsRoundPitchProperty, ref _effectsRoundPitch, value);
            AdjustTempo();
        }
    }
    
    /// <summary>
    /// Defines the EffectsSkipTempo property. 
    /// </summary>
    public static readonly DirectProperty<BassPlayerHost, bool> EffectsSkipTempoProperty =
        AvaloniaProperty.RegisterDirect<BassPlayerHost, bool>(nameof(EffectsSkipTempo), o => o.EffectsSkipTempo,
            (o, v) => o.EffectsSkipTempo = v, false);
    private bool _effectsSkipTempo = false;
    /// <summary>
    /// Gets or sets whether to skip tempo adjustment for maximum audio quality. 
    /// </summary>
    public bool EffectsSkipTempo
    {
        get => _effectsSkipTempo;
        set
        {
            SetAndRaise(EffectsSkipTempoProperty, ref _effectsSkipTempo, value);
            AdjustTempo();
        }
    }

    /// <summary>
    /// Defines the PitchError property. 
    /// </summary>
    public static readonly DirectProperty<BassPlayerHost, double?> PitchErrorProperty =
        AvaloniaProperty.RegisterDirect<BassPlayerHost, double?>(nameof(PitchError), o => o.PitchError, (o, v) => o.PitchError = v);
    /// <summary>
    /// Gets the pitch rounding error when EffectsRoundPitch is true.
    /// </summary>
    public double? PitchError
    {
        get => _pitchError;
        set => SetAndRaise(PitchErrorProperty, ref _pitchError, value);
    }
    private double? _pitchError;
    
      
    /// <summary>
    /// Defines the OutputSampleRate property. 
    /// </summary>
    public static readonly DirectProperty<BassPlayerHost, int?> OutputSampleRateProperty =
        AvaloniaProperty.RegisterDirect<BassPlayerHost, int?>(nameof(OutputSampleRate), o => o.OutputSampleRate,
            (o, v) => o.OutputSampleRate = v);
    /// <summary>
    /// Gets or sets the device output sample rate. 0 = auto-detected.
    /// On Linux, it cannot be auto-detected and must be set manually if different than 48000. 
    /// </summary>
    public int? OutputSampleRate
    {
        get => _outputSampleRate;
        set => SetAndRaise(OutputSampleRateProperty, ref _outputSampleRate, value);
    }
    private int? _outputSampleRate;
    
    private bool BassActive => _chanIn != 0;

    private TimeSpan BassDuration =>
        TimeSpan.FromSeconds(ManagedBass.Bass.ChannelBytes2Seconds(_chanIn, ManagedBass.Bass.ChannelGetLength(_chanIn)));

    private TimeSpan BassPosition =>
        TimeSpan.FromSeconds(ManagedBass.Bass.ChannelBytes2Seconds(_chanIn, ManagedBass.Bass.ChannelGetPosition(_chanIn)));

    private void Player_PlaybackStopped(int handle, int channel, int data, IntPtr user)
    {
        // This event should only occur when media ends on its own. Discard when pressing Stop.
        if (_isStopping) { return; }

        Dispatcher.UIThread.InvokeAsync(() =>
        {
            // ReleaseChannel();
            base.OnMediaUnloaded();
            RaiseEvent(new RoutedEventArgs(MediaFinishedEvent));
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

    /// <inheritdoc />
    protected override void SetDisplayText()
    {
        Text = Status switch
        {
            PlaybackStatus.Loading => Properties.Resources.Loading,
            PlaybackStatus.Playing => Title ?? System.IO.Path.GetFileName(Source),
            PlaybackStatus.Error => Properties.Resources.MediaError,
            _ => string.Empty
        } ?? string.Empty;
    }

    /// <inheritdoc />
    protected override void PositionChanged(TimeSpan value, bool isSeeking)
    {
        base.PositionChanged(value, isSeeking);
        if (BassActive && isSeeking)
        {
            lock (_lock)
            {
                if (BassActive && isSeeking)
                {
                    ManagedBass.Bass.ChannelSetPosition(_chanIn,
                        ManagedBass.Bass.ChannelSeconds2Bytes(_chanIn, value.TotalSeconds));
                }
            }
        }
    }

    /// <inheritdoc />
    protected override void IsPlayingChanged(bool value)
    {
        base.IsPlayingChanged(value);
        if (!BassActive) { return; }

        if (value)
        {
            ManagedBass.Bass.ChannelPlay(_chanOut).Valid();
            _posTimer?.Start();
        }
        else
        {
            ManagedBass.Bass.ChannelPause(_chanOut); //.Valid();
            _posTimer?.Stop();
        }
    }

    /// <inheritdoc />
    protected override void VolumeChanged(int value)
    {
        base.VolumeChanged(value);
        if (BassActive)
        {
            AdjustVolume(value);
        }
    }

    /// <inheritdoc />
    protected override void SpeedChanged(double value)
    {
        base.SpeedChanged(value);
        AdjustTempo();
    }

    /// <inheritdoc />
    protected override void LoopChanged(bool value)
    {
        base.LoopChanged(value);
        if (BassActive)
        {
            // _mediaOut.Loop = value;
        }
    }

    /// <inheritdoc />
    public override void Restart()
    {
        base.Restart();
        if (BassActive)
        {
            ManagedBass.Bass.ChannelSetPosition(_chanIn, 0).Valid();
            ManagedBass.Bass.ChannelPlay(_chanOut, true).Valid();
        }
    }

    [SuppressMessage("ReSharper", "CompareOfFloatsByEqualityOperator")]
    private void LoadMedia()
    {
        BassDevice.Instance.InitDevice();
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
            var deviceSampleRate = OutputSampleRate;
            var useEffects = UseEffects || speed != 1.0 || rate != 1.0 || pitch != 1.0;
            _ = Task.Run(() =>
            {
                try
                {
                    ManagedBass.Bass.GetInfo(out _deviceInfo).Valid();
                    var flagFloat = EffectsFloat ? BassFlags.Float : 0;
                    _chanIn = _chanOut = ManagedBass.Bass
                        .CreateStream(fileName, Flags: useEffects ? flagFloat | BassFlags.Decode : 0).Valid();
                    _chanInfo = ManagedBass.Bass.ChannelGetInfo(_chanIn);
                    ManagedBass.Bass.ChannelSetSync(_chanIn, SyncFlags.End | SyncFlags.Mixtime, 0, Player_PlaybackStopped)
                        .Valid();

                    if (useEffects)
                    {
                        // Add mix plugin.
                        _chanMix = BassMix.CreateMixerStream(deviceSampleRate ?? _deviceInfo.SampleRate, _chanInfo.Channels, 
                            BassFlags.MixerEnd | BassFlags.Decode).Valid();
                        BassMix.MixerAddChannel(_chanMix, _chanIn, 0 | BassFlags.MixerChanNoRampin | BassFlags.AutoFree);

                        // Add tempo plugin.
                        _chanOut = BassFx.TempoCreate(_chanMix, 0 | BassFlags.FxFreeSource).Valid();
                        AdjustEffects();
                        AdjustVolume(volume);
                        AdjustTempo(speed, rate, pitch);
                    }

                    if (autoPlay)
                    {
                        ManagedBass.Bass.ChannelPlay(_chanOut).Valid();
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
                        RaiseEvent(new RoutedEventArgs(MediaErrorEvent));
                    });
                }
            }).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Sets the UseQuickAlgorithm parameter. 
    /// </summary>
    private void AdjustEffects()
    {
        if (BassActive)
        {
            ManagedBass.Bass.ChannelSetAttribute(_chanOut, ChannelAttribute.TempoUseQuickAlgorithm, EffectsQuick ? 1 : 0);
            ManagedBass.Bass.ChannelSetAttribute(_chanOut, ChannelAttribute.TempoUseAAFilter, EffectsAntiAlias ? 1 : 0);
            ManagedBass.Bass.ChannelSetAttribute(_chanOut, ChannelAttribute.TempoAAFilterLength, EffectsAntiAliasLength);
            ManagedBass.Bass.ChannelSetAttribute(_chanMix, ChannelAttribute.SampleRateConversion, EffectsSampleRateConversion);
            ManagedBass.Bass.ChannelSetAttribute(_chanOut, ChannelAttribute.SampleRateConversion, EffectsSampleRateConversion);
        }
    }

    /// <summary>
    /// Sets the volume on the playback channel. 
    /// </summary>
    /// <param name="volume">The volume to set, between 0 and 100.</param>
    private void AdjustVolume(double volume)
    {
        ManagedBass.Bass.ChannelSetAttribute(_chanOut, ChannelAttribute.Volume, VolumeBoost * volume / 100);
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
            // ManagedBass.Bass.ChannelSetAttribute(_chanOut, ChannelAttribute.Tempo, (1.0 / pitch * speed - 1.0) * 100.0);
            // ManagedBass.Bass.ChannelSetAttribute(_chanOut, ChannelAttribute.TempoFrequency, _chanInfo.Frequency * pitch * rate);

            // Optimized pitch shifting for increased quality
            // 1. Rate shift to Output * Pitch (rounded)
            // 2. Resample to Output (48000Hz)
            // 3. Tempo adjustment: -Pitch
            double pitchError = 0;

            var r = pitch * rate;
            if (EffectsRoundPitch)
            {
                r = Fraction.RoundToFraction(r, .005, out pitchError);
            }
            var t = r / speed;

            // 1. Rate Shift (lossless)
            ManagedBass.Bass.ChannelSetAttribute(_chanIn, ChannelAttribute.Frequency, _chanInfo.Frequency * r);
            // 2. Resampling to output in _chanMix constructor
            // 3. Tempo adjustment
            ManagedBass.Bass.ChannelSetAttribute(_chanOut, ChannelAttribute.Tempo,
                !EffectsSkipTempo ? (1.0 / t - 1.0) * 100.0 : 0);

            Dispatcher.UIThread.Post(() =>
            {
                PitchError = EffectsRoundPitch ? pitchError : null;
            });
        }
    }

    /// <inheritdoc />
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
            ManagedBass.Bass.ChannelStop(_chanOut).Valid();
            ManagedBass.Bass.StreamFree(_chanOut).Valid();
            _chanOut = 0;
            _chanMix = 0;
            _chanIn = 0;
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
                ReleaseChannel();
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
