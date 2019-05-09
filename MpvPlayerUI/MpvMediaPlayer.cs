using System;
using System.ComponentModel;
using System.Windows;
using EmergenceGuardian.MediaPlayerUI;

namespace EmergenceGuardian.MpvPlayerUI {
	public class MpvMediaPlayer : MediaPlayerWpf, ISupportInitialize {
		static MpvMediaPlayer() {
			// DefaultStyleKeyProperty.OverrideMetadata(typeof(MpvMediaPlayer), new FrameworkPropertyMetadata(typeof(MpvMediaPlayer)));
		}

		public event EventHandler MediaPlayerInitialized;

		public MpvMediaPlayer() {
        }

        public override void OnApplyTemplate() {
			base.OnApplyTemplate();

			if (DesignerProperties.GetIsInDesignMode(this))
				return;

            BeginInit();
            var PlayerHost = Host ?? new MpvMediaPlayerHost();
            PlayerHost.DllPath = DllPath;
            if (Content == null)
                Content = PlayerHost;
            PlayerHost.MediaPlayerInitialized += (s2, e2) => {
                MediaPlayerInitialized?.Invoke(s2, e2);
                EndInit();
            };
        }

		// DllPath
		public static DependencyProperty DllPathProperty = DependencyProperty.Register("DllPath", typeof(string), typeof(MpvMediaPlayer));
		public string DllPath { get => (string)GetValue(DllPathProperty); set => SetValue(DllPathProperty, value); }

		public MpvMediaPlayerHost Host {
			get => Content as MpvMediaPlayerHost;
			set => Content = value;
		}
	}
}
