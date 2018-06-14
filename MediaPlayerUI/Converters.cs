using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace EmergenceGuardian.MediaPlayerUI {
	[ValueConversion(typeof(TimeSpan), typeof(double))]
	public class TimeSpanToDoubleConverter : IValueConverter {
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
			return ((TimeSpan)value).TotalSeconds;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
			return TimeSpan.FromSeconds((double)value);
		}
	}

	[ValueConversion(typeof(bool), typeof(Visibility))]
	public sealed class BoolToVisibilityConverter : IValueConverter {
		public Visibility TrueValue { get; set; }
		public Visibility FalseValue { get; set; }

		public BoolToVisibilityConverter() {
			// set defaults
			TrueValue = Visibility.Visible;
			FalseValue = Visibility.Hidden;
		}

		public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
			if (!(value is bool))
				return null;
			return (bool)value ? TrueValue : FalseValue;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
			if (Equals(value, TrueValue))
				return true;
			if (Equals(value, FalseValue))
				return false;
			return null;
		}
	}

	[ValueConversion(typeof(double), typeof(double))]
	public class InvertDoubleConverter : IValueConverter {
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
			return -(double)value;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
			return -(double)value;
		}
	}
}
