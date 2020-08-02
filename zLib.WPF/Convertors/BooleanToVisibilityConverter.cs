using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace zLib.WPF.Converters {
	public class BooleanToVisibilityConverter : IValueConverter {
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
			bool flag = false;
			if (value is bool) {
				flag = (bool)value;
			} else if (value is bool?) {
				bool? nullable = (bool?)value;
				flag = nullable.HasValue ? nullable.Value : false;
			}
			string par = (string)parameter;
			if (par != null)
				if (par.ToLower().Contains("invert"))
					return (!flag ? Visibility.Visible : Visibility.Collapsed);
			return (flag ? Visibility.Visible : Visibility.Collapsed);
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
			Visibility flag = Visibility.Collapsed;
			if (value is Visibility) {
				flag = (Visibility)value;
			} else if (value is Visibility?) {
				Visibility? nullable = (Visibility?)value;
				flag = nullable.HasValue ? nullable.Value : Visibility.Collapsed;
			}
			string par = (string)parameter;
			if (par != null)
				if (par.ToLower().Contains("invert"))
					return (flag != Visibility.Visible? true : false);
			return (flag == Visibility.Visible ? true : false);
		}
	}
}
