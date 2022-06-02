using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
// ReSharper disable InconsistentNaming

namespace HanumanInstitute.MediaPlayer.Wpf;

/// <summary>
/// Represents the base class for Host controls. Each player implementation inherits from this class to manage common properties of a player.
/// </summary>
public abstract class PlayerHostBase : Control
{
    static PlayerHostBase()
    {
        FocusableProperty.OverrideMetadata(typeof(PlayerHostBase), new FrameworkPropertyMetadata(false));
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
    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        _stopTimer = new DispatcherTimer(TimeSpan.FromSeconds(1), DispatcherPriority.Background, (_, _) =>
        {
            _stopTimer?.Stop();
            _isStopping = false;
        }, Dispatcher);
    }

    /// <summary>
    /// Defines the Source property.
    /// </summary>
    public static readonly DependencyProperty SourceProperty = DependencyProperty.Register("Source", typeof(string), typeof(PlayerHostBase),
        new PropertyMetadata(null, SourceChanged));
    /// <summary>
    /// Gets or sets the path to the media file to play.
    /// If resetting the same file path, you may need to first set an empty string before resetting the value to ensure it detects the value change.
    /// </summary>
    public string? Source { get => (string)GetValue(SourceProperty); set => SetValue(SourceProperty, value); }
    private static void SourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var p = (PlayerHostBase)d;
        p.SourceChanged((string?)e.NewValue);
    }
    /// <summary>
    /// Occurs when the Source property is changed. 
    /// </summary>
    /// <param name="value">The new source.</param>
    protected virtual void SourceChanged(string? value) { }

    /// <summary>
    /// Defines the Position property.
    /// </summary>
    public static readonly DependencyProperty PositionProperty = DependencyProperty.Register("Position", typeof(TimeSpan),
        typeof(PlayerHostBase),
        new PropertyMetadata(TimeSpan.Zero, PositionChanged, CoercePosition));
    /// <summary>
    /// Gets or sets the playback position.
    /// </summary>
    public TimeSpan Position { get => (TimeSpan)GetValue(PositionProperty); set => SetValue(PositionProperty, value); }
    private static void PositionChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var p = (PlayerHostBase)d;
        p.PositionChanged((TimeSpan)e.NewValue, !p._isSettingPosition);
    }
    private static object CoercePosition(DependencyObject d, object baseValue)
    {
        var p = (PlayerHostBase)d;
        return p.CoercePosition((TimeSpan)baseValue);
    }
    /// <summary>
    /// Occurs when the Position property is changed.
    /// </summary>
    /// <param name="value">The new position.</param>
    /// <param name="isSeeking">True if position is set by seeking, False if set by playback.</param>
    protected virtual void PositionChanged(TimeSpan value, bool isSeeking) { }
    /// <summary>
    /// Validates the position value to ensure it is within range.
    /// </summary>
    /// <param name="value">The new validation value.</param>
    /// <returns>The validated position­.</returns>
    protected virtual TimeSpan CoercePosition(TimeSpan value)
    {
        if (value < TimeSpan.Zero)
        {
            return TimeSpan.Zero;
        }
        else if (value > Duration)
        {
            return Duration;
        }
        else
        {
            return value;
        }
    }

    /// <summary>
    /// Defines the Duration property.
    /// </summary>
    public static readonly DependencyPropertyKey DurationPropertyKey = DependencyProperty.RegisterReadOnly("Duration", typeof(TimeSpan),
        typeof(PlayerHostBase),
        new PropertyMetadata(TimeSpan.FromSeconds(1), DurationChanged));
    private static readonly DependencyProperty s_durationProperty = DurationPropertyKey.DependencyProperty;
    /// <summary>
    /// Gets the duration of the media file.
    /// </summary>
    public TimeSpan Duration { get => (TimeSpan)GetValue(s_durationProperty); protected set => SetValue(DurationPropertyKey, value); }
    private static void DurationChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var p = (PlayerHostBase)d;
        p.DurationChanged((TimeSpan)e.NewValue);
    }
    /// <summary>
    /// Occurs when the Duration property is changed.
    /// </summary>
    /// <param name="value">The new duration.</param>
    protected virtual void DurationChanged(TimeSpan value) { }

    /// <summary>
    /// Defines the IsPlaying property.
    /// </summary>
    public static readonly DependencyProperty IsPlayingProperty = DependencyProperty.Register("IsPlaying", typeof(bool),
        typeof(PlayerHostBase),
        new PropertyMetadata(false, IsPlayingChanged, CoerceIsPlaying));
    /// <summary>
    /// Gets or sets whether the media file is playing or paused.
    /// </summary>
    public bool IsPlaying { get => (bool)GetValue(IsPlayingProperty); set => SetValue(IsPlayingProperty, value); }
    private static void IsPlayingChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var p = (PlayerHostBase)d;
        p.IsPlayingChanged((bool)e.NewValue);
    }
    private static object CoerceIsPlaying(DependencyObject d, object baseValue)
    {
        var p = (PlayerHostBase)d;
        return p.CoerceIsPlaying((bool)baseValue);
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
    public static readonly DependencyProperty VolumeProperty = DependencyProperty.Register("Volume", typeof(int), typeof(PlayerHostBase),
        new PropertyMetadata(50, VolumeChanged, CoerceVolume));
    /// <summary>
    /// Gets or sets the volume.
    /// </summary>
    public int Volume { get => (int)GetValue(VolumeProperty); set => SetValue(VolumeProperty, value); }
    private static void VolumeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var p = (PlayerHostBase)d;
        p.VolumeChanged((int)e.NewValue);
    }
    private static object CoerceVolume(DependencyObject d, object baseValue)
    {
        var p = (PlayerHostBase)d;
        return p.CoerceVolume((int)baseValue);
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
    public static readonly DependencyProperty SpeedFloatProperty = DependencyProperty.Register("SpeedFloat", typeof(double),
        typeof(PlayerHostBase),
        new PropertyMetadata(1.0, SpeedChanged, CoerceDouble));
    /// <summary>
    /// Gets or sets the speed multiplier.
    /// </summary>
    public double SpeedFloat { get => (double)GetValue(SpeedFloatProperty); set => SetValue(SpeedFloatProperty, value); }
    private static void SpeedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var p = (PlayerHostBase)d;
        p.SpeedChanged(p.GetSpeed());
    }
    /// <summary>
    /// Occurs when the Speed property is changed.
    /// </summary>
    /// <param name="value">The new speed value.</param>
    protected virtual void SpeedChanged(double value) { }

    /// <summary>
    /// Defines the SpeedInt property.
    /// </summary>
    public static readonly DependencyProperty SpeedIntProperty = DependencyProperty.Register("SpeedInt", typeof(int),
        typeof(PlayerHostBase),
        new PropertyMetadata(0, SpeedChanged, CoerceSpeedInt));
    private static object CoerceSpeedInt(DependencyObject d, object baseValue)
    {
        var p = (PlayerHostBase)d;
        return p.CoerceSpeedInt((int)baseValue);
    }
    /// <summary>
    /// Gets or sets the speed as an integer, where normal playback is 0. Useful for binding to a slider.
    /// </summary>
    public int SpeedInt { get => (int)GetValue(SpeedIntProperty); set => SetValue(SpeedIntProperty, value); }
    /// <summary>
    /// Occurs when the SpeedInt property is changed.
    /// </summary>
    /// <param name="value">The new speed value.</param>
    /// <returns>The speed value after validation.</returns>
    protected virtual int CoerceSpeedInt(int value) => value;

    /// <summary>
    /// Defines the Loop property.
    /// </summary>
    public static readonly DependencyProperty LoopProperty = DependencyProperty.Register("Loop", typeof(bool), typeof(PlayerHostBase),
        new PropertyMetadata(false, LoopChanged, CoerceLoop));
    /// <summary>
    /// Gets or sets whether to loop.
    /// </summary>
    public bool Loop { get => (bool)GetValue(LoopProperty); set => SetValue(LoopProperty, value); }
    private static void LoopChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var p = (PlayerHostBase)d;
        p.LoopChanged((bool)e.NewValue);
    }
    private static object CoerceLoop(DependencyObject d, object baseValue)
    {
        var p = (PlayerHostBase)d;
        return p.CoerceLoop((bool)baseValue);
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
    public static readonly DependencyProperty AutoPlayProperty = DependencyProperty.Register("AutoPlay", typeof(bool),
        typeof(PlayerHostBase), new PropertyMetadata(true, AutoPlayChanged, CoerceAutoPlay));
    /// <summary>
    /// Gets or sets whether to auto-play the file when setting the source.
    /// </summary>
    public bool AutoPlay { get => (bool)GetValue(AutoPlayProperty); set => SetValue(AutoPlayProperty, value); }
    private static void AutoPlayChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var p = (PlayerHostBase)d;
        p.AutoPlayChanged((bool)e.NewValue);
    }
    private static object CoerceAutoPlay(DependencyObject d, object baseValue)
    {
        var p = (PlayerHostBase)d;
        return p.CoerceAutoPlay((bool)baseValue);
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
    public static readonly DependencyProperty TitleProperty = DependencyProperty.Register("Title", typeof(string), typeof(PlayerHostBase),
        new PropertyMetadata(null, TitleChanged));
    /// <summary>
    /// Gets or sets the display title of the media file.
    /// A title is set by default and you can override it using this property.
    /// </summary>
    public string? Title { get => (string)GetValue(TitleProperty); set => SetValue(TitleProperty, value); }
    private static void TitleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var p = (PlayerHostBase)d;
        p.SetDisplayText();
    }

    /// <summary>
    /// Defines the Text property.
    /// </summary>
    public static readonly DependencyPropertyKey TextProperty =
        DependencyProperty.RegisterReadOnly("Text", typeof(string), typeof(PlayerHostBase), null);
    /// <summary>
    /// Gets the text being displayed, which may include the title and/or loading status.
    /// </summary>
    public string? Text { get => (string)GetValue(TextProperty.DependencyProperty); protected set => SetValue(TextProperty, value); }

    /// <summary>
    /// Defines the IsMediaLoaded property.
    /// </summary>
    public static readonly DependencyPropertyKey IsMediaLoadedPropertyKey = DependencyProperty.RegisterReadOnly("IsMediaLoaded",
        typeof(bool), typeof(PlayerHostBase),
        new PropertyMetadata(false));
    private static readonly DependencyProperty s_isMediaLoadedProperty = IsMediaLoadedPropertyKey.DependencyProperty;
    /// <summary>
    /// Gets whether a media file is loaded.
    /// </summary>
    public bool IsMediaLoaded { get => (bool)GetValue(s_isMediaLoadedProperty); private set => SetValue(IsMediaLoadedPropertyKey, value); }

    /// <summary>
    /// Defines the IsVideoVisible property.
    /// </summary>
    public static readonly DependencyProperty IsVideoVisibleProperty = DependencyProperty.Register("IsVideoVisible", typeof(bool),
        typeof(PlayerHostBase),
        new PropertyMetadata(false, IsVideoVisibleChanged, CoerceIsVideoVisible));
    /// <summary>
    /// Gets or sets whether to show the video.
    /// </summary>
    public bool IsVideoVisible { get => (bool)GetValue(IsVideoVisibleProperty); set => SetValue(IsVideoVisibleProperty, value); }
    private static void IsVideoVisibleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var p = (PlayerHostBase)d;
        p.IsVideoVisibleChanged((bool)e.NewValue);
    }
    private static object CoerceIsVideoVisible(DependencyObject d, object baseValue)
    {
        var p = (PlayerHostBase)d;
        return p.CoerceIsVideoVisible((bool)baseValue);
    }
    /// <summary>
    /// Occurs when the IsVideoVisible property is changed.
    /// </summary>
    /// <param name="value">The new visibility value.</param>
    protected virtual void IsVideoVisibleChanged(bool value)
    {
        if (HostContainer != null)
        {
            HostContainer.Visibility = value ? Visibility.Visible : Visibility.Hidden;
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
    /// <returns>The value after validation.</returns>
    protected static object CoerceDouble(DependencyObject d, object baseValue)
    {
        var value = (double)baseValue;
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
    public virtual FrameworkElement? HostContainer { get; } = null;


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
    public Panel GetInnerControlParent() =>
        _innerControlParentCache ??= HostContainer?.Parent as Panel ??
                                     throw new InvalidCastException(string.Format(CultureInfo.InvariantCulture,
                                         Properties.Resources.ParentMustBePanel, "PlayerHostBase", HostContainer?.Parent?.GetType()));
}
