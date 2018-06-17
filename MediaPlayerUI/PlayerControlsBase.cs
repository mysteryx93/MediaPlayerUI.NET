using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace EmergenceGuardian.MediaPlayerUI {
	public class PlayerControlsBase : Control, INotifyPropertyChanged {

		#region Declarations / Constructor

		public event PropertyChangedEventHandler PropertyChanged;
		public void InvokePropertyChanged(string propertyName) {
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}

		private bool isSeekBarButtonDown = false;
		// Restart won't be triggered after Stop while this timer is running.
		private bool isStopping = false;
		private Timer stopTimer = new Timer(1000);

		public PlayerControlsBase() {
			stopTimer.AutoReset = false;
			stopTimer.Elapsed += (o, e) => isStopping = false;
		}

		#endregion


		#region Commands

		private ICommand playPauseCommand;
		public ICommand PlayPauseCommand => CommandHelper.InitCommand(ref playPauseCommand, PlayPause, CanPlayPause);

		private ICommand stopCommand;
		public ICommand StopCommand => CommandHelper.InitCommand(ref stopCommand, Stop, CanStop);

		private ICommand seekForwardCommand;
		public ICommand SeekForwardCommand => CommandHelper.InitCommand(ref seekForwardCommand, () => Seek(1), CanSeek);

		private ICommand seekForwardLargeCommand;
		public ICommand SeekForwardLargeCommand => CommandHelper.InitCommand(ref seekForwardLargeCommand, () => Seek(10), CanSeek);

		private ICommand seekBackCommand;
		public ICommand SeekBackCommand => CommandHelper.InitCommand(ref seekBackCommand, () => Seek(-1), CanSeek);

		private ICommand seekBackLargeCommand;
		public ICommand SeekBackLargeCommand => CommandHelper.InitCommand(ref seekBackLargeCommand, () => Seek(-10), CanSeek);

		private ICommand volumeUpCommand;
		public ICommand VolumeUpCommand => CommandHelper.InitCommand(ref volumeUpCommand, () => ChangeVolume(5), CanChangeVolume);

		private ICommand volumeDownCommand;
		public ICommand VolumeDownCommand => CommandHelper.InitCommand(ref volumeDownCommand, () => ChangeVolume(-5), CanChangeVolume);

		private bool CanPlayPause() => IsMediaLoaded;
		private void PlayPause() {
			IsPlaying = !IsPlaying;
			PlayerHost.Paused = !IsPlaying;
		}

		private bool CanStop() => IsMediaLoaded;
		private void Stop() {
			isStopping = true;
			stopTimer.Stop();
			stopTimer.Start();
			PlayerHost.Stop();
		}

		private bool CanSeek() => IsMediaLoaded;
		private void Seek(int seconds) {
			TimeSpan NewPos = PlayerHost.Position.Add(TimeSpan.FromSeconds(seconds));
			if (NewPos < TimeSpan.Zero)
				NewPos = TimeSpan.Zero;
			else if (NewPos > PlayerHost.Duration)
				NewPos = Duration;
			if (NewPos != PlayerHost.Position) {
				PositionBar = NewPos;
				PlayerHost.Position = NewPos;
			}
		}

		private bool CanChangeVolume() => true;
		private void ChangeVolume(int value) {
			PlayerHost.Volume = PlayerHost.Volume + value;
		}

		#endregion


		#region Dependency Properties

		// PlayerHost
		public static DependencyProperty PlayerHostProperty = DependencyProperty.Register("PlayerHost", typeof(PlayerBase), typeof(PlayerControlsBase),
			new PropertyMetadata(null, OnPlayerHostChanged));
		public PlayerBase PlayerHost { get => (PlayerBase)GetValue(PlayerHostProperty); set => SetValue(PlayerHostProperty, value); }

		// PositionBar
		public static DependencyProperty PositionBarProperty = DependencyProperty.Register("PositionBar", typeof(TimeSpan), typeof(PlayerControlsBase),
			new PropertyMetadata(TimeSpan.Zero, null, CoercePositionBar));
		public TimeSpan PositionBar { get => (TimeSpan)GetValue(PositionBarProperty); set => SetValue(PositionBarProperty, value); }
		private static object CoercePositionBar(DependencyObject d, object value) {
			PlayerControlsBase P = d as PlayerControlsBase;
			TimeSpan Pos = (TimeSpan)value;
			if (P.PlayerHost == null)
				return DependencyProperty.UnsetValue;

			if (Pos < TimeSpan.Zero)
				Pos = TimeSpan.Zero;
			if (Pos > P.PlayerHost.Duration)
				Pos = P.PlayerHost.Duration;
			return Pos;
		}

		// Duration
		public static DependencyProperty DurationProperty = DependencyProperty.Register("Duration", typeof(TimeSpan), typeof(PlayerControlsBase),
			new PropertyMetadata(TimeSpan.FromSeconds(1), OnDurationChanged), IsDurationValid);
		public TimeSpan Duration { get => (TimeSpan)GetValue(DurationProperty); set => SetValue(DurationProperty, value); }
		private static bool IsDurationValid(object value) => ((TimeSpan)value >= TimeSpan.Zero);
		private static void OnDurationChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) => (d as PlayerControlsBase).InvokePropertyChanged("PositionText");

		// PositionDisplay
		public static DependencyProperty PositionDisplayProperty = DependencyProperty.Register("PositionDisplay", typeof(TimeDisplayStyles), typeof(PlayerControlsBase),
			new PropertyMetadata(TimeDisplayStyles.Standard));
		public TimeDisplayStyles PositionDisplay { get => (TimeDisplayStyles)GetValue(PositionDisplayProperty); set => SetValue(PositionDisplayProperty, value); }

		// IsMediaLoaded
		public static DependencyPropertyKey IsMediaLoadedPropertyKey = DependencyProperty.RegisterReadOnly("IsMediaLoaded", typeof(bool), typeof(PlayerControlsBase),
			new PropertyMetadata(false, OnIsMediaLoadedChanged));
		public static DependencyProperty IsMediaLoadedProperty = IsMediaLoadedPropertyKey.DependencyProperty;
		public bool IsMediaLoaded { get => (bool)GetValue(IsMediaLoadedProperty); private set => SetValue(IsMediaLoadedPropertyKey, value); }
		private static void OnIsMediaLoadedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
			PlayerControlsBase P = d as PlayerControlsBase;
			P.IsPlaying = (bool)e.NewValue;
			CommandManager.InvalidateRequerySuggested();
		}

		// IsPlaying
		public static DependencyProperty IsPlayingProperty = DependencyProperty.Register("IsPlaying", typeof(bool), typeof(PlayerControlsBase),
			new PropertyMetadata(false, OnIsPlayingChanged));
		public bool IsPlaying { get => (bool)GetValue(IsPlayingProperty); set => SetValue(IsPlayingProperty, value); }
		private static void OnIsPlayingChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
			PlayerControlsBase P = d as PlayerControlsBase;
			P.InvokePropertyChanged("PlayPauseButtonIcon");
			P.PlayerHost.Paused = !P.IsPlaying;
		}


		public string PositionText {
			get {
				if (PlayerHost != null && IsMediaLoaded && PositionDisplay != TimeDisplayStyles.None)
					return string.Format("{0} / {1}",
						FormatTime(PlayerHost.Position),
						FormatTime(Duration));
				else
					return "";
			}
		}

		#endregion


		#region Events

		private static void OnPlayerHostChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
			PlayerControlsBase P = d as PlayerControlsBase;
			if (e.OldValue != null) {
				((PlayerBase)e.OldValue).PropertyChanged -= P.Player_PropertyChanged;
				((PlayerBase)e.OldValue).OnMediaLoaded -= P.Player_MediaLoaded;
				((PlayerBase)e.OldValue).OnMediaUnloaded -= P.Player_MediaUnloaded;
			}
			if (e.NewValue != null) {
				((PlayerBase)e.NewValue).PropertyChanged += P.Player_PropertyChanged;
				((PlayerBase)e.NewValue).OnMediaLoaded += P.Player_MediaLoaded;
				((PlayerBase)e.NewValue).OnMediaUnloaded += P.Player_MediaUnloaded;
			}
			P.OnPlayerHostChanged(e);
		}
		// Allow derived class to bind to new host.
		protected virtual void OnPlayerHostChanged(DependencyPropertyChangedEventArgs e) { }

		private void Player_PropertyChanged(object sender, PropertyChangedEventArgs e) {
			if (e.PropertyName == "Position") {
				InvokePropertyChanged("PositionText");
				if (!isSeekBarButtonDown)
					PositionBar = PlayerHost.Position;
			}
		}

		private void Player_MediaLoaded(object sender, EventArgs e) {
			PlayerHost.Position = TimeSpan.Zero;
			Duration = PlayerHost.Duration;
			IsMediaLoaded = true;
		}

		private void Player_MediaUnloaded(object sender, EventArgs e) {
			PositionBar = TimeSpan.Zero;
			Duration = TimeSpan.FromSeconds(1);
			IsMediaLoaded = false;
			if (PlayerHost.Loop && !isStopping)
				PlayerHost.Restart();
		}

		public void SeekBar_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e) {
			// Only process event if click is not on thumb.
			Thumb Obj = GetSliderThumb(sender as Slider);
			Point Pos = e.GetPosition(Obj);
			if (Pos.X < 0 || Pos.Y < 0 || Pos.X > Obj.ActualWidth || Pos.Y > Obj.ActualHeight) {
				// Immediate seek when clicking elsewhere.
				isSeekBarButtonDown = true;
				PlayerHost.Position = PositionBar;
				isSeekBarButtonDown = false;
			}
		}

		public void SeekBar_DragStarted(object sender, DragStartedEventArgs e) {
			isSeekBarButtonDown = true;
		}

		public void SeekBar_DragCompleted(object sender, DragCompletedEventArgs e) {
			PlayerHost.Position = PositionBar;
			isSeekBarButtonDown = false;
		}

		#endregion


		#region Helper Functions

		private string FormatTime(TimeSpan t) {
			if (PositionDisplay == TimeDisplayStyles.Standard) {
				if (t.TotalHours >= 1)
					return t.ToString("h\\:mm\\:ss");
				else
					return t.ToString("m\\:ss");
			} else if (PositionDisplay == TimeDisplayStyles.Seconds) {
				return t.TotalSeconds.ToString();
			} else
				return null;
		}

		public Thumb GetSliderThumb(Slider obj) {
			var track = obj.Template.FindName("PART_Track", obj) as Track;
			return track?.Thumb;
		}

		#endregion

	}
}
