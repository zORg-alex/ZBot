using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace zLib.WPF.Converters {
	public class XmlStringConverter : IValueConverter {
		public object Convert(
			object value,
			Type targetType,
			object parameter,
			CultureInfo culture) {
			string valueAsString = value as string;
			if (string.IsNullOrEmpty(valueAsString)) {
				return value;
			}

			valueAsString = valueAsString.Replace("\\r", "\r");
			valueAsString = valueAsString.Replace("\\n", "\n");
			return valueAsString;
		}

		public object ConvertBack(
			object value,
			Type targetType,
			object parameter,
			CultureInfo culture) {
			throw new NotImplementedException();
		}
	}
}
