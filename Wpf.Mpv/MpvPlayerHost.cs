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

/// <summary>
/// MPV media player to be displayed within <see cref="MediaPlayer"/>.
/// </summary>
[TemplatePart(Name = HostPartName, Type = typeof(WindowsFormsHost))]
public class MpvPlayerHost : PlayerHostBase, IDisposable
{
    static MpvPlayerHost()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(MpvPlayerHost),
            new FrameworkPropertyMetadata(typeof(MpvPlayerHost)));
    }

    /// <summary>
    /// Initializes a new instance of the MpvPlayerHost class.
    /// </summary>
    public MpvPlayerHost()
    {
        if (!DesignerProperties.GetIsInDesignMode(this))
        {
            Loaded += UserControl_Loaded;
            Unloaded += UserControl_Unloaded;
            Dispatcher.ShutdownStarted += (s2, _) => UserControl_Unloaded(s2, null);
        }
    }

    /// <summary>
    /// Releases MPV ressources.
    /// </summary>
    ~MpvPlayerHost()
    {
        Dispose(false);
    }

    /// <summary>
    /// Gets the name of the WindowsFormsHost.
    /// </summary>
    public const string HostPartName = "PART_Host";
    /// <summary>
    /// Gets the WindowsFormsHost control.
    /// </summary>
    public WindowsFormsHost? Host => _host ??= GetTemplateChild(HostPartName) as WindowsFormsHost;
    private WindowsFormsHost? _host;

    /// <summary>
    /// Gets the MpvPlayer class instance.
    /// </summary>
    public MpvPlayer? Player { get; private set; }

    /// <summary>
    /// Occurs after the media player is initialized.
    /// </summary>
    public event EventHandler? MediaPlayerInitialized;
    /// <summary>
    /// Occurs when the player throws an error.
    /// </summary>
    public event EventHandler? MediaError;
    /// <summary>
    /// Occurs when media playback is finished.
    /// </summary>
    public event EventHandler? MediaFinished;

    private bool _initLoaded;

    /// <summary>
    /// Defines the DllPath property.
    /// </summary>
    public static readonly DependencyProperty DllPathProperty =
        DependencyProperty.Register("DllPath", typeof(string), typeof(MpvPlayerHost));
    /// <summary>
    /// Gets or sets the path where MPV DLL file is located.
    /// </summary>
    public string DllPath { get => (string)GetValue(DllPathProperty); set => SetValue(DllPathProperty, value); }

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

    /// <summary>
    /// Defines the Status property.
    /// </summary>
    public static readonly DependencyPropertyKey StatusProperty = DependencyProperty.RegisterReadOnly("Status",
        typeof(PlaybackStatus), typeof(MpvPlayerHost),
        new PropertyMetadata(PlaybackStatus.Stopped, StatusChanged));
    /// <summary>
    /// Gets the playback status of the media player.
    /// </summary>
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

    /// <summary>
    /// Defines the Pitch property.
    /// </summary>
    public static readonly DependencyProperty PitchProperty = DependencyProperty.Register("Pitch", typeof(double),
        typeof(MpvPlayerHost),
        new PropertyMetadata(1.0, PitchChanged, CoerceDouble));
    /// <summary>
    /// Gets or sets the playback pitch as a double, rising or lowering the pitch by given factor without altering speed. 
    /// </summary>
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

    /// <inheritdoc />
    public override FrameworkElement? HostContainer => Host;

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

    /// <inheritdoc />
    protected override void AutoPlayChanged(bool value)
    {
        base.AutoPlayChanged(value);
        if (Player != null)
        {
            Player.AutoPlay = value;
        }
    }

    /// <inheritdoc />
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

    /// <inheritdoc />
    protected override void VolumeChanged(int value)
    {
        base.VolumeChanged(value);
        if (Player != null)
        {
            Player.Volume = value;
        }
    }

    /// <inheritdoc />
    protected override void SpeedChanged(double value)
    {
        base.SpeedChanged(value);
        if (Player != null)
        {
            Player.Speed = value;
        }
    }

    /// <inheritdoc />
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
