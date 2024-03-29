﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.Interactivity;
using DSharpPlus.VoiceNext;
using Newtonsoft.Json;
using ZBot;

namespace ZBot {
	public class Bot {
		public static Bot Instance { get; protected set; }
		public DiscordClient Client { get; protected set; }

		public ulong BotId { get; private set; }
		public DiscordUser BotUser { get; private set; }

		public CommandsNextExtension CommandsNext { get; protected set; }
		public InteractivityExtension InteractivityConfiguration { get; protected set; }
		public Voice​Next​Extension Voice​Next​Configuration { get; protected set; }

		public Bot(Dictionary<string, string> args) {
			if (Instance != null)
				throw new InvalidOperationException("Instance is already running");
			Instance = this;

			BotConfig conf = null;
			try {
				if (args.Count > 0)
					conf = new BotConfig() { Token = args["token"], Prefix = args["prefix"] };
			} catch { throw new ArgumentException("Couldn't find token and prefix arguements"); }

			ConfigBotAsync(conf).ConfigureAwait(false);
		}
		/// <summary>
		/// Initializes the bot Asynchronously
		/// </summary>
		/// <returns></returns>
		public async Task ConfigBotAsync(BotConfig connectionConfig = null) {
			//Read bot connection config
			if (connectionConfig == null) connectionConfig = JsonConvert.DeserializeObject<BotConfig>(
				await new StreamReader("config.json").ReadToEndAsync()
			);

			Client = new DiscordClient(new DiscordConfiguration {
				Token = connectionConfig.Token,
				TokenType = TokenType.Bot,
				AutoReconnect = true,
				MinimumLogLevel = Microsoft.Extensions.Logging.LogLevel.Error
			});
			Client.Ready += OnClientReadyAsync;

			CommandsNext = Client.UseCommandsNext(
				new CommandsNextConfiguration {
					StringPrefixes = new string[] { connectionConfig.Prefix },
					EnableMentionPrefix = true,
					EnableDms = true
				});

			InteractivityConfiguration = Client.UseInteractivity(
				new InteractivityConfiguration {
					Timeout = TimeSpan.FromSeconds(30)
				});

			Voice​Next​Configuration = Client.UseVoiceNext();

			RegisterCommands();

			await Client.ConnectAsync();
		}

		public virtual void RegisterCommands() {}

		/// <summary>
		/// When Bot is up, set up event handlers for guilds and voice channel updates.
		/// </summary>
		/// <param name="e"></param>
		/// <returns></returns>
		private async Task OnClientReadyAsync(DSharpPlus.EventArgs.ReadyEventArgs e) {

			BotId = Client.CurrentUser.Id;
			BotUser = Client.CurrentUser;

			await ClientReadyAsync(e);
		}

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
		public virtual async Task ClientReadyAsync(ReadyEventArgs e) {}
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
	}
}
