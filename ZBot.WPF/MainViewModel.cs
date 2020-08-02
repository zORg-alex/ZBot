using System;
using System.Collections.Generic;
using System.Text;
using zLib;

namespace ZBot.WPF {
	public class MainViewModel : Notifiable {
		public MainViewModel(DateBot.Bot dateBot) {
			DateBot = dateBot;
		}

		DateBot.Bot DateBot { get; set; }

		public StringBuilder Log { get; set; } = new StringBuilder();
	}
}
