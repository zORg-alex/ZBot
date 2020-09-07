using System;
using System.Diagnostics;
using System.Threading;

namespace Bot.ConsoleProgram {
	class Program {
		static void Main(string[] args) {
			DateBot.Base.DateBot bot = new DateBot.Base.DateBot();
			bool quit = false;
			while (!quit) {
				string line = Console.ReadLine();
				if (line.ToLower().Contains("quit"))
					quit = true;
				if (line.ToLower().Contains("save"))
					bot.SaveStates().Wait();
			}
			bot.SaveStates().Wait();
		}
	}
}
