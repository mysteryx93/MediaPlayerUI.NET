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
    /// <summary>
    /// Converts a TimeSpan into seconds, and optionally adds specified amount of seconds.
    /// </summary>
    [ValueConversion(typeof(TimeSpan), typeof(double))]
    public class TimeSpanToDoubleConverter : IValueConverter {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            double ValueAdd = System.Convert.ToDouble(parameter);
            return ((TimeSpan)value).TotalSeconds + ValueAdd;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            double ValueAdd = System.Convert.ToDouble(parameter);
            return TimeSpan.FromSeconds((double)value - ValueAdd);
        }
    }

    //[ValueConversion(typeof(double), typeof(double))]
    //public class AddConverter : IValueConverter {
    //    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
    //        if (parameter != null)
    //            return (double)value + (double)parameter;
    //        else
    //            return (double)value;
    //    }

    //    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
    //        if (parameter != null)
    //            return (double)value - (double)parameter;
    //        else
    //            return (double)value;
    //    }
    //}
}
