using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Threading;
using Mpv.WPF;
using System.Windows.Forms.Integration;
using EmergenceGuardian.MediaPlayerUI;

namespace Business {
	/// <summary>
	/// Interaction logic for WpfMPlayerControl.xaml
	/// </summary>
	public partial class WpfMPlayerControl {
		public Mpv.WPF.MpvPlayer Player;
		public event EventHandler MediaPlayerInitialized;
		private string source;

		public WpfMPlayerControl() {
			InitializeComponent();
		}

		private void UserControl_Loaded(object sender, RoutedEventArgs e) {
			Player = new Mpv.WPF.MpvPlayer(HostPanel.Handle, "lib\\mpv-1.dll");
			//Player.MediaLoaded += Player_MediaLoaded;
			//Player.MediaUnloaded += Player_MediaUnloaded;
			//Player.PositionChanged += Player_PositionChanged;
			Player.AutoPlay = true;
			//UI.Volume = 30;


			Dispatcher.ShutdownStarted += (s2, e2) => Player.Dispose();

			MediaPlayerInitialized?.Invoke(this, new EventArgs());
		}

		//private void UI_OnVolumeChanged1(object sender, EventArgs e) {
		//	Player.Volume = UI.Volume;
		//}

		//private void Dispatcher_ShutdownStarted(object sender, EventArgs e) {
		//	Player.Dispose();
		//}

		//public bool IsVideoVisible {
		//	get {
		//		return Host.Visibility == Visibility.Visible;
		//	}
		//	set {
		//		Host.Visibility = value ? Visibility.Visible : Visibility.Hidden;
		//	}
		//}

		//public void Play(string source) {
		//	this.source = source;
		//	UI.Title = Path.GetFileName(source);
		//	Player.Stop();
		//	Player.KeepOpen = KeepOpen.Always;
		//	Player.Load(source, true);
		//}

		//private void Player_PositionChanged(object sender, EventArgs e) {
		//	Dispatcher.Invoke(() => {
		//		UI.Position = Player.Position;
		//	});
		//}

		//private void Player_MediaUnloaded(object sender, EventArgs e) {
		//	Dispatcher.Invoke(() => {
		//		UI.MediaUnloaded();
		//	});
		//}

		//public void Player_MediaLoaded(object sender, EventArgs e) {
		//	Dispatcher.Invoke(() => {
		//		UI.MediaLoaded(Player.Duration);
		//	});
		//}

		//private void UI_OnPlay(object sender, EventArgs e) {
		//	Player.Resume();
		//}

		//private void UI_OnPause(object sender, EventArgs e) {
		//	Player.Pause();
		//}

		//private void UI_OnStop(object sender, EventArgs e) {
		//	Player.Stop();
		//}

		//private void UI_OnSeek(object sender, EventArgs e) {
		//	Player.Position = UI.PositionBar;
		//}

		//private void UI_OnToggleLoop(object sender, EventArgs e) {
		//	Player.Loop = UI.Loop;
		//}

		//private void UI_OnVolumeChanged(object sender, EventArgs e) {
		//	Player.Volume = UI.Volume;
		//}

		//private void UI_OnSpeedChanged(object sender, EventArgs e) {
		//	Player.Speed = UI.Speed;
		//}

		//public int Volume {
		//	get => Player?.Volume ?? 0;
		//	set {
		//		Player.Volume = value;
		//	}
		//}
	}
}