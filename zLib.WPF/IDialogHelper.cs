using System;

namespace zLib.WPF {
	public interface IDialogHelper {
		/// <summary>
		/// Filter string should contain a description of the filter, 
		/// followed by a vertical bar and the filter pattern. 
		/// Must also separate multiple filter description and 
		/// pattern pairs by a vertical bar. Must separate multiple 
		/// extensions in a filter pattern with a semicolon. Example: 
		/// "Image files (*.bmp, *.jpg)|*.bmp;*.jpg|All files (*.*)|*.*"
		/// </summary>
		/// <param name="Type"></param>
		/// <param name="Filter"></param>
		/// <param name="Path"></param>
		/// <returns></returns>
		string OpenDialog(string Type, string Filter, string Path);
		void OpenWindow(string Name, string OwnerName, Action<System.Windows.Forms.DialogResult> OnResult, string Title = "", string Text = "", Func<object, bool> Validator = null);
		void OpenWindowWithObject(string Name, string OwnerName, Action<System.Windows.Forms.DialogResult> OnResult, object Context, Func<object, bool> Validator = null);
		void OpenWindowWithReturn(string Name, object DataContext, string OwnerName, Action<System.Windows.Forms.DialogResult> OnResult, Action<object> OnReturn, string Title = "", string Text = "", Func<object, bool> Validator = null);
		//void OpenSelfConstructingWindow(AutoWindowDataContext DataContext, bool AsDialog = false);
	}
}