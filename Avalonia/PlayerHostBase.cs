using System;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable MemberCanBeProtected.Global

namespace HanumanInstitute.MediaPlayer.Avalonia;

/// <summary>
/// Represents the base class for Host controls. Each player implementation inherits from this class to manage common properties of a player.
/// </summary>
public abstract class PlayerHostBase : Control
{
    static PlayerHostBase()
    {
        FocusableProperty.OverrideMetadata(typeof(PlayerHostBase), new StyledPropertyMetadata<bool>(false));
    }

    private bool _isSettingPosition;
    private Panel? _innerControlParentCache;

    // Restart won't be triggered after Stop while this timer is running.
    private bool _isStopping;
    private DispatcherTimer? _stopTimer;

    /// <summary>
    /// Occurs after a media file is loaded.
    /// </summary>
    public event EventHandler? MediaLoaded;

    /// <summary>
    /// Occurs after a media file is unloaded.
    /// </summary>
    public event EventHandler? MediaUnloaded;

    /// <inheritdoc />
    public override void ApplyTemplate()
    {
        base.ApplyTemplate();
        _stopTimer = new DispatcherTimer(TimeSpan.FromSeconds(1), DispatcherPriority.Background, (_, _) =>
        {
            _stopTimer?.Stop();
            _isStopping = false;
        });
    }

    /// <summary>
    /// Defines the Source property.
    /// </summary>
    public static readonly DirectProperty<PlayerHostBase, string?> SourceProperty =
        AvaloniaProperty.RegisterDirect<PlayerHostBase, string?>(nameof(Source), o => o.Source, (o, v) => o.Source = v);
    private string? _source;
    /// <summary>
    /// Gets or sets the path to the media file to play.
    /// If resetting the same file path, you may need to first set an empty string before resetting the value to ensure it detects the value change.
    /// </summary>
    public string? Source
    {
        get => _source;
        set
        {
            if (SetAndRaise(SourceProperty, ref _source, value))
            {
                SourceChanged(value);
            }
        }
    }
    /// <summary>
    /// Occurs when the Source property is changed. 
    /// </summary>
    /// <param name="value">The new source.</param>
    protected virtual void SourceChanged(string? value) { }

    /// <summary>
    /// Defines the Position property.
    /// </summary>
    public static readonly DirectProperty<PlayerHostBase, TimeSpan> PositionProperty =
        AvaloniaProperty.RegisterDirect<PlayerHostBase, TimeSpan>(nameof(Position), 
            o => o.Position, (o, v) => o.Position = v);
    private TimeSpan _position;
    /// <summary>
    /// Gets or sets the playback position.
    /// </summary>
    public TimeSpan Position
    {
        get => _position;
        set
        {
            if (SetAndRaise(PositionProperty, ref _position, TimeSpan.FromTicks(Math.Max(0, Math.Min(Duration.Ticks, value.Ticks)))))
            {
                PositionChanged(_position, !_isSettingPosition);
            }
        }
    }
    /// <summary>
    /// Occurs when the Position property is changed.
    /// </summary>
    /// <param name="value">The new position.</param>
    /// <param name="isSeeking">True if position is set by seeking, False if set by playback.</param>
    protected virtual void PositionChanged(TimeSpan value, bool isSeeking) { }

    /// <summary>
    /// Defines the Duration property.
    /// </summary>
    public static readonly DirectProperty<PlayerHostBase, TimeSpan> DurationProperty =
        AvaloniaProperty.RegisterDirect<PlayerHostBase, TimeSpan>(nameof(Duration), 
            o => o.Duration, (o, v) => o.Duration = v);
    private TimeSpan _duration = TimeSpan.FromSeconds(1);
    /// <summary>
    /// Gets the duration of the media file.
    /// </summary>
    public TimeSpan Duration
    {
        get => _duration;
        protected set
        {
            if (SetAndRaise(DurationProperty, ref _duration, value))
            {
                DurationChanged(value);
            }
        }
    }
    /// <summary>
    /// Occurs when the Duration property is changed.
    /// </summary>
    /// <param name="value">The new duration.</param>
    protected virtual void DurationChanged(TimeSpan value) { }

    /// <summary>
    /// Defines the IsPlaying property.
    /// </summary>
    public static readonly DirectProperty<PlayerHostBase, bool> IsPlayingProperty =
        AvaloniaProperty.RegisterDirect<PlayerHostBase, bool>(nameof(IsPlaying), 
            o => o.IsPlaying, (o, v) => o.IsPlaying = v);
    private bool _isPlaying;
    /// <summary>
    /// Gets or sets whether the media file is playing or paused.
    /// </summary>
    public bool IsPlaying
    {
        get => _isPlaying;
        set
        {
            if (SetAndRaise(IsPlayingProperty, ref _isPlaying, CoerceIsPlaying(value)))
            {
                IsPlayingChanged(_isPlaying);
            }
        }
    }
    /// <summary>
    /// Occurs when IsPlaying property is changed.
    /// </summary>
    /// <param name="value">The new playing status.</param>
    protected virtual void IsPlayingChanged(bool value) { }
    /// <summary>
    /// Validates the IsPlaying property value.
    /// </summary>
    /// <param name="value">The new IsPlaying value.</param>
    /// <returns>The value after validation.</returns>
    protected virtual bool CoerceIsPlaying(bool value) => value;

    /// <summary>
    /// Defines the Volume property.
    /// </summary>
    public static readonly DirectProperty<PlayerHostBase, int> VolumeProperty =
        AvaloniaProperty.RegisterDirect<PlayerHostBase, int>(nameof(Volume), 
            o => o.Volume, (o, v) => o.Volume = v);
    private int _volume = 100;
    /// <summary>
    /// Gets or sets the volume.
    /// </summary>
    public int Volume
    {
        get => _volume;
        set
        {
            if (SetAndRaise(VolumeProperty, ref _volume, CoerceVolume(value)))
            {
                VolumeChanged(_volume);
            }
        }
    }
    /// <summary>
    /// Occurs when the Volume property is changed.
    /// </summary>
    /// <param name="value">The new volume.</param>
    protected virtual void VolumeChanged(int value) { }
    /// <summary>
    /// Validates the Volume property value.
    /// </summary>
    /// <param name="value">The new volume.</param>
    /// <returns>The volume after validation.</returns>
    protected virtual int CoerceVolume(int value)
    {
        return Math.Max(0, (Math.Min(100, value)));
    }

    /// <summary>
    /// Defines the SpeedFloat property.
    /// </summary>
    public static readonly DirectProperty<PlayerHostBase, double> SpeedFloatProperty =
        AvaloniaProperty.RegisterDirect<PlayerHostBase, double>(nameof(SpeedFloat), 
            o => o.SpeedFloat, (o, v) => o.SpeedFloat = v);
    private double _speedFloat = 1;
    /// <summary>
    /// Gets or sets the speed multiplier.
    /// </summary>
    public double SpeedFloat
    {
        get => _speedFloat;
        set
        {
            if (SetAndRaise(SpeedFloatProperty, ref _speedFloat, CoerceDouble(value)))
            {
                SpeedChanged(GetSpeed());
            }
        }
    }
    /// <summary>
    /// Occurs when the Speed property is changed.
    /// </summary>
    /// <param name="value">The new speed value.</param>
    protected virtual void SpeedChanged(double value) { }

    /// <summary>
    /// Defines the SpeedInt property.
    /// </summary>
    public static readonly DirectProperty<PlayerHostBase, int> SpeedIntProperty =
        AvaloniaProperty.RegisterDirect<PlayerHostBase, int>(nameof(SpeedInt), 
            o => o.SpeedInt, (o, v) => o.SpeedInt = v);
    private int _speedInt;
    /// <summary>
    /// Gets or sets the speed as an integer, where normal playback is 0. Useful for binding to a slider.
    /// </summary>
    public int SpeedInt
    {
        get => _speedInt;
        set
        {
            if (SetAndRaise(SpeedIntProperty, ref _speedInt, CoerceSpeedInt(value)))
            {
                SpeedChanged(GetSpeed());
            }
        }
    }
    /// <summary>
    /// Occurs when the SpeedInt property is changed.
    /// </summary>
    /// <param name="value">The new speed value.</param>
    /// <returns>The speed value after validation.</returns>
    protected virtual int CoerceSpeedInt(int value) => value;

    /// <summary>
    /// Defines the Loop property.
    /// </summary>
    public static readonly DirectProperty<PlayerHostBase, bool> LoopProperty =
        AvaloniaProperty.RegisterDirect<PlayerHostBase, bool>(nameof(Loop), 
            o => o.Loop, (o, v) => o.Loop = v);
    private bool _loop;
    /// <summary>
    /// Gets or sets whether to loop.
    /// </summary>
    public bool Loop
    {
        get => _loop;
        set
        {
            if (SetAndRaise(LoopProperty, ref _loop, CoerceLoop(value)))
            {
                LoopChanged(_loop);
            }
        }
    }
    /// <summary>
    /// Occurs when the Loop property is changed.
    /// </summary>
    /// <param name="value">The new Loop value.</param>
    protected virtual void LoopChanged(bool value) { }
    /// <summary>
    /// Validates the Loop value.
    /// </summary>
    /// <param name="value">The new loop value.</param>
    /// <returns>The loop value after validation.</returns>
    protected virtual bool CoerceLoop(bool value) => value;
    
    /// <summary>
    /// Defines the AutoPlay property.
    /// </summary>
    public static readonly DirectProperty<PlayerHostBase, bool> AutoPlayProperty =
        AvaloniaProperty.RegisterDirect<PlayerHostBase, bool>(nameof(AutoPlay), 
            o => o.AutoPlay, (o, v) => o.AutoPlay = v);
    private bool _autoPlay = true;
    /// <summary>
    /// Gets or sets whether to auto-play the file when setting the source.
    /// </summary>
    public bool AutoPlay
    {
        get => _autoPlay;
        set
        {
            if (SetAndRaise(AutoPlayProperty, ref _autoPlay, CoerceAutoPlay(value)))
            {
                AutoPlayChanged(_autoPlay);
            }
        }
    }
    /// <summary>
    /// Occurs when the AutoPlay property is changed.
    /// </summary>
    /// <param name="value">The new AutoPlay value.</param>
    protected virtual void AutoPlayChanged(bool value)
    {
        // AutoPlay can be set AFTER Script, we need to reset IsPlaying in that case.
        // Default value needs to be false otherwise it can cause video to start loading and immediately stop which can cause issues.
        IsPlaying = value;
    }
    /// <summary>
    /// Validates the AutoPlay value.
    /// </summary>
    /// <param name="value">The new auto play value.</param>
    /// <returns>The value after validation.</returns>
    protected virtual bool CoerceAutoPlay(bool value) => value;
    
    /// <summary>
    /// Defines the Title property.
    /// </summary>
    public static readonly DirectProperty<PlayerHostBase, string?> TitleProperty =
        AvaloniaProperty.RegisterDirect<PlayerHostBase, string?>(nameof(Title), o => o.Title, (o, v) => o.Title = v);
    private string? _title;
    /// <summary>
    /// Gets or sets the display title of the media file.
    /// A title is set by default and you can override it using this property.
    /// </summary>
    public string? Title
    {
        get => _title;
        set
        {
            _title = value;
            SetDisplayText();
        }
    }
    
    /// <summary>
    /// Defines the Text property.
    /// </summary>
    public static readonly DirectProperty<PlayerHostBase, string> TextProperty =
        AvaloniaProperty.RegisterDirect<PlayerHostBase, string>(nameof(Text), 
            o => o.Text, (o, v) => o.Text = v);
    private string _text = string.Empty;
    /// <summary>
    /// Gets the text being displayed, which may include the title and/or loading status.
    /// </summary>
    public string Text
    {
        get => _text;
        protected set
        {
            SetAndRaise(TextProperty, ref _text, value);
        }
    }

    /// <summary>
    /// Defines the IsMediaLoaded property.
    /// </summary>
    public static readonly DirectProperty<PlayerHostBase, bool> IsMediaLoadedProperty =
        AvaloniaProperty.RegisterDirect<PlayerHostBase, bool>(nameof(IsMediaLoaded), 
            o => o.IsMediaLoaded, (o, v) => o.IsMediaLoaded = v);
    private bool _isMediaLoaded;
    /// <summary>
    /// Gets whether a media file is loaded.
    /// </summary>
    public bool IsMediaLoaded
    {
        get => _isMediaLoaded;
        private set
        {
            SetAndRaise(IsMediaLoadedProperty, ref _isMediaLoaded, value);
        }
    }

    /// <summary>
    /// Defines the IsVideoVisible property.
    /// </summary>
    public static readonly DirectProperty<PlayerHostBase, bool> IsVideoVisibleProperty =
        AvaloniaProperty.RegisterDirect<PlayerHostBase, bool>(nameof(IsVideoVisible), 
            o => o.IsVideoVisible, (o, v) => o.IsVideoVisible = v);
    private bool _isVideoVisible;
    /// <summary>
    /// Gets or sets whether to show the video.
    /// </summary>
    public bool IsVideoVisible
    {
        get => _isVideoVisible;
        set
        {
            if (SetAndRaise(IsVideoVisibleProperty, ref _isVideoVisible, CoerceIsVideoVisible(value)))
            {
                IsVideoVisibleChanged(_isVideoVisible);
            }
        }
    }
    /// <summary>
    /// Occurs when the IsVideoVisible property is changed.
    /// </summary>
    /// <param name="value">The new visibility value.</param>
    protected virtual void IsVideoVisibleChanged(bool value)
    {
        if (HostContainer != null)
        {
            HostContainer.IsVisible = value;
        }
    }
    /// <summary>
    /// Validates the IsVideoVisible value.
    /// </summary>
    /// <param name="value">The new visibility value.</param>
    /// <returns>The value after validation.</returns>
    protected virtual bool CoerceIsVideoVisible(bool value) => value;

    /// <summary>
    /// Ensures that a double is above 0. 
    /// </summary>
    /// <param name="value">A double value.</param>
    /// <returns>The value after validation.</returns>
    protected static double CoerceDouble(double value)
    {
        return !(value > 0) ? 1 : value;
    }

    /// <summary>
    /// Sets the Text property.
    /// </summary>
    protected abstract void SetDisplayText();

    /// <summary>
    /// Sets the position without raising PositionChanged.
    /// </summary>
    /// <param name="pos">The position value to set.</param>
    protected virtual void SetPositionNoSeek(TimeSpan pos)
    {
        _isSettingPosition = true;
        Position = pos;
        _isSettingPosition = false;
    }

    /// <summary>
    /// Returns the media player control UI. This object will be transferred during full-screen mode.
    /// </summary>
    public virtual Visual? HostContainer { get; } = null;

    /// <summary>
    /// Stops playback and unloads media file.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1716:Identifiers should not match keywords",
        Justification = "Reviewed: It's the right name.")]
    public virtual void Stop()
    {
        _isStopping = true;
        // Use timer for Loop feature if player doesn't support it natively, but not after pressing Stop.
        _stopTimer?.Stop();
        _stopTimer?.Start();
    }

    /// <summary>
    /// Restarts playback.
    /// </summary>
    public virtual void Restart() { }

    /// <summary>
    /// Must be called by the derived class when media is loaded.
    /// </summary>
    protected void OnMediaLoaded()
    {
        SetPositionNoSeek(TimeSpan.Zero);
        IsMediaLoaded = true;
        IsPlaying = AutoPlay;
        MediaLoaded?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Must be called by the derived class when media is unloaded.
    /// </summary>
    protected void OnMediaUnloaded()
    {
        if (Loop && !_isStopping)
        {
            Restart();
        }
        else
        {
            IsMediaLoaded = false;
            Duration = TimeSpan.FromSeconds(1);
            MediaUnloaded?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>
    /// Returns the playback speed. If SpeedInt is defined (0-based), it will calculate a float value based on it, otherwise SpeedFloat (1-based) is returned.
    /// </summary>
    /// <returns></returns>
    public double GetSpeed()
    {
        if (SpeedInt == 0)
        {
            return SpeedFloat;
        }
        else
        {
            var factor = SpeedInt / 8.0;
            return factor < 0 ? 1 / (1 - factor) : 1 * (1 + factor);
        }
    }

    /// <summary>
    /// Returns the container of InnerControl the first time it is called and maintain reference to that container.
    /// </summary>
    public Panel GetInnerControlParent()
    {
        _innerControlParentCache ??= HostContainer?.Parent as Panel;

        if (_innerControlParentCache == null)
        {
            throw new InvalidCastException(string.Format(CultureInfo.InvariantCulture,
                Properties.Resources.ParentMustBePanel, "PlayerHostBase", HostContainer?.Parent?.GetType()));
        }

        return _innerControlParentCache;
    }
}
