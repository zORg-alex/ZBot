using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace Bot.ConsoleProgram {
	class Program {
		static void Main(string[] args) {
			var z = DateBot.Base.JSONDateBotStateProvider.CreateAndLoad(@".\save\zzz.json").ContinueWith(async (o)=> {
				o.Result.GuildTasks.Add(new DateBot.Base.JSONDateBotGuildTaskProvider() { GuildId = 1111 });
				await o.Result.SaveAsync();
				return o.Result;
			});


			Dictionary<string, string> arguments = args
				.Select(a => new { key = a.Substring(1, a.IndexOf(':') - 1), value = a.Substring(a.IndexOf(':') + 1) })
				.ToDictionary(p => p.key, p => p.value);
			var bot = new DateBot.Base.DateBot(arguments);
			bool quit = false;
			var autosaveTimer = new Timer(async (e) => {if (bot.State != null) await bot.SaveStates().ConfigureAwait(false); }, null, 0, 60000);
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
