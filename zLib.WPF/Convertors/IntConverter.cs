using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace zLib.WPF.Converters {
	public class IntConverter : IValueConverter {
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
			string par = parameter as string;
			int r = 0;
			if (value is int) {
				r = (int)value;
				if (par.ToLower().Contains("inv") || par.ToLower().Substring(0,1) == "-") r = -r;
				if (par.ToLower().Contains("/")) r = r / int.Parse(par.Substring(par.IndexOf("/"), par.Length - par.IndexOf("/")));
			}
			return r;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
			string par = parameter as string;
			int r = 0;
			if (value is int) {
				r = (int)value;
				if (par.ToLower().Contains("inv")) r = -r;
				if (par.ToLower().Contains("/")) r = r * int.Parse(par.Substring(par.IndexOf("/"), par.Length - par.IndexOf("/")));
			}
			return r;
		}
	}
}
