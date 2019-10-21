using EmergenceGuardian.MediaPlayerUI;
using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Forms.Integration;

namespace EmergenceGuardian.MpvPlayerUI
{
    [TemplatePart(Name = MpvMediaPlayerHost.PART_Host, Type = typeof(WindowsFormsHost))]
    public class MpvMediaPlayerHost : PlayerHostBase
    {
        static MpvMediaPlayerHost()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(MpvMediaPlayerHost), new FrameworkPropertyMetadata(typeof(MpvMediaPlayerHost)));
        }

        public const string PART_Host = "PART_Host";
        public WindowsFormsHost Host => GetTemplateChild(PART_Host) as WindowsFormsHost;

        public Mpv.NET.Player.MpvPlayer Player;
        public event EventHandler MediaPlayerInitialized;
        private bool _initLoaded = false;

        public MpvMediaPlayerHost()
        { }

        // DllPath
        public static readonly DependencyProperty DllPathProperty = DependencyProperty.Register("DllPath", typeof(string), typeof(MpvMediaPlayerHost));
        public string DllPath { get => (string)GetValue(DllPathProperty); set => SetValue(DllPathProperty, value); }

        // Source
        public static readonly DependencyProperty SourceProperty = DependencyProperty.Register("Source", typeof(string), typeof(MpvMediaPlayerHost),
            new PropertyMetadata(null, SourceChanged));
        public string Source { get => (string)GetValue(SourceProperty); set => SetValue(SourceProperty, value); }
        private static void SourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            MpvMediaPlayerHost P = d as MpvMediaPlayerHost;
            if (!string.IsNullOrEmpty((string)e.NewValue))
            {
                P.Status = PlaybackStatus.Loading;
                P.LoadMedia();
            }
            else
            {
                P.Status = PlaybackStatus.Stopped;
                P.Player.Stop();
            }
        }

        // Title
        public static readonly DependencyProperty TitleProperty = DependencyProperty.Register("Title", typeof(string), typeof(MpvMediaPlayerHost),
            new PropertyMetadata(null, TitleChanged));
        public string Title { get => (string)GetValue(TitleProperty); set => SetValue(TitleProperty, value); }
        private static void TitleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            MpvMediaPlayerHost P = d as MpvMediaPlayerHost;
            P.SetDisplayText();
        }

        // Status
        public static readonly DependencyPropertyKey StatusProperty = DependencyProperty.RegisterReadOnly("Status", typeof(PlaybackStatus), typeof(MpvMediaPlayerHost),
            new PropertyMetadata(PlaybackStatus.Stopped, StatusChanged));
        public PlaybackStatus Status { get => (PlaybackStatus)GetValue(StatusProperty.DependencyProperty); protected set => SetValue(StatusProperty, value); }
        private static void StatusChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            MpvMediaPlayerHost P = d as MpvMediaPlayerHost;
            P.SetDisplayText();
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            if (DesignerProperties.GetIsInDesignMode(this))
            {
                return;
            }

            Loaded += UserControl_Loaded;
            Unloaded += UserControl_Unloaded;
            Dispatcher.ShutdownStarted += (s2, e2) => UserControl_Unloaded(s2, null);
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            Player = new Mpv.NET.Player.MpvPlayer(Host.Handle, DllPath);

            Player.MediaError += Player_MediaError;
            Player.MediaFinished += Player_MediaFinished;
            Player.MediaLoaded += Player_MediaLoaded;
            Player.MediaUnloaded += Player_MediaUnloaded;
            Player.PositionChanged += Player_PositionChanged;

            Player.AutoPlay = base.AutoPlay;
            Player.Volume = base.Volume;
            Player.Speed = base.GetSpeed();
            Player.Loop = base.Loop;

            MediaPlayerInitialized?.Invoke(this, new EventArgs());

            if (Source != null && !_initLoaded)
            {
                LoadMedia();
            }
        }

        public event EventHandler MediaError;

        private void Player_MediaError(object sender, EventArgs e)
        {
            if (MediaError != null)
            {
                Dispatcher.Invoke(() =>
                    MediaError?.Invoke(this, new EventArgs()));
            }
        }

        public event EventHandler MediaFinished;

        private void Player_MediaFinished(object sender, EventArgs e)
        {
            if (MediaFinished != null)
            {
                Dispatcher.Invoke(() =>
                    MediaFinished?.Invoke(this, new EventArgs()));
            }
        }

        private void Player_MediaLoaded(object sender, EventArgs e)
        {
            //Debug.WriteLine("MediaLoaded");
            Dispatcher.Invoke(() =>
            {
                Status = PlaybackStatus.Playing;
                base.Duration = Player.Duration;
                base.RaiseMediaLoaded();
            });
        }

        private void Player_MediaUnloaded(object sender, EventArgs e)
        {
            //Debug.WriteLine("MediaUnloaded");
            Dispatcher.Invoke(() => base.RaiseMediaUnloaded());
        }

        private void Player_PositionChanged(object sender, Mpv.NET.Player.MpvPlayerPositionChangedEventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(() => base.SetPositionNoSeek(e.NewPosition)));
        }

        private void UserControl_Unloaded(object sender, RoutedEventArgs e)
        {
            Player?.Dispose();
            Player = null;
        }

        public override FrameworkElement InnerControl => Host;

        private void SetDisplayText()
        {
            if (Status == PlaybackStatus.Loading)
            {
                Text = "Loading...";
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
            if (Player != null)
            {
                lock (Player)
                {
                    if (Player != null && Player.IsMediaLoaded && isSeeking)
                    {
                        Player.Position = value;
                    }
                }
            }
        }

        protected override void AutoPlayChanged(bool value)
        {
            base.AutoPlayChanged(value);
            if (Player != null)
            {
                Player.AutoPlay = value;
            }
        }

        protected override void IsPlayingChanged(bool value)
        {
            base.IsPlayingChanged(value);
            if (value)
            {
                Player?.Resume();
            }
            else
            {
                Player?.Pause();
            }
        }

        protected override void VolumeChanged(int value)
        {
            base.VolumeChanged(value);
            if (Player != null)
            {
                Player.Volume = value;
            }
        }

        protected override void SpeedChanged(float value)
        {
            base.SpeedChanged(value);
            if (Player != null)
            {
                Player.Speed = value;
            }
        }

        protected override void LoopChanged(bool value)
        {
            base.LoopChanged(value);
            if (Player != null)
            {
                Player.Loop = value;
            }
        }

        private void LoadMedia()
        {
            Player?.Stop();
            if (Source != null && Player != null)
            {
                _initLoaded = true;
                Player.Load(Source, true);
            }
        }

        public override void Stop()
        {
            base.Stop();
            Source = null;
        }
    }
}
