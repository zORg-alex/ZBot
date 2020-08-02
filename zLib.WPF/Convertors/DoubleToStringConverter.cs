using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Data;

namespace zLib.WPF.Converters {
	public class DoubleToStringConverter : IValueConverter {
		/// <summary>
		/// Double To String
		/// </summary>
		/// <param name="value"></param>
		/// <param name="targetType"></param>
		/// <param name="parameter"></param>
		/// <param name="culture"></param>
		/// <returns></returns>
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
			if (value is decimal d) {
				if (parameter is string par)
					return d.ToString(par);
				return d.ToString();
			}
			return "";
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
			decimal r = 0;
			if (value is string str) {
				string v = Regex.Replace(str, @"[^-0-9,\.]", string.Empty).Replace(',', '.');
				if (v.Split('.').Length > 1)
					v = v.Substring(0, v.IndexOf('.') + 1) + v.Substring(v.IndexOf('.') + 1).Replace(".", string.Empty);
				decimal.TryParse(v, out r);
			}
			return r;
		}
	}
}
