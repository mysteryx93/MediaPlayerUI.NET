using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;
using HanumanInstitute.MediaPlayer.Avalonia.Helpers;
using ManagedBass;
using ManagedBass.Fx;

// ReSharper disable ConstantNullCoalescingCondition

namespace HanumanInstitute.MediaPlayer.Avalonia.Bass
{
    public class BassPlayerHost : PlayerHostBase, IDisposable
    {
        public BassPlayerHost()
        {
            if (!Design.IsDesignMode)
            {
                Initialized += UserControl_Loaded;
                this.FindAncestor<TopLevel>()!.Closed += (_, _) => Dispose();
                // Dispatcher.ShutdownStarted += (s2, e2) => UserControl_Unloaded(s2, null);
            }
        }

        // BASS audio stream handle.
        private int _chan;
        // BASS audio stream effect handle.
        private int _fx;
        // Timer to get position.
        private DispatcherTimer? _posTimer;
        /// <summary>
        /// Whether LoadMedia has ever been called.
        /// </summary>
        private bool _initLoaded;
        private readonly object _lock = new object();

        public event EventHandler? MediaError;
        public event EventHandler? MediaFinished;

        private void UserControl_Loaded(object? sender, EventArgs e)
        {
            ManagedBass.Bass.ChannelSetSync(_chan, SyncFlags.End | SyncFlags.Mixtime, 0, Player_PlaybackStopped);
            // ManagedBass.Bass.ChannelSetSync(_chan, SyncFlags.Position, 0, Player_PositionChanged);

            _posTimer = new DispatcherTimer(TimeSpan.FromMilliseconds(PositionRefreshMilliseconds),
                DispatcherPriority.Render, Timer_PositionChanged) { IsEnabled = false };

            // _mediaOut.Volume = (float)base.Volume / 100;

            if (!string.IsNullOrEmpty(Source) && !_initLoaded)
            {
                LoadMedia();
            }
        }

        // DllPath
        public static readonly DirectProperty<BassPlayerHost, string?> DllPathProperty =
            AvaloniaProperty.RegisterDirect<BassPlayerHost, string?>(nameof(DllPath), o => o.DllPath);
        public string? DllPath { get; set; }

        // Source
        public static readonly DirectProperty<BassPlayerHost, string?> SourceProperty =
            AvaloniaProperty.RegisterDirect<BassPlayerHost, string?>(nameof(Source), o => o.DllPath);
        private string? _source;
        public string? Source
        {
            get => _source;
            set
            {
                _source = value;

                if (!string.IsNullOrEmpty(value))
                {
                    Status = PlaybackStatus.Loading;
                    LoadMedia();
                }
                else
                {
                    Status = PlaybackStatus.Stopped;
                    ReleaseChannel();
                }
            }
        }

        // Title
        public static readonly DirectProperty<BassPlayerHost, string> TitleProperty =
            AvaloniaProperty.RegisterDirect<BassPlayerHost, string>(nameof(Title), o => o.Title);
        private string _title = string.Empty;
        public string Title
        {
            get => _title;
            set
            {
                _title = value ?? string.Empty;
                SetDisplayText();
            }
        }

        // Status
        public static readonly DirectProperty<BassPlayerHost, PlaybackStatus> StatusProperty =
            AvaloniaProperty.RegisterDirect<BassPlayerHost, PlaybackStatus>(nameof(Status), o => o.Status);
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
                o => o.PositionRefreshMilliseconds);
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
                }
            }
        }

        // Rate
        public static readonly DirectProperty<BassPlayerHost, double> RateProperty =
            AvaloniaProperty.RegisterDirect<BassPlayerHost, double>(nameof(Rate), o => o.Rate);
        private double _rate = 1;
        public double Rate
        {
            get => _rate;
            set
            {
                _rate = CoerceDouble(value);
                if (BassActive)
                {
                    // _mediaProvider.Rate = (double)e.NewValue;
                }
            }
        }

        // Pitch
        public static readonly DirectProperty<BassPlayerHost, double> PitchProperty =
            AvaloniaProperty.RegisterDirect<BassPlayerHost, double>(nameof(Pitch), o => o.Pitch);
        private double _pitch;
        public double Pitch
        {
            get => _pitch;
            set
            {
                _pitch = CoerceDouble(value);
                if (BassActive)
                {
                    ManagedBass.Bass.FXSetParameters(_fx,
                        new PitchShiftParameters() { fPitchShift = 432f / 440, lOsamp = 8 }).Valid();
                }
            }
        }

        // VolumeBoost
        public static readonly DirectProperty<BassPlayerHost, double> VolumeBoostProperty =
            AvaloniaProperty.RegisterDirect<BassPlayerHost, double>(nameof(VolumeBoost), o => o.VolumeBoost);
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
            AvaloniaProperty.RegisterDirect<BassPlayerHost, bool>(nameof(UseEffects), o => o.UseEffects);
        public bool UseEffects { get; set; }

        private bool BassActive => _chan != 0;

        private TimeSpan BassDuration =>
            TimeSpan.FromSeconds(ManagedBass.Bass.ChannelBytes2Seconds(_chan, ManagedBass.Bass.ChannelGetLength(_chan)));

        private TimeSpan BassPosition =>
            TimeSpan.FromSeconds(ManagedBass.Bass.ChannelBytes2Seconds(_chan, ManagedBass.Bass.ChannelGetPosition(_chan)));

        private void Player_PlaybackStopped(int handle, int channel, int data, IntPtr user)
        {
            Dispatcher.UIThread.InvokeAsync(() =>
            {
                if (BassActive && (base.Duration - BassPosition).TotalSeconds < 1.0)
                {
                    MediaFinished?.Invoke(this, EventArgs.Empty);
                }

                base.OnMediaUnloaded();
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
                        ManagedBass.Bass.ChannelSetPosition(_chan, ManagedBass.Bass.ChannelSeconds2Bytes(_chan, value.TotalSeconds));
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
                ManagedBass.Bass.ChannelPause(_chan).Valid();
                _posTimer?.Stop();
            }
        }

        protected override void VolumeChanged(int value)
        {
            base.VolumeChanged(value);
            if (BassActive)
            {
                ManagedBass.Bass.ChannelSetAttribute(_chan, ChannelAttribute.Volume, (float)(VolumeBoost * value / 100));
            }
        }

        protected override void SpeedChanged(double value)
        {
            base.SpeedChanged(value);
            if (BassActive)
            {
                // _mediaProvider.Tempo = value;
            }
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
                var speed = GetSpeed();
                var rate = Rate;
                var pitch = Pitch;
                var autoPlay = AutoPlay;
                var useEffects = UseEffects || speed != 1.0 || rate != 1.0 || pitch != 1.0;
                _ = Task.Run(() =>
                {
                    try
                    {
                        _chan = ManagedBass.Bass.CreateStream(fileName, Flags: BassFlags.AutoFree | BassFlags.Decode).Valid();

                        if (useEffects)
                        {
                            _fx = ManagedBass.Bass.ChannelSetFX(_chan, EffectType.PitchShift, 10).Valid();
                            ManagedBass.Bass.FXSetParameters(_fx,
                                new PitchShiftParameters() { fPitchShift = 432f / 440, lOsamp = 8 }).Valid();
                        }

                        if (autoPlay)
                        {
                            ManagedBass.Bass.ChannelPlay(_chan);
                            _posTimer?.Start();
                        }

                        Player_MediaLoaded();
                    }
                    catch
                    {
                        ReleaseChannel();
                        MediaError?.Invoke(this, EventArgs.Empty);
                    }
                }).ConfigureAwait(false);
            }
        }

        public override void Stop()
        {
            base.Stop();
            Source = string.Empty;
        }

        private void ReleaseChannel()
        {
            ManagedBass.Bass.SampleFree(_chan).Valid();
            _chan = 0;
            _fx = 0;
        }


        private bool _disposedValue;

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

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
