using System;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using EmergenceGuardian.MediaPlayerUI.Mvvm;

namespace EmergenceGuardian.MediaPlayerUI {
    [TemplatePart(Name = MediaPlayer.PART_HostGrid, Type = typeof(Grid))]
    [TemplatePart(Name = MediaPlayer.PART_UI, Type = typeof(Border))]
    [TemplatePart(Name = MediaPlayer.PART_SeekBar, Type = typeof(Slider))]
    public class MediaPlayer : MediaPlayerBase, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        public const string PART_HostGrid = "PART_HostGrid";
        public Grid HostGrid => GetTemplateChild(PART_HostGrid) as Grid;
        public const string PART_UI = "PART_UI";
        public Border UI => GetTemplateChild(PART_UI) as Border;
        public const string PART_SeekBar = "PART_SeekBar";
        public Slider SeekBar => GetTemplateChild(PART_SeekBar) as Slider;

        static MediaPlayer() {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(MediaPlayer), new FrameworkPropertyMetadata(typeof(MediaPlayer)));
            BackgroundProperty.OverrideMetadata(typeof(MediaPlayer), new FrameworkPropertyMetadata(Brushes.Black));
            HorizontalAlignmentProperty.OverrideMetadata(typeof(MediaPlayer), new FrameworkPropertyMetadata(HorizontalAlignment.Stretch));
            VerticalAlignmentProperty.OverrideMetadata(typeof(MediaPlayer), new FrameworkPropertyMetadata(VerticalAlignment.Stretch));
            ContentProperty.OverrideMetadata(typeof(MediaPlayer), new FrameworkPropertyMetadata(ContentChanged, CoerceContent));
            // FocusableProperty.OverrideMetadata(typeof(MediaPlayerWpf), new FrameworkPropertyMetadata(false));
        }

        public MediaPlayer() {
        }

        public override void OnApplyTemplate() {
            base.OnApplyTemplate();

            // ApplyTemplate can get called several times (switching full screen), attach to events only once.
            if (DesignerProperties.GetIsInDesignMode(this))
                return;

            Slider Bar = SeekBar;
            if (Bar != null)
            {
                MouseDown += UserControl_MouseDown;
                Bar.AddHandler(Slider.PreviewMouseDownEvent, new MouseButtonEventHandler(base.SeekBar_PreviewMouseLeftButtonDown), true);
                // Thumb doesn't yet exist.
                Bar.Loaded += (s, e) => {
                    Thumb SeekBarThumb = GetSliderThumb(Bar);
                    if (SeekBarThumb != null)
                    {
                        SeekBarThumb.DragStarted += SeekBar_DragStarted;
                        SeekBarThumb.DragCompleted += SeekBar_DragCompleted;
                    }
                };
            }
        }

        private static void ContentChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            MediaPlayerBase.OnContentChanged(d, e);
        }

        private static object CoerceContent(DependencyObject d, object baseValue) => baseValue as PlayerHostBase;

        protected override void OnPreviewMouseLeftButtonDown(MouseButtonEventArgs e) {
            base.OnPreviewMouseLeftButtonDown(e);
            this.Focus();
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

        private void Host_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (ChangeVolumeOnMouseWheel)
            {
                if (e.Delta > 0)
                    PlayerHost.Volume += 5;
                else if (e.Delta < 0)
                    PlayerHost.Volume -= 5;
            }
        }

        private void Host_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            // ** Double clicks aren't working properly yet.
            HandleMouseAction(sender, e, 2);
        }

        private void Host_MouseDown(object sender, MouseButtonEventArgs e)
        {
            HandleMouseAction(sender, e, 1);
        }

        /// <summary>
        /// Handles mouse click events for both Host and Fullscreen.
        /// </summary>
        private void HandleMouseAction(object sender, MouseButtonEventArgs e, int clickCount)
        {
            bool IsFullScreen = sender is FullScreenUI;
            if (IsActionFullScreen(e, clickCount))
            {
                FullScreen = !IsFullScreen; // using !FullScreen can return wrong value when exiting fullscreen
                e.Handled = true;
            }
            else if (IsActionPause(e, clickCount))
            {
                if (PlayPauseCommand.CanExecute(null))
                    PlayPauseCommand.Execute(null);
                e.Handled = true;
            }
        }

        private bool IsActionFullScreen(MouseButtonEventArgs e, int clickCount) => IsMouseAction(MouseFullscreen, e, clickCount);
        private bool IsActionPause(MouseButtonEventArgs e, int clickCount) => IsMouseAction(MousePause, e, clickCount);

        private bool IsMouseAction(MouseTrigger a, MouseButtonEventArgs e, int clickCount)
        {
            if (clickCount != TriggerClickCount(a))
                return false;
            if (a == MouseTrigger.LeftClick && e.ChangedButton == MouseButton.Left)
                return true;
            if (a == MouseTrigger.MiddleClick && e.ChangedButton == MouseButton.Middle)
                return true;
            if (a == MouseTrigger.RightClick && e.ChangedButton == MouseButton.Right)
                return true;
            else
                return false;
        }

        /// <summary>
        /// Returns the amount of clicks represented by specified mouse trigger.
        /// </summary>
        private int TriggerClickCount(MouseTrigger a)
        {
            if (a == MouseTrigger.None)
                return 0;
            else if (a == MouseTrigger.LeftClick || a == MouseTrigger.MiddleClick || a == MouseTrigger.RightClick)
                return 1;
            else
                return 2;
        }

        private Panel uiParentCache;
        /// <summary>
        /// Returns the container of this control the first time it is called and maintain reference to that container.
        /// </summary>
        private Panel UIParentCache {
            get {
                if (uiParentCache == null)
                    uiParentCache = UI.Parent as Panel;
                if (uiParentCache == null)
                    throw new NullReferenceException("UIParentCache returned null.");
                return uiParentCache;
            }
        }

        #region Properties

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

        #endregion


        #region Fullscreen

        public FullScreenUI FullScreenUI { get; private set; }

        private ICommand toggleFullScreenCommand;

        public ICommand ToggleFullScreenCommand => CommandHelper.InitCommand(ref toggleFullScreenCommand, ToggleFullScreen, CanToggleFullScreen);

        private bool CanToggleFullScreen() => PlayerHost != null;
        private void ToggleFullScreen()
        {
            FullScreen = !FullScreen;
        }

        public bool FullScreen {
            get => FullScreenUI != null;
            set {
                if (value != FullScreen)
                {
                    if (PlayerHost == null)
                        throw new ArgumentException("PlayerHost must be set to use FullScreen");
                    if (value)
                    {
                        // Create full screen.
                        FullScreenUI = new FullScreenUI();
                        FullScreenUI.Closed += FullScreenUI_Closed;
                        FullScreenUI.MouseDown += Host_MouseDown;
                        // Transfer key bindings.
                        InputBindingBehavior.TransferBindingsToWindow(Window.GetWindow(this), FullScreenUI, false);
                        // Transfer player.
                        TransferElement(PlayerHost.InnerControlParentCache, FullScreenUI.ContentGrid, PlayerHost.InnerControl);
                        TransferElement(UIParentCache, FullScreenUI.AirspaceGrid, UI);
                        FullScreenUI.Airspace.VerticalOffset = -UI.ActualHeight;
                        // Show.
                        FullScreenUI.ShowDialog();
                    }
                    else if (FullScreenUI != null)
                    {
                        // Transfer player back.
                        TransferElement(FullScreenUI.ContentGrid, PlayerHost.InnerControlParentCache, PlayerHost.InnerControl);
                        TransferElement(FullScreenUI.AirspaceGrid, UIParentCache, UI);
                        // Close.
                        var F = FullScreenUI;
                        FullScreenUI = null;
                        F.CloseOnce();
                        // Activate.
                        Window.GetWindow(this).Activate();
                        this.Focus();
                    }
                }
            }
        }

        private static void TransferElement(Panel src, Panel dst, FrameworkElement element)
        {
            if (src == null) throw new ArgumentNullException(nameof(src));
            if (dst == null) throw new ArgumentNullException(nameof(dst));
            if (element == null) throw new ArgumentNullException(nameof(element));

            src.Children.Remove(element);
            dst.Children.Add(element);
        }

        private void FullScreenUI_Closed(object sender, EventArgs e)
        {
            FullScreen = false;
        }

        #endregion

    }
}
