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
using System.Windows.Shapes;

namespace EmergenceGuardian.MediaPlayerUI {
    /// <summary>
    /// Interaction logic for FullScreenUI.xaml
    /// </summary>
    public partial class FullScreenUI : Window {
        public bool IsClosing { get; private set; }

        public FullScreenUI() {
            InitializeComponent();
        }

        public Grid ContentGrid => this.MainGrid;

        public void CloseOnce() {
            if (!IsClosing) {
                IsClosing = true;
                this.Close();
            }
        }

        private void UI_Closing(object sender, System.ComponentModel.CancelEventArgs e) {
            IsClosing = true;
        }

        private void Window_Deactivated(object sender, EventArgs e) {
            if (IsLoaded)
                CloseOnce();
        }
    }
}
