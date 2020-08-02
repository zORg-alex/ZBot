using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace zLib.WPF.Converters {
	public class ParameterConverter : IValueConverter {
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
			String par = (string)parameter;
			if (par.ToLower() == "anytovisibility") {
				if (value == null) return Visibility.Collapsed;
				else return Visibility.Visible;
			} else if (par.ToLower() == "inttovisibility") {
				if (value == null || (int)value == 0) return Visibility.Collapsed;
				else return Visibility.Visible;
			} else if (par.ToLower() == "anytoautodatagridlength") {
				if (value == null) return new DataGridLength(0);
				else return new DataGridLength(1, DataGridLengthUnitType.Auto);
			} else if (par.ToLower() == "anytostardatagridlength") {
				if (value == null) return new DataGridLength(0);
				else return new DataGridLength(1, DataGridLengthUnitType.Star);
			} else if (par.ToLower() == "anytobool") {
				return value == null ? false : true;
			} else if (par.ToLower() =="isnull") {
				return value == null ? true : false;
			} else if (par.ToLower().Contains("boolinv") || value.GetType() == typeof(bool)) {
				return !(bool)value;
			} else
				throw new NotImplementedException();
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
			String par = (string)parameter;
			if (par.ToLower().Contains("boolinv") || value.GetType() == typeof(bool)) {
				return !(bool)value;
			} else
				throw new NotImplementedException();
		}
	}
}
