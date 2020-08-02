using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows.Data;

namespace zLib.WPF {
	public class UVMCommandHost {// : ICommand {
		List<UVMCommand> list = new List<UVMCommand>();

		public void Add(UVMCommand Command) {
			list.Add(Command);
		}

		public UVMCommand Get(string Name) {
			var c = list.Find(m => m.Name == Name);
			return c;
		}
	}

	public class UVMCommandHostToUVMCommandConverter : IValueConverter {
		public object Convert(object value,Type targetType, object parameter, CultureInfo culture) {
			if (value as UVMCommandHost == null) return null;
			return (value as UVMCommandHost).Get(parameter as string);
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
			throw new NotImplementedException();
		}
	}
}
