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
	[TemplatePart(Name = PlayerControls.PART_SeekBar, Type = typeof(Slider))]
	public class PlayerControls : PlayerControlsBase {
		public const string PART_SeekBar = "PART_SeekBar";
		public Slider SeekBar => GetTemplateChild(PART_SeekBar) as Slider;

		static PlayerControls() {
			DefaultStyleKeyProperty.OverrideMetadata(typeof(PlayerControls), new FrameworkPropertyMetadata(typeof(PlayerControls)));
		}

		public override void OnApplyTemplate() {
			base.OnApplyTemplate();

			if (DesignerProperties.GetIsInDesignMode(this))
				return;

			Slider Bar = SeekBar;
			if (Bar != null) {
				MouseDown += UserControl_MouseDown;
				Bar.AddHandler(Slider.PreviewMouseDownEvent, new MouseButtonEventHandler(base.SeekBar_PreviewMouseLeftButtonDown), true);
				// Thumb doesn't yet exist.
				Bar.Loaded += (s, e) => {
					Thumb SeekBarThumb = GetSliderThumb(Bar);
					if (SeekBarThumb != null) {
						SeekBarThumb.DragStarted += SeekBar_DragStarted;
						SeekBarThumb.DragCompleted += SeekBar_DragCompleted;
					}
				};
			}
		}

		/// <summary>
		/// Prevents the Host from receiving mouse events when clicking on controls bar.
		/// </summary>
		private void UserControl_MouseDown(object sender, MouseButtonEventArgs e) {
			e.Handled = true;
		}

		protected override void OnPlayerHostChanged(DependencyPropertyChangedEventArgs e) {
			if (e.OldValue != null)
				((Control)e.OldValue).MouseDown -= Host_MouseDown;
			if (e.NewValue != null) {
				((Control)e.NewValue).MouseDown += Host_MouseDown;
				((Control)e.NewValue).MouseDoubleClick += PlayerControls_MouseDoubleClick;
			}
		}

		private void PlayerControls_MouseDoubleClick(object sender, MouseButtonEventArgs e) {
			
		}

		/// <summary>
		/// Handles mouse click events for both Host and Fullscreen.
		/// </summary>
		private void Host_MouseDown(object sender, MouseButtonEventArgs e) {
			bool IsFullScreen = sender is FullScreenUI;
			if (IsActionFullScreen(e)) {
				FullScreen = !IsFullScreen; // using !FullScreen can return wrong value when exiting fullscreen
				e.Handled = true;
			} else if (IsActionPause(e)) {
				PlayPauseCommand.Execute(null);
				e.Handled = true;
			}
		}

		private bool IsActionFullScreen(MouseButtonEventArgs e) => IsMouseAction(MouseFullscreen, e);
		private bool IsActionPause(MouseButtonEventArgs e) => IsMouseAction(MousePause, e);

		private bool IsMouseAction(MouseTrigger a, MouseButtonEventArgs e) {
			if (a == MouseTrigger.LeftClick && e.ChangedButton == MouseButton.Left)
				return true;
			if (a == MouseTrigger.MiddleClick && e.ChangedButton == MouseButton.Middle)
				return true;
			if (a == MouseTrigger.RightClick && e.ChangedButton == MouseButton.Right)
				return true;
			else
				return false;
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

		// TitleProperty
		public static DependencyProperty TitleProperty = DependencyProperty.Register("Title", typeof(bool), typeof(PlayerControls));
		public string Title { get => (string)GetValue(TitleProperty); set => SetValue(TitleProperty, value); }

		// MouseFullscreen
		public static DependencyProperty MouseFullscreenProperty = DependencyProperty.Register("MouseFullscreen", typeof(MouseTrigger), typeof(PlayerControls),
			new PropertyMetadata(MouseTrigger.MiddleClick));
		public MouseTrigger MouseFullscreen { get => (MouseTrigger)GetValue(MouseFullscreenProperty); set => SetValue(MouseFullscreenProperty, value); }

		// MousePause
		public static DependencyProperty MousePauseProperty = DependencyProperty.Register("MousePause", typeof(MouseTrigger), typeof(PlayerControls),
			new PropertyMetadata(MouseTrigger.LeftClick));
		public MouseTrigger MousePause { get => (MouseTrigger)GetValue(MousePauseProperty); set => SetValue(MousePauseProperty, value); }

		// IsPlayPauseVisible
		public static DependencyProperty IsPlayPauseVisibleProperty = DependencyProperty.Register("IsPlayPauseVisible", typeof(bool), typeof(PlayerControls),
			new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.AffectsParentArrange));
		public bool IsPlayPauseVisible { get => (bool)GetValue(IsPlayPauseVisibleProperty); set => SetValue(IsPlayPauseVisibleProperty, value); }

		// IsStopVisible
		public static DependencyProperty IsStopVisibleProperty = DependencyProperty.Register("IsStopVisible", typeof(bool), typeof(PlayerControls),
			new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.AffectsParentArrange));
		public bool IsStopVisible { get => (bool)GetValue(IsStopVisibleProperty); set => SetValue(IsStopVisibleProperty, value); }

		// IsLoopVisible
		public static DependencyProperty IsLoopVisibleProperty = DependencyProperty.Register("IsLoopVisible", typeof(bool), typeof(PlayerControls),
			new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.AffectsParentArrange));
		public bool IsLoopVisible { get => (bool)GetValue(IsLoopVisibleProperty); set => SetValue(IsLoopVisibleProperty, value); }

		// IsVolumeVisible
		public static DependencyProperty IsVolumeVisibleProperty = DependencyProperty.Register("IsVolumeVisible", typeof(bool), typeof(PlayerControls),
			new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.AffectsParentArrange));
		public bool IsVolumeVisible { get => (bool)GetValue(IsVolumeVisibleProperty); set => SetValue(IsVolumeVisibleProperty, value); }

		// IsSpeedVisible
		public static DependencyProperty IsSpeedVisibleProperty = DependencyProperty.Register("IsSpeedVisible", typeof(bool), typeof(PlayerControls), 
			new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.AffectsParentArrange));
		public bool IsSpeedVisible { get => (bool)GetValue(IsSpeedVisibleProperty); set => SetValue(IsSpeedVisibleProperty, value); }

		// IsSeekBarVisible
		public static DependencyProperty IsSeekBarVisibleProperty = DependencyProperty.Register("IsSeekBarVisible", typeof(bool), typeof(PlayerControls), 
			new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.AffectsParentArrange));
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
						throw new ArgumentException("PlayerHost must be set to use FullScreen");
					if (value) {
						// Create full screen.
						FullScreenUI = new FullScreenUI();
						FullScreenUI.Closed += FullScreenUI_Closed;
						FullScreenUI.MouseDown += Host_MouseDown;
						// Transfer key bindings.
						InputBindingBehavior.TransferBindingsToWindow(Window.GetWindow(this), FullScreenUI, false);
						// Transfer player.
						TransferElement(PlayerHost.InnerControlParentCache, FullScreenUI.ContentGrid, PlayerHost.InnerControl);
						TransferElement(ParentCache, FullScreenUI.AirspaceGrid, this);
						FullScreenUI.Airspace.VerticalOffset = -this.ActualHeight;
						// Show.
						FullScreenUI.ShowDialog();
					} else if (FullScreenUI != null) {
						// Transfer player back.
						TransferElement(FullScreenUI.ContentGrid, PlayerHost.InnerControlParentCache, PlayerHost.InnerControl);
						TransferElement(FullScreenUI.AirspaceGrid, ParentCache, this);
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

		public void TransferElement(Panel src, Panel dst, FrameworkElement element) {
			src.Children.Remove(element);
			dst.Children.Add(element);
		}

		private void FullScreenUI_Closed(object sender, EventArgs e) {
			FullScreen = false;
		}

		#endregion
	}
}
