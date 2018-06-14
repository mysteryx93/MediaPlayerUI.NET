using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace EmergenceGuardian.MediaPlayerUI {
	public class MediaPlayerUIViewModel : UserControl, INotifyPropertyChanged {
		public event PropertyChangedEventHandler PropertyChanged;

		public void InvokePropertyChanged(string propertyName) {
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}

		private bool isLoaded = false;
		private bool isPlaying = false;
		private TimeSpan position;
		private TimeSpan positionBar;
		private TimeSpan duration;
		private int volume;
		private bool loop;
		private int speedInt = 0;
		private float speed = 1;
		private TimeDisplayStyles timeDisplayStyle = TimeDisplayStyles.Standard;
		private bool keyboardShortcuts = true;

		public event EventHandler OnPlay;
		public event EventHandler OnPause;
		public event EventHandler OnStop;
		public event EventHandler OnLoop;
		public event EventHandler OnToggleLoop;
		public event EventHandler OnSeek;
		public event EventHandler OnVolumeChanged;
		public event EventHandler OnSpeedChanged;

		private bool isSeekBarButtonDown = false;
		// Loop won't be triggered after Stop while this timer is running.
		private bool isStopping = false;
		private Timer stopTimer = new Timer(1000);

		public MediaPlayerUIViewModel() {
			stopTimer.AutoReset = false;
			stopTimer.Elapsed += (o, e) => isStopping = false;
		}

		private ICommand playPauseCommand;
		public ICommand PlayPauseCommand => InitCommand(ref playPauseCommand, PlayPause, CanPlayPause);

		private ICommand stopCommand;
		public ICommand StopCommand => this.InitCommand(ref stopCommand, Stop, CanStop);

		private ICommand keyPlayPauseCommand;
		public ICommand KeyPlayPauseCommand => InitCommand(ref keyPlayPauseCommand, PlayPause, () => KeyboardShortcuts && CanPlayPause());

		private ICommand keyStopCommand;
		public ICommand KeyStopCommand => this.InitCommand(ref keyStopCommand, Stop, () => KeyboardShortcuts && CanStop());

		private ICommand seekForwardCommand;
		public ICommand SeekForwardCommand => this.InitCommand(ref seekForwardCommand, () => Seek(1), CanSeek);

		private ICommand seekForward2Command;
		public ICommand SeekForward2Command => this.InitCommand(ref seekForward2Command, () => Seek(10), CanSeek);

		private ICommand seekBackCommand;
		public ICommand SeekBackCommand => this.InitCommand(ref seekBackCommand, () => Seek(-1), CanSeek);

		private ICommand seekBack2Command;
		public ICommand SeekBack2Command => this.InitCommand(ref seekBack2Command, () => Seek(-10), CanSeek);

		private ICommand volumeUpCommand;
		public ICommand VolumeUpCommand => this.InitCommand(ref volumeUpCommand, () => ChangeVolume(5), () => KeyboardShortcuts);

		private ICommand volumeDownCommand;
		public ICommand VolumeDownCommand => this.InitCommand(ref volumeDownCommand, () => ChangeVolume(-5), () => KeyboardShortcuts);

		protected ICommand InitCommand(ref ICommand cmd, Action execute, Func<bool> canExecute) {
			if (cmd == null)
				cmd = new DelegateCommand(execute, canExecute);
			return cmd;
		}

		public TimeSpan Position {
			get => position;
			set {
				if (value < TimeSpan.Zero)
					value = TimeSpan.Zero;
				if (value > Duration)
					value = Duration;
				position = value;
				InvokePropertyChanged("Position");
				InvokePropertyChanged("PositionText");
				if (!isSeekBarButtonDown) {
					PositionBar = value;
				}
			}
		}

		public TimeSpan PositionBar {
			get => positionBar;
			set {
				if (value < TimeSpan.Zero)
					value = TimeSpan.Zero;
				if (value > Duration)
					value = Duration;
				positionBar = value;
				InvokePropertyChanged("PositionBar");
			}
		}

		public TimeSpan Duration {
			get => duration;
			private set {
				if (value < TimeSpan.Zero)
					throw new ArgumentOutOfRangeException("Duration cannot be negative.");
				duration = value;
				InvokePropertyChanged("Duration");
				InvokePropertyChanged("PositionText");
			}
		}

		public int Volume {
			get => volume;
			set {
				if (value < 0)
					value = 0;
				if (value > 100)
					value = 100;
				volume = value;
				InvokePropertyChanged("Volume");
				OnVolumeChanged?.Invoke(this, new EventArgs());
			}
		}

		public bool Loop {
			get => loop;
			set {
				loop = value;
				InvokePropertyChanged("Loop");
				OnToggleLoop(this, new EventArgs());
			}
		}

		public int SpeedInt {
			get => speedInt;
			set {
				speedInt = value;
				InvokePropertyChanged("SpeedInt");
				InvokePropertyChanged("Speed");
				OnSpeedChanged?.Invoke(this, new EventArgs());
			}
		}

		public float Speed {
			get {
				if (speed != 1)
					return speed;
				else {
					float Factor = speedInt / 8f;
					return Factor < 0 ? 1 / (1 - Factor) : 1 * (1 + Factor);
				}
			}
			set {
				if (speed <= 0)
					throw new ArgumentOutOfRangeException("Speed must be above 0.");
				speed = value;
				InvokePropertyChanged("Speed");
				OnSpeedChanged?.Invoke(this, new EventArgs());
			}
		}

		public TimeDisplayStyles TimeDisplayStyle {
			get => timeDisplayStyle;
			set {
				timeDisplayStyle = value;
				InvokePropertyChanged("TimeDisplayStyle");
			}
		}

		public bool KeyboardShortcuts {
			get => keyboardShortcuts;
			set {
				keyboardShortcuts = value;
				InvokePropertyChanged("KeyboardShortcuts");
			}
		}

		private void PlayPause() {
			IsPlaying = !IsPlaying;
			if (IsPlaying)
				OnPlay?.Invoke(this, new EventArgs());
			else
				OnPause?.Invoke(this, new EventArgs());
		}

		private bool CanPlayPause() => IsMediaLoaded;

		private void Stop() {
			isStopping = true;
			stopTimer.Stop();
			stopTimer.Start();
			OnStop?.Invoke(this, new EventArgs());
		}

		private bool CanStop() => IsMediaLoaded;

		private void Seek(int seconds) {
			TimeSpan NewPos = position.Add(TimeSpan.FromSeconds(seconds));
			if (NewPos < TimeSpan.Zero)
				NewPos = TimeSpan.Zero;
			else if (NewPos > Duration)
				NewPos = Duration;
			if (NewPos != Position) {
				PositionBar = NewPos;
				OnSeek?.Invoke(this, new EventArgs());
			}
		}

		private void ChangeVolume(int value) {
			Volume = Volume + value;
		}

		private bool CanSeek() => KeyboardShortcuts && IsMediaLoaded;

		public bool IsMediaLoaded {
			get => isLoaded;
			private set {
				if (value != isLoaded) {
					isLoaded = value;
					InvokePropertyChanged("IsMediaLoaded");
					IsPlaying = value;
					Application.Current.Dispatcher.Invoke(() => {
						CommandManager.InvalidateRequerySuggested();
					});
				}
			}
		}

		public void MediaLoaded(TimeSpan duration) {
			Position = TimeSpan.Zero;
			Duration = duration;
			IsMediaLoaded = true;
		}

		public void MediaUnloaded() {
			Position = TimeSpan.Zero;
			Duration = TimeSpan.FromSeconds(1);
			IsMediaLoaded = false;
			if (loop && !isStopping)
				OnLoop?.Invoke(this, new EventArgs());
		}

		public bool IsPlaying {
			get => isPlaying;
			set {
				if (value != isPlaying) {
					isPlaying = value;
					InvokePropertyChanged("IsPlaying");
					InvokePropertyChanged("PlayPauseButtonIcon");
					if (isPlaying)
						OnPlay?.Invoke(this, new EventArgs());
					else
						OnPause?.Invoke(this, new EventArgs());
				}
			}
		}

		public string PositionText {
			get {
				if (Duration > TimeSpan.Zero)
					return string.Format("{0} / {1}",
						FormatTime(Position),
						FormatTime(Duration));
				else
					return "";
			}
		}

		private string FormatTime(TimeSpan t) {
			if (TimeDisplayStyle == TimeDisplayStyles.Standard) {
				if (t.TotalHours >= 1)
					return t.ToString("h\\:mm\\:ss");
				else
					return t.ToString("m\\:ss");
			} else if (TimeDisplayStyle == TimeDisplayStyles.Seconds) {
				return t.TotalSeconds.ToString();
			} else
				return null;
		}

		public ImageSource PlayPauseButtonIcon => IsPlaying ? PauseButtonIcon : PlayButtonIcon;

		private ImageSource playButtonIcon;
		public ImageSource PlayButtonIcon {
			get {
				if (playButtonIcon == null)
					playButtonIcon = new BitmapImage(new Uri("pack://application:,,,/MediaPlayerUI;component/Icons/play.png"));
				return playButtonIcon;
			}
		}

		private ImageSource pauseButtonIcon;
		public ImageSource PauseButtonIcon {
			get {
				if (pauseButtonIcon == null)
					pauseButtonIcon = new BitmapImage(new Uri("pack://application:,,,/MediaPlayerUI;component/Icons/pause.png"));
				return pauseButtonIcon;
			}
		}

		private ImageSource stopButtonIcon;
		public ImageSource StopButtonIcon {
			get {
				if (stopButtonIcon == null)
					stopButtonIcon = new BitmapImage(new Uri("pack://application:,,,/MediaPlayerUI;component/Icons/stop.png"));
				return stopButtonIcon;
			}
		}

		private ImageSource loopButtonIcon;
		public ImageSource LoopButtonIcon {
			get {
				if (loopButtonIcon == null)
					loopButtonIcon = new BitmapImage(new Uri("pack://application:,,,/MediaPlayerUI;component/Icons/loop.png"));
				return loopButtonIcon;
			}
		}

		public void SeekBar_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e) {
			isSeekBarButtonDown = true;
		}

		public void SeekBar_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e) {
			// Move the video to the new selected postion.
			OnSeek?.Invoke(this, new EventArgs());
			isSeekBarButtonDown = false;
		}
	}
}
