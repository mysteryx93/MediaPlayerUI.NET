using System;
using System.ComponentModel;
using System.Globalization;
using System.Threading;
using System.Windows;
using System.Windows.Forms.Integration;
using Mpv.NET.Player;
// ReSharper disable CompareOfFloatsByEqualityOperator
// ReSharper disable InconsistentNaming

namespace HanumanInstitute.MediaPlayer.Wpf.Mpv;

[TemplatePart(Name = HostPartName, Type = typeof(WindowsFormsHost))]
public class MpvPlayerHost : PlayerHostBase
{
    static MpvPlayerHost()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(MpvPlayerHost),
            new FrameworkPropertyMetadata(typeof(MpvPlayerHost)));
    }

    public MpvPlayerHost()
    {
        if (!DesignerProperties.GetIsInDesignMode(this))
        {
            Loaded += UserControl_Loaded;
            Unloaded += UserControl_Unloaded;
            Dispatcher.ShutdownStarted += (s2, _) => UserControl_Unloaded(s2, null);
        }
    }

    public const string HostPartName = "PART_Host";
    public WindowsFormsHost? Host => _host ??= GetTemplateChild(HostPartName) as WindowsFormsHost;
    private WindowsFormsHost? _host;

    public MpvPlayer? Player { get; private set; }
    public event EventHandler? MediaPlayerInitialized;
    public event EventHandler? MediaError;
    public event EventHandler? MediaFinished;

    private bool _initLoaded;

    // DllPath
    public static readonly DependencyProperty DllPathProperty =
        DependencyProperty.Register("DllPath", typeof(string), typeof(MpvPlayerHost));

    public string DllPath { get => (string)GetValue(DllPathProperty); set => SetValue(DllPathProperty, value); }

    // Source
    public static readonly DependencyProperty SourceProperty = DependencyProperty.Register("Source", typeof(string),
        typeof(MpvPlayerHost),
        new PropertyMetadata(null, SourceChanged));

    public string Source { get => (string)GetValue(SourceProperty); set => SetValue(SourceProperty, value); }

    private static void SourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var p = (MpvPlayerHost)d;
        if (!string.IsNullOrEmpty((string)e.NewValue))
        {
            p.Status = PlaybackStatus.Loading;
            p.LoadMedia();
        }
        else
        {
            p.Status = PlaybackStatus.Stopped;
            p.Player?.Stop();
        }
    }

    // Title
    public static readonly DependencyProperty TitleProperty = DependencyProperty.Register("Title", typeof(string),
        typeof(MpvPlayerHost),
        new PropertyMetadata(null, TitleChanged));

    public string? Title { get => (string)GetValue(TitleProperty); set => SetValue(TitleProperty, value); }

    private static void TitleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var p = (MpvPlayerHost)d;
        p.SetDisplayText();
    }

    // Status
    public static readonly DependencyPropertyKey StatusProperty = DependencyProperty.RegisterReadOnly("Status",
        typeof(PlaybackStatus), typeof(MpvPlayerHost),
        new PropertyMetadata(PlaybackStatus.Stopped, StatusChanged));

    public PlaybackStatus Status
    {
        get => (PlaybackStatus)GetValue(StatusProperty.DependencyProperty);
        protected set => SetValue(StatusProperty, value);
    }

    private static void StatusChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var p = (MpvPlayerHost)d;
        p.SetDisplayText();
    }

    // Pitch
    public static readonly DependencyProperty PitchProperty = DependencyProperty.Register("Pitch", typeof(double),
        typeof(MpvPlayerHost),
        new PropertyMetadata(1.0, PitchChanged, CoerceDouble));

    public double Pitch { get => (double)GetValue(PitchProperty); set => SetValue(PitchProperty, value); }

    private static void PitchChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var p = (MpvPlayerHost)d;
        if (p.Player != null)
        {
            // ReSharper disable once StringLiteralTypo
            p.Player.API.SetPropertyString("af",
                string.Format(CultureInfo.InvariantCulture, "rubberband=pitch-scale={0}", e.NewValue));
        }
    }

    private void UserControl_Loaded(object sender, RoutedEventArgs e)
    {
        if (Host == null)
        {
            throw new InvalidCastException(string.Format(CultureInfo.InvariantCulture,
                Properties.Resources.TemplateElementNotFound, HostPartName, nameof(WindowsFormsHost)));
        }

        Player = new MpvPlayer(Host.Handle, DllPath);

        Player.MediaError += Player_MediaError;
        Player.MediaFinished += Player_MediaFinished;
        Player.MediaLoaded += Player_MediaLoaded;
        Player.MediaUnloaded += Player_MediaUnloaded;
        Player.PositionChanged += Player_PositionChanged;

        Player.AutoPlay = base.AutoPlay;
        Player.Volume = base.Volume;
        Player.Speed = base.GetSpeed();
        Player.Loop = base.Loop;
        if (Pitch != 1)
        {
            Player.API.SetPropertyString("af",
                // ReSharper disable once StringLiteralTypo
                string.Format(CultureInfo.InvariantCulture, "rubberband=pitch-scale={0}", Pitch));
        }

        MediaPlayerInitialized?.Invoke(this, EventArgs.Empty);

        if (!string.IsNullOrEmpty(Source) && !_initLoaded)
        {
            LoadMedia();
        }
    }

    private void Player_MediaError(object? sender, EventArgs e)
    {
        Dispatcher.Invoke(() =>
        {
            Status = PlaybackStatus.Error;
            MediaError?.Invoke(this, EventArgs.Empty);
        });
    }

    private void Player_MediaFinished(object? sender, EventArgs e)
    {
        if (MediaFinished != null)
        {
            Dispatcher.Invoke(() =>
                MediaFinished?.Invoke(this, EventArgs.Empty));
        }
    }

    private void Player_MediaLoaded(object? sender, EventArgs e)
    {
        //Debug.WriteLine("MediaLoaded");
        Dispatcher.Invoke(() =>
        {
            Status = PlaybackStatus.Playing;
            base.Duration = Player!.Duration;
            base.OnMediaLoaded();
        });
    }

    private void Player_MediaUnloaded(object? sender, EventArgs e)
    {
        //Debug.WriteLine("MediaUnloaded");
        Dispatcher.Invoke(() => base.OnMediaUnloaded());
    }

    private void Player_PositionChanged(object? sender, MpvPlayerPositionChangedEventArgs e)
    {
        Dispatcher.BeginInvoke(new Action(() => base.SetPositionNoSeek(e.NewPosition)));
    }

    private void UserControl_Unloaded(object? sender, RoutedEventArgs? e)
    {
        Player?.Dispose();
        Player = null;
    }

    public override FrameworkElement? HostContainer => Host;

    private void SetDisplayText()
    {
        Text = Status switch
        {
            PlaybackStatus.Loading => Properties.Resources.Loading,
            PlaybackStatus.Playing => Title ?? System.IO.Path.GetFileName(Source),
            PlaybackStatus.Error => Properties.Resources.MediaError,
            _ => ""
        };
    }

    protected override void PositionChanged(TimeSpan value, bool isSeeking)
    {
        base.PositionChanged(value, isSeeking);
        if (Player != null)
        {
            lock (Player)
            {
                if (Player is { IsMediaLoaded: true } && isSeeking)
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

    protected override void SpeedChanged(double value)
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
        if (Player == null) { return; }

        Player.Stop();
        if (!string.IsNullOrEmpty(Source))
        {
            _initLoaded = true;
            Thread.Sleep(10);
            Player.Load(Source, true);
        }
    }

    public override void Stop()
    {
        base.Stop();
        Source = string.Empty;
    }
}
