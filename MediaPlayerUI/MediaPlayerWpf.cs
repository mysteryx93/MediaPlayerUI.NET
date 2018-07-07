using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace EmergenceGuardian.MediaPlayerUI {
    [TemplatePart(Name = MediaPlayerWpf.PART_MediaUI, Type = typeof(PlayerUI))]
    [TemplatePart(Name = MediaPlayerWpf.PART_HostGrid, Type = typeof(Grid))]
    public class MediaPlayerWpf : ContentControl {
        public const string PART_MediaUI = "PART_MediaUI";
        public PlayerUI TemplateUI => GetTemplateChild(PART_MediaUI) as PlayerUI;
        public const string PART_HostGrid = "PART_HostGrid";
        public Grid HostGrid => GetTemplateChild(PART_HostGrid) as Grid;

        static MediaPlayerWpf() {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(MediaPlayerWpf), new FrameworkPropertyMetadata(typeof(MediaPlayerWpf)));
            BackgroundProperty.OverrideMetadata(typeof(MediaPlayerWpf), new FrameworkPropertyMetadata(Brushes.Black));
            HorizontalAlignmentProperty.OverrideMetadata(typeof(MediaPlayerWpf), new FrameworkPropertyMetadata(HorizontalAlignment.Stretch));
            VerticalAlignmentProperty.OverrideMetadata(typeof(MediaPlayerWpf), new FrameworkPropertyMetadata(VerticalAlignment.Stretch));
            ContentProperty.OverrideMetadata(typeof(MediaPlayerWpf), new FrameworkPropertyMetadata(ContentChanged, CoerceContent));
        }

        public MediaPlayerWpf() {
        }

        public override void OnApplyTemplate() {
            base.OnApplyTemplate();
            UI = TemplateUI;
            UI.PlayerHost = Content as PlayerBase;
        }

        private static void ContentChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            MediaPlayerWpf P = d as MediaPlayerWpf;
            if (P.TemplateUI != null)
                P.TemplateUI.PlayerHost = e.NewValue as PlayerBase;
        }

        private static object CoerceContent(DependencyObject d, object baseValue) {
            return baseValue as PlayerBase;
        }

        protected override void OnPreviewMouseLeftButtonDown(MouseButtonEventArgs e) {
            base.OnPreviewMouseLeftButtonDown(e);
            this.Focus();
        }

        // UI
        public static readonly DependencyPropertyKey UIPropertyKey = DependencyProperty.RegisterReadOnly("UI", typeof(PlayerUI), typeof(MediaPlayerWpf), new PropertyMetadata());
        public static readonly DependencyProperty UIProperty = UIPropertyKey.DependencyProperty;
        public PlayerUI UI { get => (PlayerUI)GetValue(UIProperty); private set => SetValue(UIPropertyKey, value); }


        // These are bound to the same properties on UI

        // MouseFullscreen
        public static readonly DependencyProperty MouseFullscreenProperty = DependencyProperty.Register("MouseFullscreen", typeof(MouseTrigger), typeof(MediaPlayerWpf),
            new PropertyMetadata(MouseTrigger.MiddleClick));
        public MouseTrigger MouseFullscreen { get => (MouseTrigger)GetValue(MouseFullscreenProperty); set => SetValue(MouseFullscreenProperty, value); }

        // MousePause
        public static readonly DependencyProperty MousePauseProperty = DependencyProperty.Register("MousePause", typeof(MouseTrigger), typeof(MediaPlayerWpf),
            new PropertyMetadata(MouseTrigger.LeftClick));
        public MouseTrigger MousePause { get => (MouseTrigger)GetValue(MousePauseProperty); set => SetValue(MousePauseProperty, value); }

        // IsPlayPauseVisible
        public static readonly DependencyProperty IsPlayPauseVisibleProperty = DependencyProperty.Register("IsPlayPauseVisible", typeof(bool), typeof(MediaPlayerWpf),
            new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.AffectsParentArrange));
        public bool IsPlayPauseVisible { get => (bool)GetValue(IsPlayPauseVisibleProperty); set => SetValue(IsPlayPauseVisibleProperty, value); }

        // IsStopVisible
        public static readonly DependencyProperty IsStopVisibleProperty = DependencyProperty.Register("IsStopVisible", typeof(bool), typeof(MediaPlayerWpf),
            new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.AffectsParentArrange));
        public bool IsStopVisible { get => (bool)GetValue(IsStopVisibleProperty); set => SetValue(IsStopVisibleProperty, value); }

        // IsLoopVisible
        public static readonly DependencyProperty IsLoopVisibleProperty = DependencyProperty.Register("IsLoopVisible", typeof(bool), typeof(MediaPlayerWpf),
            new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.AffectsParentArrange));
        public bool IsLoopVisible { get => (bool)GetValue(IsLoopVisibleProperty); set => SetValue(IsLoopVisibleProperty, value); }

        // IsVolumeVisible
        public static readonly DependencyProperty IsVolumeVisibleProperty = DependencyProperty.Register("IsVolumeVisible", typeof(bool), typeof(MediaPlayerWpf),
            new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.AffectsParentArrange));
        public bool IsVolumeVisible { get => (bool)GetValue(IsVolumeVisibleProperty); set => SetValue(IsVolumeVisibleProperty, value); }

        // IsSpeedVisible
        public static readonly DependencyProperty IsSpeedVisibleProperty = DependencyProperty.Register("IsSpeedVisible", typeof(bool), typeof(MediaPlayerWpf),
            new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.AffectsParentArrange));
        public bool IsSpeedVisible { get => (bool)GetValue(IsSpeedVisibleProperty); set => SetValue(IsSpeedVisibleProperty, value); }

        // IsSeekBarVisible
        public static readonly DependencyProperty IsSeekBarVisibleProperty = DependencyProperty.Register("IsSeekBarVisible", typeof(bool), typeof(MediaPlayerWpf),
            new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.AffectsParentArrange));
        public bool IsSeekBarVisible { get => (bool)GetValue(IsSeekBarVisibleProperty); set => SetValue(IsSeekBarVisibleProperty, value); }

        // PositionDisplay
        public static readonly DependencyProperty PositionDisplayProperty = DependencyProperty.Register("PositionDisplay", typeof(TimeDisplayStyles), typeof(MediaPlayerWpf),
            new PropertyMetadata(TimeDisplayStyles.Standard));
        public TimeDisplayStyles PositionDisplay { get => (TimeDisplayStyles)GetValue(PositionDisplayProperty); set => SetValue(PositionDisplayProperty, value); }

        // PositionBar
        public static readonly DependencyProperty PositionBarProperty = DependencyProperty.Register("PositionBar", typeof(TimeSpan), typeof(MediaPlayerWpf),
            new FrameworkPropertyMetadata(TimeSpan.Zero, FrameworkPropertyMetadataOptions.AffectsRender));
        public TimeSpan PositionBar { get => (TimeSpan)GetValue(PositionBarProperty); set => SetValue(PositionBarProperty, value); }
    }
}
