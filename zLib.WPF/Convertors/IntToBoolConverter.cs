using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace zLib.WPF.Converters {
	public class IntToBoolConverter : IValueConverter {
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
			bool r = false;
			if (value is int) {
				r = (int)value == 1;
			}
			int par;
			if (int.TryParse((string)parameter, out par)){

				if (value is int) {
					r = (int)value == par;
				}
			}
			return r;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
			int? r = new int?();
			if (value is bool) {
				r = ((bool)value) ? 1 : 0;
			}
			int par;
			if (int.TryParse((string)parameter, out par))
				if (value is bool) {
					r = ((bool)value) ? (int?)par : null;
				}
			return r;
		}
	}
}
