using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace EmergenceGuardian.MediaPlayerUI {
	public class PlayerBase : Control, INotifyPropertyChanged {
		public event PropertyChangedEventHandler PropertyChanged;
		public void InvokePropertyChanged(string propertyName) {
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}

		public virtual TimeSpan Position { get; set; }
		public virtual TimeSpan Duration { get; }
		public virtual bool Paused { get; set; }
		public virtual int Volume { get; set; }
		public virtual float SpeedFloat { get; set; } = 1;
		public virtual int SpeedInt { get; set; }
		public virtual bool Loop { get; set; }
		public virtual FrameworkElement InnerControl { get; }

		public virtual void Load(string source) { }
		public virtual void Stop() { }
		public virtual void Restart() { }

		public event EventHandler OnMediaLoaded;
		public event EventHandler OnMediaUnloaded;


		protected void PositionChanged() {
			Dispatcher.Invoke(() => {
				InvokePropertyChanged("Position");
			});
		}

		protected void MediaLoaded() {
			Dispatcher.Invoke(() => {
				OnMediaLoaded?.Invoke(this, new EventArgs());
			});
		}

		protected void MediaUnloaded() {
			Dispatcher.Invoke(() => {
				OnMediaUnloaded?.Invoke(this, new EventArgs());
			});
		}

		private string title;
		public string Title {
			get => title;
			set {
				title = value;
				InvokePropertyChanged("Title");
			}
		}

		protected int speedInt;
		protected float speedFloat = 1;

		public float GetSpeed() {
			if (SpeedInt == 0)
				return speedFloat;
			else {
				float Factor = speedInt / 8f;
				return Factor < 0 ? 1 / (1 - Factor) : 1 * (1 + Factor);
			}
		}

		private Panel innerControlParentCache;
		/// <summary>
		/// Returns the container of InnerControl the first time it is called and maintain reference to that container.
		/// </summary>
		public Panel InnerControlParentCache {
			get {
				if (innerControlParentCache == null) {
					if (InnerControl.Parent is Panel)
						innerControlParentCache = InnerControl.Parent as Panel;
					else
						throw new InvalidCastException(string.Format("PlayerBase must be within a Panel or Grid control. Container is of type '{0}'.", InnerControl?.Parent?.GetType()));
				}
				if (innerControlParentCache == null)
					throw new ArgumentNullException("ParentCache returned null.");
				return innerControlParentCache;
			}
		}
	}
}
