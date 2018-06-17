using System;
using System.ComponentModel;
using System.Windows;
using EmergenceGuardian.MediaPlayerUI;

namespace MpvPlayer {
	public class MpvMediaPlayer : MediaPlayerWpf {
		static MpvMediaPlayer() {
			DefaultStyleKeyProperty.OverrideMetadata(typeof(MpvMediaPlayer), new FrameworkPropertyMetadata(typeof(MpvMediaPlayer)));
		}

		public event EventHandler MediaPlayerInitialized;

		public MpvMediaPlayer() {
		}

		public override void OnApplyTemplate() {
			base.OnApplyTemplate();

			if (DesignerProperties.GetIsInDesignMode(this))
				return;

			this.Loaded += UserControl_Loaded;
		}

		private void UserControl_Loaded(object sender, System.Windows.RoutedEventArgs e) {
			var PlayerHost = new MpvMediaPlayerHost();
			base.Host = PlayerHost;
			PlayerHost.MediaPlayerInitialized += (s2, e2) => {
				MediaPlayerInitialized?.Invoke(s2, e2);
			};
		}

		public new MpvMediaPlayerHost Host {
			get => base.Host as MpvMediaPlayerHost;
			set => base.Host = value;
		}
	}
}
