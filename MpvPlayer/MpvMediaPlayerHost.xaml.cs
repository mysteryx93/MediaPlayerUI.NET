using System;
using System.Windows;
using EmergenceGuardian.MediaPlayerUI;

namespace MpvPlayer {
	/// <summary>
	/// Interaction logic for MpvPlayerHost.xaml
	/// </summary>
	public partial class MpvMediaPlayerHost : PlayerBase {
		public Mpv.WPF.MpvPlayer Player;
		public event EventHandler MediaPlayerInitialized;
		private string source;

		public MpvMediaPlayerHost() {
			InitializeComponent();
		}

		private void PlayerBase_Loaded(object sender, RoutedEventArgs e) {
			Player = new Mpv.WPF.MpvPlayer(HostPanel.Handle, "lib\\mpv-1.dll");
			Player.MediaLoaded += (s, a) => base.MediaLoaded();
			Player.MediaUnloaded += (s, a) => base.MediaUnloaded();
			Player.PositionChanged += (s, a) => base.PositionChanged();
			Player.AutoPlay = true;
			InvokePropertyChanged("Volume");

			Dispatcher.ShutdownStarted += (s2, e2) => Player.Dispose();

			MediaPlayerInitialized?.Invoke(this, new EventArgs());
		}

		public override FrameworkElement InnerControl => Host;

		public override TimeSpan Position {
			get => Player?.Position ?? TimeSpan.Zero;
			set {
				if (Player.IsMediaLoaded) {
					Player.Position = value;
					InvokePropertyChanged("Position");
				}
			}
		}

		public override TimeSpan Duration {
			get => Player?.Duration ?? TimeSpan.FromSeconds(1);
		}

		public override bool Paused {
			get => Player?.IsPlaying ?? false;
			set {
				if (value)
					Player?.Pause();
				else
					Player?.Resume();
			}
		}

		public override int Volume {
			get => Player?.Volume ?? 0;
			set {
				Player.Volume = value;
				InvokePropertyChanged("Volume");
			}
		}

		public override int SpeedInt {
			get => speedInt;
			set {
				speedFloat = 1;
				speedInt = value;
				Player.Speed = GetSpeed();
				InvokePropertyChanged("SpeedInt");
			}
		}

		public override float SpeedFloat {
			get => (float?)Player?.Speed ?? 1f;
			set {
				speedInt = 0;
				speedFloat = value;
				Player.Speed = value;
				InvokePropertyChanged("SpeedFloat");
			}
		}

		public override bool Loop {
			get => Player?.Loop ?? false;
			set {
				Player.Loop = value;
				InvokePropertyChanged("Loop");
			}
		}

		public override void Load(string source) {
			this.source = source;
			Title = System.IO.Path.GetFileName(source);
			Player.Stop();
			Player.Load(source, true);
		}

		public override void Stop() {
			Player.Stop();
		}
	}
}
