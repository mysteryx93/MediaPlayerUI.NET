using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms.Integration;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using EmergenceGuardian.MediaPlayerUI;

namespace EmergenceGuardian.MpvPlayerUI {
	[TemplatePart(Name = MpvMediaPlayerHost.PART_Host, Type = typeof(WindowsFormsHost))]
	public class MpvMediaPlayerHost : PlayerBase {
		public const string PART_Host= "PART_Host";
		public WindowsFormsHost Host => GetTemplateChild(PART_Host) as WindowsFormsHost;

		static MpvMediaPlayerHost() {
			DefaultStyleKeyProperty.OverrideMetadata(typeof(MpvMediaPlayerHost), new FrameworkPropertyMetadata(typeof(MpvMediaPlayerHost)));
		}

		public string DllPath { get; set; }
		public Mpv.NET.Player.MpvPlayer Player;
		public event EventHandler MediaPlayerInitialized;
		private string source;


		public override void OnApplyTemplate() {
			base.OnApplyTemplate();

			if (DesignerProperties.GetIsInDesignMode(this))
				return;

			Loaded += UserControl_Loaded;
			Unloaded += UserControl_Unloaded;
			Dispatcher.ShutdownStarted += (s2, e2) => UserControl_Unloaded(s2, null);
		}

		public MpvMediaPlayerHost() {
		}

		private void UserControl_Loaded(object sender, RoutedEventArgs e) {
			Player = new Mpv.NET.Player.MpvPlayer(Host.Handle, DllPath);
            Player.MediaLoaded += Player_MediaLoaded;
            Player.MediaUnloaded += Player_MediaUnloaded;
            Player.PositionChanged += Player_PositionChanged;
			Player.AutoPlay = true;
			InvokePropertyChanged("Volume");

			MediaPlayerInitialized?.Invoke(this, new EventArgs());
		}

        private void Player_MediaLoaded(object sender, EventArgs e) {
            Dispatcher.Invoke(() => base.MediaLoaded());
        }

        private void Player_MediaUnloaded(object sender, EventArgs e) {
            Dispatcher.Invoke(() => base.MediaUnloaded());
        }

        private void Player_PositionChanged(object sender, Mpv.NET.Player.PositionChangedEventArgs e) {
            Dispatcher.BeginInvoke(new Action(() => base.PositionChanged()));
        }

        private void UserControl_Unloaded(object sender, RoutedEventArgs e) {
			Player?.Dispose();
			Player = null;
		}

		public override FrameworkElement InnerControl => Host;

		public override TimeSpan Position {
			get => Player?.Position ?? TimeSpan.Zero;
			set {
				if (Player.IsMediaLoaded) {
					Player.Position = value;
                    base.PositionChanged();
				}
			}
		}

		public override TimeSpan Duration {
			get => Player?.Duration ?? TimeSpan.FromSeconds(1);
		}

		public override bool IsPlaying {
			get => Player?.IsPlaying ?? false;
			set {
				if (value)
					Player?.Resume();
				else
					Player?.Pause();
				InvokePropertyChanged("IsPlaying");
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
			Load(source, null);
		}

		public void Load(string source, string title) {
			this.source = source;
			if (title == null)
				Title = System.IO.Path.GetFileName(source);
			Player.Stop();
			Player.Load(source, true);
		}

		public override void Stop() {
			base.Stop();
			Player.Stop();
		}
	}
}
