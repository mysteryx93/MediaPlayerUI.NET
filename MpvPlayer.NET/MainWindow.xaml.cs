using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace EmergenceGuardian.MpvPlayer {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window {
        public MainWindow() {
            InitializeComponent();

			Player.MediaPlayerInitialized += player_MediaPlayerInitialized;
		}

		private void Window_Loaded(object sender, RoutedEventArgs e) {
			InputBindings.Add(new KeyBinding(Player.UI.PlayPauseCommand, Key.Space, ModifierKeys.None));
			//MpvMediaPlayerHost Host = new MpvMediaPlayerHost();
			//Host.MediaPlayerInitialized += player_MediaPlayerInitialized;
			//Player.Host = Host;
		}

		public void player_MediaPlayerInitialized(object sender, EventArgs e) {
			Player.Host.Load(@"E:\NaturalGrounding\AOA\Like a Cat.mp4", "");
		}
	}
}
