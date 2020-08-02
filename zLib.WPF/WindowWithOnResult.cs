//using NativeHelpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace zLib.WPF {
	public class WindowWithOnResult : Window {// : PerMonitorDPIWindow {
		public Action<System.Windows.Forms.DialogResult> OnResult;
		public Action<object> OnReturn;
		public Func<object, bool> IsValid { get; set; }
	}
}
