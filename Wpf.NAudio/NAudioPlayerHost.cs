using System;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using SoundTouch.Net.NAudioSupport;
// ReSharper disable CompareOfFloatsByEqualityOperator
// ReSharper disable InconsistentNaming

namespace HanumanInstitute.MediaPlayer.Wpf.NAudio;

/// <summary>
/// NAudio audio media player to be displayed within <see cref="MediaPlayer"/>.
/// </summary>
public class NAudioPlayerHost : PlayerHostBase, IDisposable
{
    static NAudioPlayerHost()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(NAudioPlayerHost), new FrameworkPropertyMetadata(typeof(NAudioPlayerHost)));
    }

    /// <summary>
    /// Initializes a new instance of the NAudioPlayerHost class.
    /// </summary>
    public NAudioPlayerHost()
    {
        if (!DesignerProperties.GetIsInDesignMode(this))
        {
            Loaded += UserControl_Loaded;
            Unloaded += UserControl_Unloaded;
            Dispatcher.ShutdownStarted += (s2, _) => UserControl_Unloaded(s2, null);
        }
    }

    private WaveOutEvent? _mediaOut;
    private WaveStream? _mediaFile;
    private SoundTouchWaveProvider? _mediaProvider;
    private DispatcherTimer? _posTimer;
    private bool _initLoaded;
    private readonly object _lock = new object();

    /// <summary>
    /// Occurs when the player throws an error.
    /// </summary>
    public event EventHandler? MediaError;
    /// <summary>
    /// Occurs when media playback is finished.
    /// </summary>
    public event EventHandler? MediaFinished;

    private void UserControl_Loaded(object sender, RoutedEventArgs e)
    {
        _mediaOut = new WaveOutEvent();
        _mediaOut.PlaybackStopped += Player_PlaybackStopped;

        _posTimer = new DispatcherTimer(TimeSpan.FromMilliseconds(PositionRefreshMilliseconds), DispatcherPriority.Render, Timer_PositionChanged, Dispatcher) { IsEnabled = false };

        _mediaOut.Volume = (float)base.Volume / 100;

        if (!string.IsNullOrEmpty(Source) && !_initLoaded)
        {
            LoadMedia();
        }
    }

    private void UserControl_Unloaded(object? sender, RoutedEventArgs? e) => Dispose();

    /// <inheritdoc />
    protected override void SourceChanged(string? value)
    {
        if (!string.IsNullOrEmpty(value))
        {
            Status = PlaybackStatus.Loading;
            LoadMedia();
        }
        else
        {
            Status = PlaybackStatus.Stopped;
            _mediaOut?.Stop();
            _mediaFile?.Dispose();
            _mediaFile = null;
        }
    }

    /// <summary>
    /// Defines the Status property.
    /// </summary>
    public static readonly DependencyPropertyKey StatusProperty = DependencyProperty.RegisterReadOnly("Status", typeof(PlaybackStatus), typeof(NAudioPlayerHost),
        new PropertyMetadata(PlaybackStatus.Stopped, StatusChanged));
    /// <summary>
    /// Gets the playback status of the media player.
    /// </summary>
    public PlaybackStatus Status { get => (PlaybackStatus)GetValue(StatusProperty.DependencyProperty); protected set => SetValue(StatusProperty, value); }
    private static void StatusChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var p = (NAudioPlayerHost)d;
        p.SetDisplayText();
    }

    /// <summary>
    /// Defines the PositionRefreshMilliseconds property.
    /// </summary>
    public static readonly DependencyProperty PositionRefreshMillisecondsProperty = DependencyProperty.Register("PositionRefreshMilliseconds", typeof(int), typeof(NAudioPlayerHost),
        new PropertyMetadata(200, PositionRefreshMillisecondsChanged, CoercePositionRefreshMilliseconds));
    /// <summary>
    /// Gets or sets the interval in milliseconds at which the position bar is updated.
    /// </summary>
    public int PositionRefreshMilliseconds { get => (int)GetValue(PositionRefreshMillisecondsProperty); set => SetValue(PositionRefreshMillisecondsProperty, value); }
    private static void PositionRefreshMillisecondsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var p = (NAudioPlayerHost)d;
        if (p._posTimer != null)
        {
            p._posTimer.Interval = TimeSpan.FromMilliseconds((int)e.NewValue);
        }
    }
    private static object CoercePositionRefreshMilliseconds(DependencyObject d, object baseValue)
    {
        var value = (int)baseValue;
        return value <= 0 ? 1 : value;
    }

    /// <summary>
    /// Defines the Rate property.
    /// </summary>
    public static readonly DependencyProperty RateProperty = DependencyProperty.Register("Rate", typeof(double), typeof(NAudioPlayerHost),
        new PropertyMetadata(1.0, RateChanged, CoerceDouble));
    /// <summary>
    /// Gets or sets the playback rate as a double, where 1.0 is normal speed, 0.5 is half-speed, and 2 is double-speed.
    /// </summary>
    public double Rate { get => (double)GetValue(RateProperty); set => SetValue(RateProperty, value); }
    private static void RateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var p = (NAudioPlayerHost)d;
        if (p._mediaProvider != null)
        {
            p._mediaProvider.Rate = (double)e.NewValue;
        }
    }

    /// <summary>
    /// Defines the Pitch property.
    /// </summary>
    public static readonly DependencyProperty PitchProperty = DependencyProperty.Register("Pitch", typeof(double), typeof(NAudioPlayerHost),
        new PropertyMetadata(1.0, PitchChanged, CoerceDouble));
    /// <summary>
    /// Gets or sets the playback pitch as a double, rising or lowering the pitch by given factor without altering speed. 
    /// </summary>
    public double Pitch { get => (double)GetValue(PitchProperty); set => SetValue(PitchProperty, value); }
    private static void PitchChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var p = (NAudioPlayerHost)d;
        if (p._mediaProvider != null)
        {
            p._mediaProvider.Pitch = (double)e.NewValue;
        }
    }

    /// <summary>
    /// Defines the VolumeBoost property.
    /// </summary>
    public static readonly DependencyProperty VolumeBoostProperty = DependencyProperty.Register("VolumeBoost", typeof(double), typeof(NAudioPlayerHost),
        new PropertyMetadata(1.0, VolumeBoostChanged, CoerceDouble));
    /// <summary>
    /// Gets or sets a value that will be multiplied to the volume.
    /// </summary>
    public double VolumeBoost { get => (double)GetValue(VolumeBoostProperty); set => SetValue(VolumeBoostProperty, value); }
    private static void VolumeBoostChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var p = (NAudioPlayerHost)d;
        p.VolumeChanged(p.Volume);
    }

    /// <summary>
    /// Defines the UseEffects property.
    /// </summary>
    public static readonly DependencyProperty UseEffectsProperty = DependencyProperty.Register("UseEffects", typeof(bool), typeof(NAudioPlayerHost),
        new PropertyMetadata(false));
    /// <summary>
    /// Gets or sets whether to enable pitch-shifting effects.
    /// By default, effects are enabled if Rate, Pitch or Speed are set before loading a media file.
    /// If file is loaded at normal speed and you want to allow changing it later, this property forces initializing the effects module.
    /// This property must be set before playback. 
    /// </summary>
    public bool UseEffects { get => (bool)GetValue(UseEffectsProperty); set => SetValue(UseEffectsProperty, value); }


    private void Player_PlaybackStopped(object? sender, StoppedEventArgs e)
    {
        // if (_mediaFile == null) { return; }

        Dispatcher.Invoke(() =>
        {
            if (_mediaFile != null && (_mediaFile.TotalTime - _mediaFile.CurrentTime).TotalSeconds < 1.0)
            {
                MediaFinished?.Invoke(this, EventArgs.Empty);
            }
            base.OnMediaUnloaded();
        });
    }

    private void Player_MediaLoaded()
    {
        //Debug.WriteLine("MediaLoaded");
        Dispatcher.Invoke(() =>
        {
            Status = PlaybackStatus.Playing;
            base.Duration = _mediaFile?.TotalTime ?? TimeSpan.Zero;
            base.OnMediaLoaded();
        });
    }

    private void Timer_PositionChanged(object? sender, EventArgs e)
    {
        if (_mediaFile != null)
        {
            lock (_lock)
            {
                if (_mediaFile != null)
                {
                    base.SetPositionNoSeek(_mediaFile.CurrentTime);
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
            _ => ""
        };
    }

    /// <inheritdoc />
    protected override void PositionChanged(TimeSpan value, bool isSeeking)
    {
        base.PositionChanged(value, isSeeking);
        if (_mediaFile != null && isSeeking)
        {
            lock (_lock)
            {
                if (_mediaFile != null && isSeeking)
                {
                    _mediaFile.CurrentTime = value;
                }
            }
        }
    }

    /// <inheritdoc />
    protected override void IsPlayingChanged(bool value)
    {
        base.IsPlayingChanged(value);
        if (_mediaOut == null) { return; }

        if (value)
        {
            _mediaOut?.Play();
            _posTimer?.Start();
        }
        else
        {
            _mediaOut?.Pause();
            _posTimer?.Stop();
        }
    }

    /// <inheritdoc />
    protected override void VolumeChanged(int value)
    {
        base.VolumeChanged(value);
        if (_mediaOut != null)
        {
            _mediaOut.Volume = (float)(VolumeBoost * value / 100);
        }
    }

    /// <inheritdoc />
    protected override void SpeedChanged(double value)
    {
        base.SpeedChanged(value);
        if (_mediaProvider != null)
        {
            _mediaProvider.Tempo = value;
        }
    }

    /// <inheritdoc />
    protected override void LoopChanged(bool value)
    {
        base.LoopChanged(value);
        if (_mediaOut != null)
        {
            // _mediaOut.Loop = value;
        }
    }

    /// <inheritdoc />
    public override void Restart()
    {
        base.Restart();
        if (_mediaFile != null && _mediaOut != null)
        {
            _mediaFile.CurrentTime = TimeSpan.Zero;
            _mediaOut.Play();
        }
    }

    private void LoadMedia()
    {
        _mediaOut?.Stop();
        _mediaFile?.Dispose();
        _mediaFile = null;
        if (Source != null && _mediaOut != null)
        {
            _initLoaded = true;
            // Store locally because properties can't be accessed from new thread.
            var fileName = Source;
            var speed = GetSpeed();
            var rate = Rate;
            var pitch = Pitch;
            var autoPlay = AutoPlay;
            var useEffects = UseEffects || speed != 1 || rate != 1 || pitch != 1;
            _ = Task.Run(() =>
            {
                try
                {
                    _mediaFile = new MediaFoundationReader(fileName, 
                        new MediaFoundationReader.MediaFoundationReaderSettings() { RequestFloatOutput = true });

                    if (useEffects)
                    {
                        // Resample to 48000hz and then use SoundTouch to provide best quality.
                        var resampler = new WdlResamplingSampleProvider(_mediaFile.ToSampleProvider(), 48000);

                        _mediaProvider = new SoundTouchWaveProvider(resampler.ToWaveProvider())
                        {
                            Tempo = speed,
                            Rate = rate,
                            Pitch = pitch
                        };
                    }

                    _mediaOut.Init((IWaveProvider?)_mediaProvider ?? _mediaFile);
                    if (autoPlay)
                    {
                        _mediaOut.Play();
                        _posTimer?.Start();
                    }
                    Player_MediaLoaded();
                }
                catch
                {
                    _mediaFile?.Dispose();
                    _mediaFile = null;
                    _mediaProvider = null;
                    Status = PlaybackStatus.Error;
                    MediaError?.Invoke(this, EventArgs.Empty);
                }
            }).ConfigureAwait(false);
        }
    }

    /// <inheritdoc />
    public override void Stop()
    {
        base.Stop();
        Source = string.Empty;
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
                _mediaOut?.Dispose();
                _mediaFile?.Dispose();
                _mediaFile = null;
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
