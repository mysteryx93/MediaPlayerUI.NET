using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace HanumanInstitute.MediaPlayer.Wpf;

/// <summary>
/// Represents the base class for Host controls. Each player implementation inherits from this class to manage commmon properties of a player.
/// </summary>
public abstract class PlayerHostBase : Control
{
    static PlayerHostBase()
    {
        FocusableProperty.OverrideMetadata(typeof(PlayerHostBase), new FrameworkPropertyMetadata(false));
    }

    public PlayerHostBase()
    { }

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

    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        _stopTimer = new DispatcherTimer(TimeSpan.FromSeconds(1), DispatcherPriority.Background, (s, e) =>
        {
            _stopTimer?.Stop();
            _isStopping = false;
        }, Dispatcher);
    }


    // Position
    public static readonly DependencyProperty PositionProperty = DependencyProperty.Register("Position", typeof(TimeSpan), typeof(PlayerHostBase),
        new PropertyMetadata(TimeSpan.Zero, PositionChanged, CoercePosition));
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
    protected virtual void PositionChanged(TimeSpan value, bool isSeeking) { }
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

    // Duration
    public static readonly DependencyPropertyKey DurationPropertyKey = DependencyProperty.RegisterReadOnly("Duration", typeof(TimeSpan), typeof(PlayerHostBase),
        new PropertyMetadata(TimeSpan.FromSeconds(1), DurationChanged));
    private static readonly DependencyProperty s_durationProperty = DurationPropertyKey.DependencyProperty;
    public TimeSpan Duration { get => (TimeSpan)GetValue(s_durationProperty); protected set => SetValue(DurationPropertyKey, value); }
    private static void DurationChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var p = (PlayerHostBase)d;
        p.DurationChanged((TimeSpan)e.NewValue);
    }
    protected virtual void DurationChanged(TimeSpan value) { }

    // IsPlaying
    public static readonly DependencyProperty IsPlayingProperty = DependencyProperty.Register("IsPlaying", typeof(bool), typeof(PlayerHostBase),
        new PropertyMetadata(false, IsPlayingChanged, CoerceIsPlaying));
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
    protected virtual void IsPlayingChanged(bool value) { }
    protected virtual bool CoerceIsPlaying(bool value) => value;

    // Volume
    public static readonly DependencyProperty VolumeProperty = DependencyProperty.Register("Volume", typeof(int), typeof(PlayerHostBase),
        new PropertyMetadata(50, VolumeChanged, CoerceVolume));
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
    protected virtual void VolumeChanged(int value) { }
    protected virtual int CoerceVolume(int value)
    {
        return Math.Max(0, (Math.Min(100, value)));
    }

    // SpeedFloat
    public static readonly DependencyProperty SpeedFloatProperty = DependencyProperty.Register("SpeedFloat", typeof(double), typeof(PlayerHostBase),
        new PropertyMetadata(1.0, SpeedChanged, CoerceDouble));
    public double SpeedFloat { get => (double)GetValue(SpeedFloatProperty); set => SetValue(SpeedFloatProperty, value); }
    private static void SpeedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var p = (PlayerHostBase)d;
        p.SpeedChanged(p.GetSpeed());
    }
    protected virtual void SpeedChanged(double value) { }

    // SpeedInt
    public static readonly DependencyProperty SpeedIntProperty = DependencyProperty.Register("SpeedInt", typeof(int), typeof(PlayerHostBase),
        new PropertyMetadata(0, SpeedChanged, CoerceSpeedInt));
    private static object CoerceSpeedInt(DependencyObject d, object baseValue)
    {
        var p = (PlayerHostBase)d;
        return p.CoerceSpeedInt((int)baseValue);
    }
    public int SpeedInt { get => (int)GetValue(SpeedIntProperty); set => SetValue(SpeedIntProperty, value); }
    protected virtual int CoerceSpeedInt(int value) => value;

    // Loop
    public static readonly DependencyProperty LoopProperty = DependencyProperty.Register("Loop", typeof(bool), typeof(PlayerHostBase),
        new PropertyMetadata(false, LoopChanged, CoerceLoop));
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
    protected virtual void LoopChanged(bool value) { }
    protected virtual bool CoerceLoop(bool value) => value;

    // AutoPlay
    public static readonly DependencyProperty AutoPlayProperty = DependencyProperty.Register("AutoPlay", typeof(bool), typeof(PlayerHostBase),
        new PropertyMetadata(true, AutoPlayChanged, CoerceAutoPlay));
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
    protected virtual void AutoPlayChanged(bool value)
    {
        // AutoPlay can be set AFTER Script, we need to reset IsPlaying in that case.
        // Default value needs to be false otherwise it can cause video to start loading and immediately stop which can cause issues.
        IsPlaying = value;
    }
    protected virtual bool CoerceAutoPlay(bool value) => value;

    // Text
    public static readonly DependencyPropertyKey TextProperty = DependencyProperty.RegisterReadOnly("Text", typeof(string), typeof(PlayerHostBase), null);
    public string Text { get => (string)GetValue(TextProperty.DependencyProperty); protected set => SetValue(TextProperty, value); }

    // IsMediaLoaded
    public static readonly DependencyPropertyKey IsMediaLoadedPropertyKey = DependencyProperty.RegisterReadOnly("IsMediaLoaded", typeof(bool), typeof(PlayerHostBase),
        new PropertyMetadata(false));
    private static readonly DependencyProperty s_isMediaLoadedProperty = IsMediaLoadedPropertyKey.DependencyProperty;
    public bool IsMediaLoaded { get => (bool)GetValue(s_isMediaLoadedProperty); private set => SetValue(IsMediaLoadedPropertyKey, value); }

    // IsVideoVisible
    public static readonly DependencyProperty IsVideoVisibleProperty = DependencyProperty.Register("IsVideoVisible", typeof(bool), typeof(PlayerHostBase),
        new PropertyMetadata(false, IsVideoVisibleChanged, CoerceIsVideoVisible));
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
    protected virtual void IsVideoVisibleChanged(bool value)
    {
        if (HostContainer != null)
        {
            HostContainer.Visibility = value ? Visibility.Visible : Visibility.Hidden;
        }
    }
    protected virtual bool CoerceIsVideoVisible(bool value) => value;

    protected static object CoerceDouble(DependencyObject d, object baseValue)
    {
        var value = (double)baseValue;
        return !(value > 0) ? 1 : value;
    }

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
    public virtual FrameworkElement? HostContainer { get; }



    /// <summary>
    /// Stops playback and unloads media file.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1716:Identifiers should not match keywords", Justification = "Reviewed: It's the right name.")]
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
        MediaLoaded?.Invoke(this, new EventArgs());
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
            MediaUnloaded?.Invoke(this, new EventArgs());
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
        if (_innerControlParentCache == null)
        {
            _innerControlParentCache = HostContainer?.Parent as Panel;
        }
        if (_innerControlParentCache == null)
        {
            throw new InvalidCastException(string.Format(CultureInfo.InvariantCulture, Properties.Resources.ParentMustBePanel, "PlayerHostBase", HostContainer?.Parent?.GetType()));
        }
        return _innerControlParentCache;
    }
}