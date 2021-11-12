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
using NAudio.Wave;
using SoundTouch.Net.NAudioSupport;

namespace Sample
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            // 1. Make sure mpv-1.dll is in the project's folder.

            // 2. Point to a valid media file.

            // 3. Run.

            //PlayerHost.MediaPlayerInitialized += (o, e) => {
            //    PlayerHost.Source = @"E:\NaturalGrounding\AOA\Like a Cat.mp4";
            //};
        }

        private WaveOutEvent _mediaOut = new WaveOutEvent();
        private SoundTouchWaveStream? _mediaFile;

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            var Source = @"E:\Music\Inna - Body And The Sun (Super Deluxe Edition) (2015)\CD1\07 Fool Me.mp3";
            _mediaOut.Stop();
            using var reader = new MediaFoundationReader(Source, new MediaFoundationReader.MediaFoundationReaderSettings() { RequestFloatOutput = true });
            _mediaFile = new SoundTouchWaveStream(reader);
            _mediaOut.Init(reader);
            _mediaOut.Play();
        }

        private void PlayerHost_MediaUnloaded(object sender, EventArgs e)
        {

        }
    }
}
