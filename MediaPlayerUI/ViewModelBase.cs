using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace EmergenceGuardian.MediaPlayerUI {
	public class ViewModelBase : INotifyPropertyChanged {
		public event PropertyChangedEventHandler PropertyChanged;

		public void InvokePropertyChanged(string propertyName) {
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}
	}
}
