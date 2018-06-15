using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace EmergenceGuardian.MediaPlayerUI {
    /// <summary>
    /// Interaction logic for MediaPlayerUI.xaml
    /// </summary>
    public partial class PlayerControls : PlayerControlsBase {
		public PlayerControls():base() {
            InitializeComponent();

			SeekBar.AddHandler(Slider.PreviewMouseDownEvent, new MouseButtonEventHandler(base.SeekBar_PreviewMouseLeftButtonDown), true);
		}

		protected override void OnPlayerHostChanged(DependencyPropertyChangedEventArgs e) {
			if (e.OldValue != null)
				((FrameworkElement)e.OldValue).MouseDown -= Host_MouseDown;
			if (e.NewValue != null)
				((FrameworkElement)e.NewValue).MouseDown += Host_MouseDown;
		}

		private void Host_MouseDown(object sender, MouseButtonEventArgs e) {
			if (e.ChangedButton == MouseButton.Middle)
				FullScreen = true;
		}

		private Panel parentCache;
		/// <summary>
		/// Returns the container of this control the first time it is called and maintain reference to that container.
		/// </summary>
		private Panel ParentCache {
			get {
				if (parentCache == null)
					parentCache = Parent as Panel;
				if (parentCache == null)
					throw new ArgumentNullException("ParentCache returned null.");
				return parentCache;
			}
		}

		#region Properties

		public static DependencyProperty TitleProperty = DependencyProperty.Register("Title", typeof(bool), typeof(PlayerControls));
		public string Title { get => (string)GetValue(TitleProperty); set => SetValue(TitleProperty, value); }

		public static DependencyProperty IsPlayPauseVisibleProperty = DependencyProperty.Register("IsPlayPauseVisible", typeof(bool), typeof(PlayerControls), new PropertyMetadata(true));
		public bool IsPlayPauseVisible { get => (bool)GetValue(IsPlayPauseVisibleProperty); set => SetValue(IsPlayPauseVisibleProperty, value); }

		public static DependencyProperty IsStopVisibleProperty = DependencyProperty.Register("IsStopVisible", typeof(bool), typeof(PlayerControls), new PropertyMetadata(true));
		public bool IsStopVisible { get => (bool)GetValue(IsStopVisibleProperty); set => SetValue(IsStopVisibleProperty, value); }

		public static DependencyProperty IsLoopVisibleProperty = DependencyProperty.Register("IsLoopVisible", typeof(bool), typeof(PlayerControls), new PropertyMetadata(true));
		public bool IsLoopVisible { get => (bool)GetValue(IsLoopVisibleProperty); set => SetValue(IsLoopVisibleProperty, value); }

		public static DependencyProperty IsVolumeVisibleProperty = DependencyProperty.Register("IsVolumeVisible", typeof(bool), typeof(PlayerControls), new PropertyMetadata(true));
		public bool IsVolumeVisible { get => (bool)GetValue(IsVolumeVisibleProperty); set => SetValue(IsVolumeVisibleProperty, value); }

		public static DependencyProperty IsSpeedVisibleProperty = DependencyProperty.Register("IsSpeedVisible", typeof(bool), typeof(PlayerControls), new PropertyMetadata(true));
		public bool IsSpeedVisible { get => (bool)GetValue(IsSpeedVisibleProperty); set => SetValue(IsSpeedVisibleProperty, value); }

		public static DependencyProperty IsSeekBarVisibleProperty = DependencyProperty.Register("IsSeekBarVisible", typeof(bool), typeof(PlayerControls), new PropertyMetadata(true));
		public bool IsSeekBarVisible { get => (bool)GetValue(IsSeekBarVisibleProperty); set => SetValue(IsSeekBarVisibleProperty, value); }

		#endregion


		#region Fullscreen

		public FullScreenUI FullScreenUI { get; private set; }

		private ICommand toggleFullScreenCommand;
		public ICommand ToggleFullScreenCommand => CommandHelper.InitCommand(ref toggleFullScreenCommand, ToggleFullScreen, CanToggleFullScreen);

		private bool CanToggleFullScreen() => PlayerHost != null;
		private void ToggleFullScreen() {
			FullScreen = !FullScreen;
		}

		public bool FullScreen {
			get => FullScreenUI != null;
			set {
				if (value != FullScreen) {
					if (PlayerHost == null)
						throw new ArgumentException("PlayerContainer and PlayerHost must be set to use FullScreen");
					if (value) {
						// Create full screen.
						FullScreenUI = new FullScreenUI();
						FullScreenUI.Closed += (o, e) => FullScreen = false;
						// Transfer key bindings.
						InputBindingBehavior.TransferBindingsToWindow(Window.GetWindow(this), FullScreenUI, false);
						// Transfer player.
						PlayerHost.InnerControlParentCache.Children.Remove(PlayerHost.InnerControl);
						FullScreenUI.ContentGrid.Children.Add(PlayerHost.InnerControl);
						// Transfer UI.
						ParentCache.Children.Remove(this);
						FullScreenUI.AirspaceGrid.Children.Add(this);
						FullScreenUI.Airspace.VerticalOffset = -this.ActualHeight;
						// Show.
						FullScreenUI.Show();
						FullScreenUI.Activate();
					} else if (FullScreenUI != null) {
						// Transfer player back.
						FullScreenUI.ContentGrid.Children.Remove(PlayerHost.InnerControl);
						//PlayerContainer.Children.Add(PlayerHost);
						PlayerHost.InnerControlParentCache.Children.Add(PlayerHost.InnerControl);
						// Transfer UI back.
						FullScreenUI.AirspaceGrid.Children.Remove(this);
						ParentCache.Children.Add(this);
						// Close.
						var F = FullScreenUI;
						FullScreenUI = null;
						F.CloseOnce();
						// Activate.
						Window.GetWindow(this).Activate();
						this.Focus();
					}
				}
			}
		}

		#endregion

	}
}
