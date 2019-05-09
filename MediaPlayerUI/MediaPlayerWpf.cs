using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace EmergenceGuardian.MediaPlayerUI {
    [TemplatePart(Name = MediaPlayerWpf.PART_MediaUI, Type = typeof(PlayerUI))]
    [TemplatePart(Name = MediaPlayerWpf.PART_HostGrid, Type = typeof(Grid))]
    public class MediaPlayerWpf : ContentControl {
        public const string PART_MediaUI = "PART_MediaUI";
        public PlayerUI MediaUI => GetTemplateChild(PART_MediaUI) as PlayerUI;
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
            UI = MediaUI;
            UI.PlayerHost = Content as PlayerBase;
        }

        private static void ContentChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            MediaPlayerWpf P = d as MediaPlayerWpf;
            if (P.MediaUI != null)
                P.MediaUI.PlayerHost = e.NewValue as PlayerBase;
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
        private static readonly DependencyProperty UIProperty = UIPropertyKey.DependencyProperty;
        public PlayerUI UI { get => (PlayerUI)GetValue(UIProperty); private set => SetValue(UIPropertyKey, value); }

        //// Host
        //public static readonly DependencyProperty HostProperty = DependencyProperty.Register("Host", typeof(PlayerBase), typeof(MediaPlayerWpf), 
        //    new PropertyMetadata(HostChanged));
        //public PlayerBase Host { get => (PlayerBase)GetValue(HostProperty); set => SetValue(HostProperty, value); }
        //private static void HostChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
        //    MediaPlayerWpf P = d as MediaPlayerWpf;
        //    if (e.OldValue is PlayerBase OldValue) {
        //        P.HostGrid.Children.Remove(OldValue);
        //    }
        //    if (e.NewValue is PlayerBase NewValue) {
        //        P.HostGrid.Children.Add(NewValue);
        //        P.MediaUI.PlayerHost = NewValue;
        //    }
        //}
    }
}
