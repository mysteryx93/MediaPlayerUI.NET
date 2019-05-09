using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

namespace Sample {
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window {
		public MainWindow() {
			InitializeComponent();

            // 1. Make sure mpv-1.dll is in the project's folder.

            // 2. Point to a valid media file.

            // 3. Run.

            // If you need further customization of the player, MpvPlayer.NET is a very simple project that you can edit as you wish.

            Player.MediaPlayerInitialized += (o, e) => {
                Player.Host.Load(@"E:\NaturalGrounding\AOA\Like a Cat.mp4");
            };
        }
	}
}
