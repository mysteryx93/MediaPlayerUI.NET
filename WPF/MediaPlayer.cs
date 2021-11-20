using System;
using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using HanumanInstitute.MediaPlayer.WPF.Mvvm;

namespace HanumanInstitute.MediaPlayer.WPF
{
    /// <summary>
    /// A media player graphical interface that can be used with any video host.
    /// </summary>
    [TemplatePart(Name = MediaPlayer.UIPartName, Type = typeof(Border))]
    [TemplatePart(Name = MediaPlayer.SeekBarPartName, Type = typeof(Slider))]
    //[TemplatePart(Name = MediaPlayer.SeekBarTrackPartName, Type = typeof(Track))]
    public class MediaPlayer : MediaPlayerBase, INotifyPropertyChanged
    {
        static MediaPlayer()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(MediaPlayer), new FrameworkPropertyMetadata(typeof(MediaPlayer)));
            BackgroundProperty.OverrideMetadata(typeof(MediaPlayer), new FrameworkPropertyMetadata(Brushes.Black));
            HorizontalAlignmentProperty.OverrideMetadata(typeof(MediaPlayer), new FrameworkPropertyMetadata(HorizontalAlignment.Stretch));
            VerticalAlignmentProperty.OverrideMetadata(typeof(MediaPlayer), new FrameworkPropertyMetadata(VerticalAlignment.Stretch));
            ContentProperty.OverrideMetadata(typeof(MediaPlayer), new FrameworkPropertyMetadata(ContentChanged, CoerceContent));
        }

        private static void ContentChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) => MediaPlayerBase.OnContentChanged(d, e);

        private static object? CoerceContent(DependencyObject d, object baseValue) => baseValue as PlayerHostBase;

        public MediaPlayer()
        { }

        public event PropertyChangedEventHandler? PropertyChanged;

        public const string UIPartName = "PART_UI";
        public Border? UIPart => _ui ??= GetTemplateChild(UIPartName) as Border;
        private Border? _ui;

        public const string SeekBarPartName = "PART_SeekBar";
        public Slider? SeekBarPart => _seekBar ??= GetTemplateChild(SeekBarPartName) as Slider;
        private Slider? _seekBar;

        public const string SeekBarTrackPartName = "PART_Track";
        public Track? SeekBarTrackPart => _seekBarTrack ??= SeekBarPart?.Template.FindName(SeekBarTrackPartName, SeekBarPart) as Track;
        private Track? _seekBarTrack;

        public Thumb? SeekBarThumbPart => _seekBarThumb ??= SeekBarTrackPart?.Thumb;
        private Thumb? _seekBarThumb;

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            if (DesignerProperties.GetIsInDesignMode(this)) { return; }

            if (UIPart == null)
            {
                throw new InvalidCastException(string.Format(CultureInfo.InvariantCulture, Properties.Resources.TemplateElementNotFound, UIPartName, typeof(Border).Name));
            }
            if (SeekBarPart == null)
            {
                throw new InvalidCastException(string.Format(CultureInfo.InvariantCulture, Properties.Resources.TemplateElementNotFound, SeekBarPartName, typeof(Slider).Name));
            }

            MouseDown += UserControl_MouseDown;
            SeekBarPart.AddHandler(Slider.PreviewMouseDownEvent, new MouseButtonEventHandler(OnSeekBarPreviewMouseLeftButtonDown), true);
            // Thumb doesn't yet exist.
            SeekBarPart.Loaded += (s, e) =>
            {
                if (SeekBarThumbPart == null)
                {
                    throw new InvalidCastException(string.Format(CultureInfo.InvariantCulture, Properties.Resources.TemplateElementNotFound, SeekBarTrackPartName, typeof(Track).Name));
                }

                SeekBarThumbPart.DragStarted += OnSeekBarDragStarted;
                SeekBarThumbPart.DragCompleted += OnSeekBarDragCompleted;
            };
        }

        protected override void OnPreviewMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            base.OnPreviewMouseLeftButtonDown(e);
            Focus();
        }

        /// <summary>
        /// Prevents the Host from receiving mouse events when clicking on controls bar.
        /// </summary>
        private void UserControl_MouseDown(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
        }

        protected override void OnContentChanged(DependencyPropertyChangedEventArgs e)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(PlayerHost)));
            // PlayerHost = e.NewValue as PlayerHostBase;
            if (e.OldValue != null)
            {
                ((Control)e.OldValue).MouseDown -= Host_MouseDown;
                ((Control)e.OldValue).MouseWheel -= Host_MouseWheel;
                ((Control)e.OldValue).MouseDoubleClick -= Host_MouseDoubleClick;
            }
            if (e.NewValue != null)
            {
                ((Control)e.NewValue).MouseDown += Host_MouseDown;
                ((Control)e.NewValue).MouseWheel += Host_MouseWheel;
                ((Control)e.NewValue).MouseDoubleClick += Host_MouseDoubleClick;
            }
        }

        private void Host_MouseWheel(object? sender, MouseWheelEventArgs e)
        {
            if (PlayerHost == null) { return; }

            if (ChangeVolumeOnMouseWheel)
            {
                if (e.Delta > 0)
                {
                    PlayerHost.Volume += 5;
                }
                else if (e.Delta < 0)
                {
                    PlayerHost.Volume -= 5;
                }
            }
        }

        private void Host_MouseDoubleClick(object? sender, MouseButtonEventArgs e)
        {
            // ** Double clicks aren't working properly yet.
            HandleMouseAction(sender, e, 2);
        }

        private void Host_MouseDown(object? sender, MouseButtonEventArgs e)
        {
            HandleMouseAction(sender, e, 1);
        }

        /// <summary>
        /// Handles mouse click events for both Host and Fullscreen.
        /// </summary>
        private void HandleMouseAction(object? sender, MouseButtonEventArgs e, int clickCount)
        {
            var isFullScreen = sender is FullScreenUI;
            if (IsActionFullScreen(e, clickCount))
            {
                FullScreen = !isFullScreen; // using !FullScreen can return wrong value when exiting fullscreen
                e.Handled = true;
            }
            else if (IsActionPause(e, clickCount))
            {
                if (PlayPauseCommand.CanExecute(null))
                {
                    PlayPauseCommand.Execute(null);
                }

                e.Handled = true;
            }
        }

        private bool IsActionFullScreen(MouseButtonEventArgs e, int clickCount) => IsMouseAction(MouseFullscreen, e, clickCount);

        private bool IsActionPause(MouseButtonEventArgs e, int clickCount) => IsMouseAction(MousePause, e, clickCount);

        private static bool IsMouseAction(MouseTrigger a, MouseButtonEventArgs e, int clickCount)
        {
            if (clickCount != TriggerClickCount(a))
            {
                return false;
            }

            if (a == MouseTrigger.LeftClick && e.ChangedButton == MouseButton.Left)
            {
                return true;
            }

            if (a == MouseTrigger.MiddleClick && e.ChangedButton == MouseButton.Middle)
            {
                return true;
            }

            if (a == MouseTrigger.RightClick && e.ChangedButton == MouseButton.Right)
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
                    throw new NullReferenceException(string.Format(CultureInfo.InvariantCulture, Properties.Resources.ParentMustBePanel, UIPartName, UIPart?.Parent?.GetType()));
                }
                return _uiParentCache;
            }
        }

        // TitleProperty
        public static readonly DependencyProperty TitleProperty = DependencyProperty.Register("Title", typeof(bool), typeof(MediaPlayer));
        public string Title { get => (string)GetValue(TitleProperty); set => SetValue(TitleProperty, value); }

        // MouseFullscreen
        public static readonly DependencyProperty MouseFullscreenProperty = DependencyProperty.Register("MouseFullscreen", typeof(MouseTrigger), typeof(MediaPlayer),
            new PropertyMetadata(MouseTrigger.MiddleClick));
        public MouseTrigger MouseFullscreen { get => (MouseTrigger)GetValue(MouseFullscreenProperty); set => SetValue(MouseFullscreenProperty, value); }

        // MousePause
        public static readonly DependencyProperty MousePauseProperty = DependencyProperty.Register("MousePause", typeof(MouseTrigger), typeof(MediaPlayer),
            new PropertyMetadata(MouseTrigger.LeftClick));
        public MouseTrigger MousePause { get => (MouseTrigger)GetValue(MousePauseProperty); set => SetValue(MousePauseProperty, value); }

        // ChangeVolumeOnMouseWheel
        public static readonly DependencyProperty ChangeVolumeOnMouseWheelProperty = DependencyProperty.Register("ChangeVolumeOnMouseWheel", typeof(bool), typeof(MediaPlayer),
            new PropertyMetadata(true));
        public bool ChangeVolumeOnMouseWheel { get => (bool)GetValue(ChangeVolumeOnMouseWheelProperty); set => SetValue(ChangeVolumeOnMouseWheelProperty, value); }

        // IsPlayPauseVisible
        public static readonly DependencyProperty IsPlayPauseVisibleProperty = DependencyProperty.Register("IsPlayPauseVisible", typeof(bool), typeof(MediaPlayer),
            new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.AffectsParentArrange));
        public bool IsPlayPauseVisible { get => (bool)GetValue(IsPlayPauseVisibleProperty); set => SetValue(IsPlayPauseVisibleProperty, value); }

        // IsStopVisible
        public static readonly DependencyProperty IsStopVisibleProperty = DependencyProperty.Register("IsStopVisible", typeof(bool), typeof(MediaPlayer),
            new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.AffectsParentArrange));
        public bool IsStopVisible { get => (bool)GetValue(IsStopVisibleProperty); set => SetValue(IsStopVisibleProperty, value); }

        // IsLoopVisible
        public static readonly DependencyProperty IsLoopVisibleProperty = DependencyProperty.Register("IsLoopVisible", typeof(bool), typeof(MediaPlayer),
            new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.AffectsParentArrange));
        public bool IsLoopVisible { get => (bool)GetValue(IsLoopVisibleProperty); set => SetValue(IsLoopVisibleProperty, value); }

        // IsVolumeVisible
        public static readonly DependencyProperty IsVolumeVisibleProperty = DependencyProperty.Register("IsVolumeVisible", typeof(bool), typeof(MediaPlayer),
            new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.AffectsParentArrange));
        public bool IsVolumeVisible { get => (bool)GetValue(IsVolumeVisibleProperty); set => SetValue(IsVolumeVisibleProperty, value); }

        // IsSpeedVisible
        public static readonly DependencyProperty IsSpeedVisibleProperty = DependencyProperty.Register("IsSpeedVisible", typeof(bool), typeof(MediaPlayer),
            new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.AffectsParentArrange));
        public bool IsSpeedVisible { get => (bool)GetValue(IsSpeedVisibleProperty); set => SetValue(IsSpeedVisibleProperty, value); }

        // IsSeekBarVisible
        public static readonly DependencyProperty IsSeekBarVisibleProperty = DependencyProperty.Register("IsSeekBarVisible", typeof(bool), typeof(MediaPlayer),
            new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.AffectsParentArrange));
        public bool IsSeekBarVisible { get => (bool)GetValue(IsSeekBarVisibleProperty); set => SetValue(IsSeekBarVisibleProperty, value); }



        public void OnSeekBarPreviewMouseLeftButtonDown(object? sender, MouseButtonEventArgs e)
        {
            if (PlayerHost == null) { return; }
            e.CheckNotNull(nameof(e));

            // Only process event if click is not on thumb.
            if (SeekBarThumbPart != null)
            {
                var pos = e.GetPosition(SeekBarThumbPart);
                if (pos.X < 0 || pos.Y < 0 || pos.X > SeekBarThumbPart.ActualWidth || pos.Y > SeekBarThumbPart.ActualHeight)
                {
                    // Immediate seek when clicking elsewhere.
                    IsSeekBarPressed = true;
                    PlayerHost.Position = PositionBar;
                    IsSeekBarPressed = false;
                }
            }
        }

        public void OnSeekBarDragStarted(object? sender, DragStartedEventArgs e)
        {
            if (PlayerHost == null) { return; }

            IsSeekBarPressed = true;
        }

        private DateTime _lastDragCompleted = DateTime.MinValue;
        public void OnSeekBarDragCompleted(object? sender, DragCompletedEventArgs e)
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


        public FullScreenUI? FullScreenUI { get; private set; }

        public ICommand ToggleFullScreenCommand => CommandHelper.InitCommand(ref _toggleFullScreenCommand, ToggleFullScreen, CanToggleFullScreen);
        private RelayCommand? _toggleFullScreenCommand;
        private bool CanToggleFullScreen() => PlayerHost != null;
        private void ToggleFullScreen()
        {
            FullScreen = !FullScreen;
        }

        public bool FullScreen
        {
            get => FullScreenUI != null;
            set
            {
                if (PlayerHost == null) { return; }
                if (UIPart == null) { return; }
                if (PlayerHost.HostContainer == null) { return; }

                if (value != FullScreen)
                {
                    if (value)
                    {
                        // Create full screen.
                        FullScreenUI = new FullScreenUI();
                        FullScreenUI.Closed += FullScreenUI_Closed;
                        FullScreenUI.MouseDown += Host_MouseDown;
                        // Transfer key bindings.
                        InputBindingBehavior.TransferBindingsToWindow(Window.GetWindow(this), FullScreenUI, false);
                        // Transfer player.
                        TransferElement(PlayerHost.GetInnerControlParent(), FullScreenUI.ContentGrid, PlayerHost.HostContainer);
                        TransferElement(UIParentCache, FullScreenUI.AirspaceGrid, UIPart);
                        FullScreenUI.Airspace.VerticalOffset = -UIPart.ActualHeight;
                        // Show.
                        FullScreenUI.ShowDialog();
                    }
                    else if (FullScreenUI != null)
                    {
                        // Transfer player back.
                        TransferElement(FullScreenUI.ContentGrid, PlayerHost.GetInnerControlParent(), PlayerHost.HostContainer);
                        TransferElement(FullScreenUI.AirspaceGrid, UIParentCache, UIPart);
                        // Close.
                        var f = FullScreenUI;
                        FullScreenUI = null;
                        f.CloseOnce();
                        // Activate.
                        Window.GetWindow(this).Activate();
                        Focus();
                    }
                }
            }
        }

        private static void TransferElement(Panel src, Panel dst, FrameworkElement element)
        {
            src.Children.Remove(element);
            dst.Children.Add(element);
        }

        private void FullScreenUI_Closed(object? sender, EventArgs e)
        {
            FullScreen = false;
        }
    }
}
