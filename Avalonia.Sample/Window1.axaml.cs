using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace HanumanInstitute.MediaPlayer.Avalonia.Sample
{
    public class Window1 : Window
    {
        public Window1()
        {
            InitializeComponent();
#if DEBUG
            this.AttachDevTools();
#endif
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}

