//using System;
//using Avalonia;
//using Avalonia.Controls;
//using Avalonia.Controls.Primitives;
//using Avalonia.Input;
//using HanumanInstitute.MediaPlayer.Avalonia.Helpers;

//namespace HanumanInstitute.MediaPlayer.Avalonia;

///// <summary>
///// Causes the slider to seek and release when clicking instead of dragging the thumb. This does not apply when dragging the thumb directly.
///// </summary>
//public class SliderExtensions : AvaloniaObject
//{
//    static SliderExtensions()
//    {
//        ReleaseOnClickProperty.Changed.Subscribe(ReleaseOnClickChanged);
//    }

//    /// <summary>
//    /// Gets or sets whether to seek and release when clicking the slider instead of dragging the thumb.
//    /// </summary>
//    public static readonly AttachedProperty<bool> ReleaseOnClickProperty =
//        AvaloniaProperty.RegisterAttached<SliderExtensions, Slider, bool>("ReleaseOnClick");
//    /// <summary>
//    /// Gets whether to seek and release when clicking the slider instead of dragging the thumb.
//    /// </summary>
//    public static bool GetReleaseOnClick(AvaloniaObject d) => d.CheckNotNull(nameof(d)).GetValue(ReleaseOnClickProperty);
//    /// <summary>
//    /// Sets whether to seek and release when clicking the slider instead of dragging the thumb.
//    /// </summary>
//    public static void SetReleaseOnClick(AvaloniaObject d, bool value) => d.CheckNotNull(nameof(d)).SetValue(ReleaseOnClickProperty, value);
//    private static void ReleaseOnClickChanged(AvaloniaPropertyChangedEventArgs<bool> e)
//    {
//        if (e.Sender is Slider elem)
//        {
//            elem.TemplateApplied += AttachRepeatButton;
//        }
//    }

//    private static void AttachRepeatButton(object? sender, TemplateAppliedEventArgs e)
//    {
//        if (sender is Slider elem)
//        {
//            elem.TemplateApplied -= AttachRepeatButton;
//            AttachRepeatButton(e.NameScope, "PART_DecreaseButton");
//            AttachRepeatButton(e.NameScope, "PART_IncreaseButton");
//        }
//    }

//    private static void AttachRepeatButton(INameScope nameScope, string name)
//    {
//        var button = nameScope.FindOrThrow<RepeatButton>(name) ??
//                     throw new NullReferenceException($"Cannot find Slider RepeatButton with name '{name}'.");
//        // button.AddHandler(InputElement.PointerPressedEvent, RepeatButton_PointerPressed, RoutingStrategies.Tunnel);
//    }

//    private static void RepeatButton_PointerPressed(object? sender, PointerPressedEventArgs e) =>
//        e.Pointer.Capture(null);
//}
