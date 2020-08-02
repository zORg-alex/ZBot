using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace zLib.WPF.Converters {
	[ValueConversion(typeof(Double), typeof(GridLength))]
	public class DoubleToGridLengthConverter : IValueConverter {
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
			// check whether a value is given
			if (value != null) {
				return new GridLength((Double)value);
			} else {
				throw new ValueUnavailableException();
			}
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
			// check whether a value is given
			if (value != null) {
				//Debug.WriteLine("value is: " + value + " type: " + value.GetType());
				return (Double)((GridLength)(value)).Value;
			} else {
				throw new ValueUnavailableException();
			}
		}
	}
}
