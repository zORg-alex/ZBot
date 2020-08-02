using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace CVIAS.View.Converters {

	public class TextToColorConverter : IValueConverter {
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
			if (value == null) return null;
			if (value is Color) {
				return ((Color)value).ToString();
			} else if (value is Color?) {
				return ((Color?)value).HasValue ? ((Color?)value).Value.ToString() : null;
			} else if (value is string) {
				return (Color)ColorConverter.ConvertFromString((string)value);
			}
			return null;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
			return Convert(value, targetType, parameter, culture);
		}
	}
}
