using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Windows;
using DateBot;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using Newtonsoft.Json;

namespace ZBot.WPF {
	/// <summary>
	/// Interaction logic for App.xaml
	/// </summary>
	public partial class App : Application {
		public DateBot.Bot DateBot_ { get; private set; }

		public MainViewModel MVM { get; private set; }

		public App() {
			DateBot_ = new DateBot.Bot();

			MVM = new MainViewModel(DateBot_);
			DateBot_.RunAsync().ContinueWith(t=> {
				DateBot.Bot.Instance.LogMessageAction = (s, e) => MVM.Log.AppendLine(e.Message);
			});
			MainWindow = new MainWindow();
			MainWindow.DataContext = MVM;
			MainWindow.Show();
		}

		protected override void OnExit(ExitEventArgs e) {
			base.OnExit(e);
		}
	}
}
