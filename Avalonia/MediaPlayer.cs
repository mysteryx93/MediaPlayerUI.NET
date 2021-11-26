using System;
using System.ComponentModel;
using System.Globalization;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.VisualTree;
using HanumanInstitute.MediaPlayer.Avalonia.Helpers.Mvvm;
using HanumanInstitute.MediaPlayer.Avalonia.Helpers;

namespace HanumanInstitute.MediaPlayer.Avalonia
{
    /// <summary>
    /// A media player graphical interface that can be used with any video host.
    /// </summary>
    public class MediaPlayer : MediaPlayerBase, INotifyPropertyChanged, IStyleable
    {
        static MediaPlayer()
        {
            BackgroundProperty.OverrideMetadata(typeof(MediaPlayer),
                new StyledPropertyMetadata<IBrush?>(new Optional<IBrush?>(Brushes.Black)));
            HorizontalAlignmentProperty.OverrideMetadata(typeof(MediaPlayer),
                new StyledPropertyMetadata<HorizontalAlignment>(HorizontalAlignment.Stretch));
            VerticalAlignmentProperty.OverrideMetadata(typeof(MediaPlayer),
                new StyledPropertyMetadata<VerticalAlignment>(VerticalAlignment.Stretch));
            ContentProperty.Changed.Subscribe(ContentChanged);
        }

        Type IStyleable.StyleKey => typeof(MediaPlayer);

        private static void ContentChanged(AvaloniaPropertyChangedEventArgs e)
        {
            if (e.Sender is MediaPlayerBase p)
            {
                p.ContentHasChanged(e);
            }
        }

        // private static object? CoerceContent(IAvaloniaObject d, object baseValue) => baseValue as PlayerHostBase;

        public MediaPlayer()
        {
        }

        public const string UIPartName = "PART_UI";
        public Border? UIPart { get; private set; }

        public const string SeekBarPartName = "PART_SeekBar";
        public Slider? SeekBarPart { get; private set; }

        public const string SeekBarTrackPartName = "PART_Track";
        public Track? SeekBarTrackPart { get; private set; }

        public Thumb? SeekBarThumbPart => SeekBarTrackPart?.Thumb;

        protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
        {
            base.OnApplyTemplate(e);
            if (Design.IsDesignMode) { return; }

            UIPart = e.NameScope.Find<Border>(UIPartName);
            SeekBarPart = e.NameScope.Find<Slider>(SeekBarPartName);

            if (UIPart == null)
            {
                throw new InvalidCastException(string.Format(CultureInfo.InvariantCulture,
                    Properties.Resources.TemplateElementNotFound, UIPartName, nameof(Border)));
            }
            
            if (SeekBarPart == null)
            {
                throw new InvalidCastException(string.Format(CultureInfo.InvariantCulture,
                    Properties.Resources.TemplateElementNotFound, SeekBarPartName, nameof(Slider)));
            }
            
            PointerPressed += UserControl_PointerPressed;
            SeekBarPart.AddHandler(Slider.PointerPressedEvent,
                OnSeekBarPreviewMouseLeftButtonDown);
            
            // Thumb doesn't yet exist.
            SeekBarPart.TemplateApplied += (_, t) =>
            {
                SeekBarTrackPart = t.NameScope.Find<Track>(SeekBarTrackPartName);                
                if (SeekBarThumbPart == null)
                {
                    throw new InvalidCastException(string.Format(CultureInfo.InvariantCulture,
                        Properties.Resources.TemplateElementNotFound, SeekBarTrackPartName, nameof(Track)));
                }
            
                SeekBarThumbPart.DragStarted += OnSeekBarDragStarted;
                SeekBarThumbPart.DragCompleted += OnSeekBarDragCompleted;
            };
        }

        protected override void OnPointerPressed(PointerPressedEventArgs e)
        {
            base.OnPointerPressed(e);
            if (e.MouseButton == MouseButton.Left)
            {
                Focus();
            }
        }

        /// <summary>
        /// Prevents the Host from receiving mouse events when clicking on controls bar.
        /// </summary>
        private void UserControl_PointerPressed(object? sender, PointerPressedEventArgs e)
        {
            e.Handled = true;
        }

        protected override void OnContentChanged(AvaloniaPropertyChangedEventArgs e)
        {
            RaisePropertyChanged<PlayerHostBase?>(PlayerHostProperty, e.OldValue as PlayerHostBase,
                e.NewValue as PlayerHostBase, BindingPriority.TemplatedParent);
            PlayerHost = e.NewValue as PlayerHostBase;
            if (e.OldValue != null)
            {
                ((Control)e.OldValue).PointerPressed -= Host_MouseDown;
                ((Control)e.OldValue).PointerWheelChanged -= Host_MouseWheel;
                ((Control)e.OldValue).DoubleTapped -= Host_MouseDoubleClick;
            }

            if (e.NewValue != null)
            {
                ((Control)e.NewValue).PointerPressed += Host_MouseDown;
                ((Control)e.NewValue).PointerWheelChanged += Host_MouseWheel;
                ((Control)e.NewValue).DoubleTapped += Host_MouseDoubleClick;
            }
        }

        private void Host_MouseWheel(object? sender, PointerWheelEventArgs e)
        {
            if (PlayerHost == null) { return; }

            if (ChangeVolumeOnMouseWheel)
            {
                if (e.Delta.Y > 0)
                {
                    PlayerHost.Volume += 5;
                }
                else if (e.Delta.Y < 0)
                {
                    PlayerHost.Volume -= 5;
                }
            }
        }

        private void Host_MouseDoubleClick(object? sender, RoutedEventArgs e)
        {
            // ** Double clicks aren't working properly yet.
            // HandleMouseAction(sender, e, 2);
        }

        private void Host_MouseDown(object? sender, PointerPressedEventArgs e)
        {
            HandleMouseAction(sender, e, 1);
        }

        /// <summary>
        /// Handles mouse click events for both Host and Fullscreen.
        /// </summary>
        private void HandleMouseAction(object? sender, PointerPressedEventArgs e, int clickCount)
        {
            var isFullScreen = false; // sender is FullScreenUI;
            if (IsActionFullScreen(e.MouseButton, clickCount))
            {
                FullScreen = !isFullScreen; // using !FullScreen can return wrong value when exiting fullscreen
                e.Handled = true;
            }
            else if (IsActionPause(e.MouseButton, clickCount))
            {
                if (PlayPauseCommand.CanExecute(null))
                {
                    PlayPauseCommand.Execute(null);
                }

                e.Handled = true;
            }
        }

        private bool IsActionFullScreen(MouseButton button, int clickCount) =>
            IsMouseAction(MouseFullscreen, button, clickCount);

        private bool IsActionPause(MouseButton button, int clickCount) => IsMouseAction(MousePause, button, clickCount);

        private static bool IsMouseAction(MouseTrigger a, MouseButton button, int clickCount)
        {
            if (clickCount != TriggerClickCount(a))
            {
                return false;
            }

            if (a == MouseTrigger.LeftClick && button == MouseButton.Left)
            {
                return true;
            }

            if (a == MouseTrigger.MiddleClick && button == MouseButton.Middle)
            {
                return true;
            }

            if (a == MouseTrigger.RightClick && button == MouseButton.Right)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Returns the amount of clicks represented by specified mouse trigger.
        /// </summary>
        private static int TriggerClickCount(MouseTrigger a)
        {
            if (a == MouseTrigger.None)
            {
                return 0;
            }
            else if (a == MouseTrigger.LeftClick || a == MouseTrigger.MiddleClick || a == MouseTrigger.RightClick)
            {
                return 1;
            }
            else
            {
                return 2;
            }
        }

        private Panel? _uiParentCache;

        /// <summary>
        /// Returns the container of this control the first time it is called and maintain reference to that container.
        /// </summary>
        private Panel UIParentCache
        {
            get
            {
                if (_uiParentCache == null)
                {
                    _uiParentCache = UIPart?.Parent as Panel;
                }

                if (_uiParentCache == null)
                {
                    throw new NullReferenceException(string.Format(CultureInfo.InvariantCulture,
                        Properties.Resources.ParentMustBePanel, UIPartName, UIPart?.Parent?.GetType()));
                }

                return _uiParentCache;
            }
        }

        // TitleProperty
        public static readonly StyledProperty<string> TitleProperty =
            AvaloniaProperty.Register<MediaPlayer, string>(nameof(Title));

        public string Title
        {
            get => GetValue(TitleProperty);
            set => SetValue(TitleProperty, value);
        }

        // MouseFullscreen
        public static readonly StyledProperty<MouseTrigger> MouseFullscreenProperty =
            AvaloniaProperty.Register<MediaPlayer, MouseTrigger>(nameof(MouseFullscreen), MouseTrigger.MiddleClick);

        public MouseTrigger MouseFullscreen
        {
            get => GetValue(MouseFullscreenProperty);
            set => SetValue(MouseFullscreenProperty, value);
        }

        // MousePause
        public static readonly StyledProperty<MouseTrigger> MousePauseProperty =
            AvaloniaProperty.Register<MediaPlayer, MouseTrigger>(nameof(MousePause), MouseTrigger.LeftClick);

        public MouseTrigger MousePause
        {
            get => GetValue(MousePauseProperty);
            set => SetValue(MousePauseProperty, value);
        }

        // ChangeVolumeOnMouseWheel
        public static readonly StyledProperty<bool> ChangeVolumeOnMouseWheelProperty =
            AvaloniaProperty.Register<MediaPlayer, bool>(nameof(ChangeVolumeOnMouseWheel), true);

        public bool ChangeVolumeOnMouseWheel
        {
            get => GetValue(ChangeVolumeOnMouseWheelProperty);
            set => SetValue(ChangeVolumeOnMouseWheelProperty, value);
        }

        // IsPlayPauseVisible
        public static readonly StyledProperty<bool> IsPlayPauseVisibleProperty =
            AvaloniaProperty.Register<MediaPlayer, bool>(nameof(IsPlayPauseVisible), true);
        // FrameworkPropertyMetadataOptions.AffectsParentArrange

        public bool IsPlayPauseVisible
        {
            get => GetValue(IsPlayPauseVisibleProperty);
            set => SetValue(IsPlayPauseVisibleProperty, value);
        }

        // IsStopVisible
        public static readonly StyledProperty<bool> IsStopVisibleProperty =
            AvaloniaProperty.Register<MediaPlayer, bool>(nameof(IsStopVisible), true);
        // FrameworkPropertyMetadataOptions.AffectsParentArrange

        public bool IsStopVisible
        {
            get => GetValue(IsStopVisibleProperty);
            set => SetValue(IsStopVisibleProperty, value);
        }

        // IsLoopVisible
        public static readonly StyledProperty<bool> IsLoopVisibleProperty =
            AvaloniaProperty.Register<MediaPlayer, bool>(nameof(IsLoopVisible), true);
        // FrameworkPropertyMetadataOptions.AffectsParentArrange

        public bool IsLoopVisible
        {
            get => GetValue(IsLoopVisibleProperty);
            set => SetValue(IsLoopVisibleProperty, value);
        }

        // IsVolumeVisible
        public static readonly StyledProperty<bool> IsVolumeVisibleProperty =
            AvaloniaProperty.Register<MediaPlayer, bool>(nameof(IsVolumeVisible), true);
        // FrameworkPropertyMetadataOptions.AffectsParentArrange

        public bool IsVolumeVisible
        {
            get => GetValue(IsVolumeVisibleProperty);
            set => SetValue(IsVolumeVisibleProperty, value);
        }

        // IsSpeedVisible
        public static readonly StyledProperty<bool> IsSpeedVisibleProperty =
            AvaloniaProperty.Register<MediaPlayer, bool>(nameof(IsSpeedVisible), true);
        // FrameworkPropertyMetadataOptions.AffectsParentArrange

        public bool IsSpeedVisible
        {
            get => GetValue(IsSpeedVisibleProperty);
            set => SetValue(IsSpeedVisibleProperty, value);
        }

        // IsSeekBarVisible
        public static readonly StyledProperty<bool> IsSeekBarVisibleProperty =
            AvaloniaProperty.Register<MediaPlayer, bool>(nameof(IsSeekBarVisible), true);
        // FrameworkPropertyMetadataOptions.AffectsParentArrange

        public bool IsSeekBarVisible
        {
            get => GetValue(IsSeekBarVisibleProperty);
            set => SetValue(IsSeekBarVisibleProperty, value);
        }


        public void OnSeekBarPreviewMouseLeftButtonDown(object? sender, PointerPressedEventArgs e)
        {
            if (PlayerHost == null) { return; }

            e.CheckNotNull(nameof(e));

            // Only process event if click is not on thumb.
            if (SeekBarThumbPart != null)
            {
                var pos = e.GetPosition(SeekBarThumbPart);
                if (pos.X < 0 || pos.Y < 0 || pos.X > SeekBarThumbPart.Width ||
                    pos.Y > SeekBarThumbPart.Height)
                {
                    // Immediate seek when clicking elsewhere.
                    IsSeekBarPressed = true;
                    PlayerHost.Position = PositionBar;
                    IsSeekBarPressed = false;
                }
            }
        }

        public void OnSeekBarDragStarted(object? sender, VectorEventArgs e)
        {
            if (PlayerHost == null) { return; }

            IsSeekBarPressed = true;
        }

        private DateTime _lastDragCompleted = DateTime.MinValue;

        public void OnSeekBarDragCompleted(object? sender, VectorEventArgs e)
        {
            if (PlayerHost == null) { return; }

            // DragCompleted can trigger multiple times after switching to/from fullscreen. Ignore multiple events within a second.
            if ((DateTime.Now - _lastDragCompleted).TotalSeconds < 1)
            {
                return;
            }

            _lastDragCompleted = DateTime.Now;

            PlayerHost.Position = PositionBar;
            IsSeekBarPressed = false;
        }


        // public FullScreenUI? FullScreenUI
        // {
        //     get;
        //     private set;
        // }

        public ICommand ToggleFullScreenCommand =>
            _toggleFullScreenCommand ??= new RelayCommand(ToggleFullScreen, CanToggleFullScreen);

        private RelayCommand? _toggleFullScreenCommand;
        private bool CanToggleFullScreen() => PlayerHost != null;

        private void ToggleFullScreen()
        {
            FullScreen = !FullScreen;
        }

        public bool FullScreen
        {
            get => false; // FullScreenUI != null;
            set
            {
                // if (PlayerHost == null) { return; }
                //
                // if (UiPart == null) { return; }
                //
                // if (PlayerHost.HostContainer == null) { return; }
                //
                // if (value != FullScreen)
                // {
                //     if (value)
                //     {
                //         // Create full screen.
                //         FullScreenUi = new FullScreenUI();
                //         FullScreenUi.Closed += FullScreenUI_Closed;
                //         FullScreenUi.MouseDown += Host_MouseDown;
                //         // Transfer key bindings.
                //         InputBindingBehavior.TransferBindingsToWindow(Window.GetWindow(this), FullScreenUi, false);
                //         // Transfer player.
                //         TransferElement(PlayerHost.GetInnerControlParent(), FullScreenUi.ContentGrid,
                //             PlayerHost.HostContainer);
                //         TransferElement(UIParentCache, FullScreenUi.AirspaceGrid, UiPart);
                //         FullScreenUi.Airspace.VerticalOffset = -UiPart.ActualHeight;
                //         // Show.
                //         FullScreenUi.ShowDialog();
                //     }
                //     else if (FullScreenUi != null)
                //     {
                //         // Transfer player back.
                //         TransferElement(FullScreenUi.ContentGrid, PlayerHost.GetInnerControlParent(),
                //             PlayerHost.HostContainer);
                //         TransferElement(FullScreenUi.AirspaceGrid, UIParentCache, UiPart);
                //         // Close.
                //         var f = FullScreenUi;
                //         FullScreenUi = null;
                //         f.CloseOnce();
                //         // Activate.
                //         Window.GetWindow(this).Activate();
                //         Focus();
                //     }
                // }
            }
        }

        private static void TransferElement(IPanel src, IPanel dst, IControl element)
        {
            src.Children.Remove(element);
            dst.Children.Add(element);
        }

        private void FullScreenUI_Closed(object? sender, EventArgs _)
        {
            FullScreen = false;
        }
    }
}
