using System;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using HanumanInstitute.MediaPlayerUI;
using NAudio.Wave;
using SoundTouch.Net.NAudioSupport;

namespace HanumanInstitute.NAudioPlayerUI
{
    public class NAudioPlayerHost : PlayerHostBase, IDisposable
    {
        static NAudioPlayerHost()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(NAudioPlayerHost), new FrameworkPropertyMetadata(typeof(NAudioPlayerHost)));
        }

        public NAudioPlayerHost()
        {
            if (!DesignerProperties.GetIsInDesignMode(this))
            {
                Loaded += UserControl_Loaded;
            }
        }

        private WaveOutEvent? _mediaOut;
        private SoundTouchWaveStream? _mediaFile;
        private DispatcherTimer? _posTimer;
        private bool _initLoaded = false;

        public event EventHandler? MediaError;
        public event EventHandler? MediaFinished;

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            _mediaOut = new WaveOutEvent();
            _mediaOut.PlaybackStopped += Player_PlaybackStopped;

            _posTimer = new DispatcherTimer(TimeSpan.FromMilliseconds(PositionRefreshMilliseconds), DispatcherPriority.Render, Timer_PositionChanged, Dispatcher) { IsEnabled = false };

            _mediaOut.Volume = (float)base.Volume / 100;
            //_mediaOut.Loop = base.Loop;

            if (Source != null && !_initLoaded)
            {
                LoadMedia();
            }
        }

        // DllPath
        public static readonly DependencyProperty DllPathProperty = DependencyProperty.Register("DllPath", typeof(string), typeof(NAudioPlayerHost));
        public string DllPath { get => (string)GetValue(DllPathProperty); set => SetValue(DllPathProperty, value); }

        // Source
        public static readonly DependencyProperty SourceProperty = DependencyProperty.Register("Source", typeof(string), typeof(NAudioPlayerHost),
            new PropertyMetadata(null, SourceChanged));
        public string Source { get => (string)GetValue(SourceProperty); set => SetValue(SourceProperty, value); }
        private static void SourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var p = (NAudioPlayerHost)d;
            if (!string.IsNullOrEmpty((string)e.NewValue))
            {
                p.Status = PlaybackStatus.Loading;
                p.LoadMedia();
            }
            else
            {
                p.Status = PlaybackStatus.Stopped;
                p._mediaOut?.Stop();
            }
        }

        // Title
        public static readonly DependencyProperty TitleProperty = DependencyProperty.Register("Title", typeof(string), typeof(NAudioPlayerHost),
            new PropertyMetadata(null, TitleChanged));
        public string Title { get => (string)GetValue(TitleProperty); set => SetValue(TitleProperty, value); }
        private static void TitleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var p = (NAudioPlayerHost)d;
            p.SetDisplayText();
        }

        // Status
        public static readonly DependencyPropertyKey StatusProperty = DependencyProperty.RegisterReadOnly("Status", typeof(PlaybackStatus), typeof(NAudioPlayerHost),
            new PropertyMetadata(PlaybackStatus.Stopped, StatusChanged));
        public PlaybackStatus Status { get => (PlaybackStatus)GetValue(StatusProperty.DependencyProperty); protected set => SetValue(StatusProperty, value); }
        private static void StatusChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var p = (NAudioPlayerHost)d;
            p.SetDisplayText();
        }

        // PositionRefreshMilliseconds
        public static readonly DependencyProperty PositionRefreshMillisecondsProperty = DependencyProperty.Register("PositionRefreshMilliseconds", typeof(int), typeof(NAudioPlayerHost),
            new PropertyMetadata(200, PositionRefreshMillisecondsChanged, CoercePositionRefreshMilliseconds));
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

        // Rate
        public static readonly DependencyProperty RateProperty = DependencyProperty.Register("Rate", typeof(double), typeof(NAudioPlayerHost),
            new PropertyMetadata(1.0, RateChanged, CoerceSpeedFloat));
        public double Rate { get => (double)GetValue(RateProperty); set => SetValue(RateProperty, value); }
        private static void RateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var p = (NAudioPlayerHost)d;
            if (p._mediaFile != null)
            {
                p._mediaFile.Rate = (double)e.NewValue;
            }
        }

        // Pitch
        public static readonly DependencyProperty PitchProperty = DependencyProperty.Register("Pitch", typeof(double), typeof(NAudioPlayerHost),
            new PropertyMetadata(1.0, PitchChanged, CoerceSpeedFloat));
        public double Pitch { get => (double)GetValue(PitchProperty); set => SetValue(PitchProperty, value); }
        private static void PitchChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var p = (NAudioPlayerHost)d;
            if (p._mediaFile != null)
            {
                p._mediaFile.Pitch = (double)e.NewValue;
            }
        }

        private void Player_MediaError()
        {
            if (MediaError != null)
            {
                Dispatcher.Invoke(() => MediaError?.Invoke(this, new EventArgs()));
            }
        }

        private void Player_PlaybackStopped(object? sender, StoppedEventArgs e)
        {
            if (_mediaFile == null) { return; }

            Dispatcher.Invoke(() =>
            {
                if (Math.Abs((_mediaFile.TotalTime - _mediaFile.CurrentTime).TotalSeconds) < 0.2)
                {
                    MediaFinished?.Invoke(this, new EventArgs());
                }
                if (!Loop)
                {
                    base.OnMediaUnloaded();
                }
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
                base.SetPositionNoSeek(_mediaFile.CurrentTime);
            }
        }

        private void SetDisplayText()
        {
            if (Status == PlaybackStatus.Loading)
            {
                Text = Properties.Resources.Loading;
            }
            else if (Status == PlaybackStatus.Playing)
            {
                Text = Title ?? System.IO.Path.GetFileName(Source);
            }
            else
            {
                Text = "";
            }
        }

        protected override void PositionChanged(TimeSpan value, bool isSeeking)
        {
            base.PositionChanged(value, isSeeking);
            if (_mediaFile != null && isSeeking)
            {
                lock (_mediaOut!)
                {
                    if (_mediaFile != null && isSeeking)
                    {
                        _mediaFile.CurrentTime = value;
                    }
                }
            }
        }

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

        protected override void VolumeChanged(int value)
        {
            base.VolumeChanged(value);
            if (_mediaOut != null)
            {
                _mediaOut.Volume = (float)value / 100;
            }
        }

        protected override void SpeedChanged(double value)
        {
            base.SpeedChanged(value);
            if (_mediaFile != null)
            {
                _mediaFile.Tempo = value;
            }
        }

        protected override void LoopChanged(bool value)
        {
            base.LoopChanged(value);
            if (_mediaOut != null)
            {
                // _mediaOut.Loop = value;
            }
        }

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
            if (Source != null && _mediaOut != null)
            {
                _initLoaded = true;
                // Store locally because properties can't be accessed from new thread.
                var fileName = Source;
                var speed = SpeedFloat;
                var rate = Rate;
                var pitch = Pitch;
                var autoPlay = AutoPlay;
                _ = Task.Run(async () =>
                {
                    try
                    {
                        using var reader = new MediaFoundationReader(fileName, new MediaFoundationReader.MediaFoundationReaderSettings() { RequestFloatOutput = true });
                        _mediaFile = new SoundTouchWaveStream(reader)
                        {
                            Tempo = speed,
                            Rate = rate,
                            Pitch = pitch
                        };
                        _mediaOut.Init(_mediaFile);
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
                        Player_MediaError();
                    }
                }).ConfigureAwait(false);
            }
        }

        public override void Stop()
        {
            base.Stop();
            Source = string.Empty;
        }


        private bool _disposedValue = false;
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    _mediaFile?.Dispose();
                    _mediaOut?.Dispose();
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
