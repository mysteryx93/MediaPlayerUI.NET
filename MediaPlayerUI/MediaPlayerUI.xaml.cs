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
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace EmergenceGuardian.MediaPlayerUI {
    /// <summary>
    /// Interaction logic for MediaPlayerUI.xaml
    /// </summary>
    public partial class MediaPlayerUI : MediaPlayerUIViewModel {
		private string title;
		public FullScreenUI FullScreenUI { get; private set; }

		public MediaPlayerUI():base() {
            InitializeComponent();

			SeekBar.AddHandler(Slider.PreviewMouseDownEvent, new MouseButtonEventHandler(base.SeekBar_PreviewMouseLeftButtonDown), true);
			SeekBar.AddHandler(Slider.PreviewMouseDownEvent, new MouseButtonEventHandler(base.SeekBar_PreviewMouseLeftButtonUp), true);
		}

		public string Title {
			get => title;
			set {
				title = value;
				InvokePropertyChanged("Title");
			}
		}

		private ICommand toggleFullScreenCommand;
		public ICommand ToggleFullScreenCommand => InitCommand(ref toggleFullScreenCommand, ToggleFullScreen, CanToggleFullScreen);

		private bool CanToggleFullScreen() => PlayerContainer != null && PlayerHost != null;
		private void ToggleFullScreen() {
			FullScreen = !FullScreen;
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

		public bool FullScreen {
			get => FullScreenUI != null;
			set {
				if (value != FullScreen) {
					if (PlayerContainer == null || PlayerHost == null)
						throw new ArgumentException("PlayerContainer and PlayerHost must be set to use FullScreen");
					if (value) {
						// Create full screen.
						FullScreenUI = new FullScreenUI();
						FullScreenUI.Closed += (o, e) => FullScreen = false;
						// Transfer key bindings.
						InputBindingBehavior.TransferBindingsToWindow(Window.GetWindow(this), FullScreenUI, false);
						// Transfer player.
						PlayerContainer.Children.Remove(PlayerHost);
						FullScreenUI.ContentGrid.Children.Add(PlayerHost);
						// Transfer UI.
						ParentCache.Children.Remove(this);
						FullScreenUI.AirspaceGrid.Children.Add(this);
						FullScreenUI.Airspace.VerticalOffset = -this.ActualHeight;
						// Show.
						FullScreenUI.Show();
						FullScreenUI.Activate();
					} else if (FullScreenUI != null) {
						// Transfer player back.
						FullScreenUI.ContentGrid.Children.Remove(PlayerHost);
						PlayerContainer.Children.Add(PlayerHost);
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

		public static DependencyProperty PlayerContainerProperty = DependencyProperty.Register("PlayerContainer", typeof(Panel), typeof(MediaPlayerUI));
		public Panel PlayerContainer {
			get => (Panel)base.GetValue(PlayerContainerProperty);
			set => base.SetValue(PlayerContainerProperty, value);
		}

		public static DependencyProperty PlayerHostProperty = DependencyProperty.Register("PlayerHost", typeof(FrameworkElement), typeof(MediaPlayerUI), new PropertyMetadata(null, OnPlayerHostChanged));
		public FrameworkElement PlayerHost {
			get => (FrameworkElement)base.GetValue(PlayerHostProperty);
			set => base.SetValue(PlayerHostProperty, value);
		}
		private static void OnPlayerHostChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
			MediaPlayerUI P = d as MediaPlayerUI;
			if (e.OldValue != null)
				((FrameworkElement)e.OldValue).MouseDown -= P.Host_MouseDown;
			if (e.NewValue != null)
				((FrameworkElement)e.NewValue).MouseDown += P.Host_MouseDown;
		}

		public static DependencyProperty IsPlayPauseVisibleProperty = DependencyProperty.Register("IsPlayPauseVisible", typeof(bool), typeof(MediaPlayerUI), new PropertyMetadata(true));
		public bool IsPlayPauseVisible {
			get => (bool)base.GetValue(IsPlayPauseVisibleProperty);
			set => base.SetValue(IsPlayPauseVisibleProperty, value);
		}

		public static DependencyProperty IsStopVisibleProperty = DependencyProperty.Register("IsStopVisible", typeof(bool), typeof(MediaPlayerUI), new PropertyMetadata(true));
		public bool IsStopVisible {
			get => (bool)base.GetValue(IsStopVisibleProperty);
			set => base.SetValue(IsStopVisibleProperty, value);
		}

		public static DependencyProperty IsLoopVisibleProperty = DependencyProperty.Register("IsLoopVisible", typeof(bool), typeof(MediaPlayerUI), new PropertyMetadata(true));
		public bool IsLoopVisible {
			get => (bool)base.GetValue(IsLoopVisibleProperty);
			set => base.SetValue(IsLoopVisibleProperty, value);
		}

		public static DependencyProperty IsVolumeVisibleProperty = DependencyProperty.Register("IsVolumeVisible", typeof(bool), typeof(MediaPlayerUI), new PropertyMetadata(true));
		public bool IsVolumeVisible {
			get => (bool)base.GetValue(IsVolumeVisibleProperty);
			set => base.SetValue(IsVolumeVisibleProperty, value);
		}

		public static DependencyProperty IsSpeedVisibleProperty = DependencyProperty.Register("IsSpeedVisible", typeof(bool), typeof(MediaPlayerUI), new PropertyMetadata(true));
		public bool IsSpeedVisible {
			get => (bool)base.GetValue(IsSpeedVisibleProperty);
			set => base.SetValue(IsSpeedVisibleProperty, value);
		}

		public static DependencyProperty IsSeekBarVisibleProperty = DependencyProperty.Register("IsSeekBarVisible", typeof(bool), typeof(MediaPlayerUI), new PropertyMetadata(true));
		public bool IsSeekBarVisible {
			get => (bool)base.GetValue(IsSeekBarVisibleProperty);
			set => base.SetValue(IsSeekBarVisibleProperty, value);
		}

		private void SeekBar_DragStarted(object sender, System.Windows.Controls.Primitives.DragStartedEventArgs e) {
			base.SeekBar_PreviewMouseLeftButtonDown(sender, null);
		}

		private void SeekBar_DragCompleted(object sender, System.Windows.Controls.Primitives.DragCompletedEventArgs e) {
			base.SeekBar_PreviewMouseLeftButtonUp(sender, null);
		}

		private void Host_MouseDown(object sender, MouseButtonEventArgs e) {
			if (e.ChangedButton == MouseButton.Middle)
				FullScreen = true;
		}
	}
}
