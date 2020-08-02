using System;
using System.Windows.Input;

namespace zLib.WPF {
	/// <summary>
	/// A generic ICommand class. It lets you to instantiate a command and apply a Function, CanExecute and a name in ctor. 
	/// With <see cref="UVMCommandHost"/> and <see cref="UVMCommandHostToUVMCommandConverter"/> and <see cref="UVMCommandListToUVMCommandConverter"/>
	/// it lets you to bind multiple commands with ease.
	/// </summary>
	/// <remarks>
	/// Here is how you use it:
	/// 
	/// //Declare in a class, that should have some commands on all of it's objects
	/// class ImportReport {
	///		private static UVMCommandHost _commands;
	///		public static UVMCommandHost Commands {
	///			get {
	///				if (_commands == null) _commands = new UVMCommandHost();
	///				return _commands;
	///			}
	///			set { _commands = value; }
	///		}
	///	}
	///	
	///	//Then in ViewModel Initialize them
	///	IQuestAccess.Commands.Add(new UVMCommand("comExpandInUserView", (p) => {
	///		//Action here
	///	}));
	///	
	///	//And finally in xaml declare a Converter and bind a command using a converter
	///		<PTR.PTRLib:UVMCommandHostToUVMCommandConverter x:Key="UVMCommandHostToUVMCommandConverter"/>
	///		<Button Command="{Binding Commands, ConverterParameter=commandName, Converter={StaticResource UVMCommandHostToUVMCommandConverter}}"
	///				CommandParameter="{Binding DataContext, RelativeSource={RelativeSource Self}}">
	///				
	/// </remarks>
	public class UVMCommand : Notifiable, ICommand {

		public string Name = "UVMCommand";
		public Action<object> Exec;
		public Func<object, bool> CanExec;
		bool staticCanExecute = true;
		private bool _finished = true;
		public bool Finished { get { return _finished; } set {
				_finished = value;
				NotifyStateAction?.Invoke(!value);
				RaisePropertyChanged("Finished");
			}
		}
		public Action<bool> NotifyStateAction;

		private UVMCommand() {

		}

		/// <summary>
		/// Creates a <see cref="UVMCommand"/> with a conditional CanExecute.
		/// </summary>
		public UVMCommand(Action<object> Execute, Func<object, bool> CanExecute) {
			this.Exec = Execute;
			this.CanExec = CanExecute;
		}

		public static UVMCommand AsyncCommand(Action<object> Execute, Func<object, bool> CanExecute, Action<bool> NotifyState = null) {
			var c = new UVMCommand();
			c.NotifyStateAction = NotifyState;
			c.Exec = p => {
				c.Finished = false;
				Execute.BeginInvoke(null, pp => c.Finished = true, null);
			};
			c.CanExec = CanExecute;
			return c;
		}

		/// <summary>
		/// Creates a <see cref="UVMCommand"/> with a static CanExecute. CanExecute should be true always, or it will be a useless command.
		/// </summary>
		/// <param name="Execute"></param>
		/// <param name="CanExecute"></param>
		public UVMCommand(Action<object> Execute, bool CanExecute) {
			this.Exec = Execute;
			staticCanExecute = CanExecute;
			this.CanExec = StaticCanExecute;
		}

		public static UVMCommand AsyncCommand(Action<object> Execute, bool CanExecute, Action<bool> NotifyState = null) {
			var c = new UVMCommand();
			c.NotifyStateAction = NotifyState;
			c.Exec = p => {
				c.Finished = false;
				Execute.BeginInvoke(null, pp => c.Finished = true, null);
			};
			c.staticCanExecute = CanExecute;
			c.CanExec = c.StaticCanExecute;
			return c;
		}

		/// <summary>
		/// Creates a <see cref="UVMCommand"/> with always true CanExecute.
		/// </summary>
		/// <param name="Execute"></param>
		/// <param name="CanExecute"></param>
		public UVMCommand(Action<object> Execute) {
			this.Exec = Execute;
			staticCanExecute = true;
			this.CanExec = StaticCanExecute;
		}

		public static UVMCommand AsyncCommand(Action<object> Execute, Action<bool> NotifyState = null) {
			var c = new UVMCommand();
			c.NotifyStateAction = NotifyState;
			c.Exec = p => {
				c.Finished = false;
				var t = Execute.BeginInvoke(p, pp => c.Finished = true, null);
			};
			c.CanExec = c.StaticCanExecute;
			return c;
		}

		/// <summary>
		/// Creates a <see cref="UVMCommand"/> with a name and conditional CanExecute.
		/// </summary>
		public UVMCommand(string Name, Action<object> Execute, Func<object, bool> CanExecute) {
			this.Name = Name;
			this.Exec = Execute;
			this.CanExec = CanExecute;
		}

		/// <summary>
		/// Creates a <see cref="UVMCommand"/> with a name and a static CanExecute. CanExecute should be true always, or it will be a useless command.
		/// </summary>
		public UVMCommand(string Name, Action<object> Execute, bool CanExecute) {
			this.Name = Name;
			this.Exec = Execute;
			staticCanExecute = CanExecute;
			this.CanExec = StaticCanExecute;
		}

		/// <summary>
		/// Creates a <see cref="UVMCommand"/> with a name and always true CanExecute.
		/// </summary>
		/// <param name="Execute"></param>
		/// <param name="CanExecute"></param>
		public UVMCommand(string Name, Action<object> Execute) {
			this.Name = Name;
			this.Exec = Execute;
			this.CanExec = StaticCanExecute;
		}

		public event EventHandler CanExecuteChanged {
			add { CommandManager.RequerySuggested += value; }
			remove { CommandManager.RequerySuggested -= value; }
		}

		public bool CanExecute(object parameter) {
			return CanExec(parameter);
		}

		public void Execute(object parameter) {
			Exec(parameter);
		}

		bool StaticCanExecute(object parameter) {
			return staticCanExecute;
		}
	}

	//Probaly left unattended while developing UVMCommand thing contraption
	//public class UVMCommandListToUVMCommandConverter : IValueConverter {
	//	public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
	//		if (value as List<UVMCommand> == null) return null;
	//		return (value as List<UVMCommand>).Find(c => c.Name == parameter as string);
	//	}

	//	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
	//		throw new NotImplementedException();
	//	}
	//}
}
