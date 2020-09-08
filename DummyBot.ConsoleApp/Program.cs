using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace DummyBot.ConsoleApp {
	class Program {
		static void Main(string[] args) {
			Dictionary<string, string> arguments = args
				.Select(a=>new {key = a.Substring(1,a.IndexOf(':')-1), value = a.Substring(a.IndexOf(':')+1) })
				.ToDictionary(p=>p.key, p=>p.value);
			DummyBot bot = new DummyBot(arguments);
			bool quit = false;
			while (!quit) {
				string line = Console.ReadLine();
				if (line.ToLower().Contains("quit"))
					quit = true;
				if (line.ToLower().Contains("save"))
					bot.SaveStates().Wait();
				if (line.ToLower().Contains("init"))
					bot.Init().Wait();
			}
			bot.SaveStates().Wait();
		}
	}
}
