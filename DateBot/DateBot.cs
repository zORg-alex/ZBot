using System;
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
using Newtonsoft.Json;

namespace DateBot {
	public class Bot {
		public static Bot Instance { get; protected set; }
		public DiscordClient Client { get; protected set; }
		public ulong BotId { get; private set; }
		public CommandsNextExtension CommandsNext { get; protected set; }
		public InteractivityExtension InteractivityConfiguration { get; protected set; }
		public KeyValuePair<ulong, DiscordChannel> BotChannel { get; private set; }
		public EventHandler<DebugLogMessageEventArgs> LogMessageAction { set {
				if (Client != null) Client.DebugLogger.LogMessageReceived += value;
				else throw new Exception("DiscorBot not initialised yet.");
			} }

		public Bot() {
			if (Instance != null)
				throw new InvalidOperationException("Instance is already running");
			Instance = this;


		}

		public async Task RunAsync() {

			DSharpBotConfig config = JsonConvert.DeserializeObject<DSharpBotConfig>(
				await new StreamReader("config.json").ReadToEndAsync()
			);

			Client = new DiscordClient(new DiscordConfiguration {
				Token = config.Token,
				TokenType = TokenType.Bot,
				AutoReconnect = true,
				LogLevel = LogLevel.Debug,
				UseInternalLogHandler = true
			});
			Client.Ready += OnClientReadyAsync;

			CommandsNext = Client.UseCommandsNext(
				new CommandsNextConfiguration {
					StringPrefixes = new string[] { config.Prefix },
					EnableMentionPrefix = true,
					EnableDms = true
				});

			InteractivityConfiguration = Client.UseInteractivity(
				new InteractivityConfiguration {
					Timeout = TimeSpan.FromSeconds(30)
				});

			CommandsNext.RegisterCommands(typeof(Bot).Assembly);
		}

		private async Task OnClientReadyAsync(DSharpPlus.EventArgs.ReadyEventArgs e) {

			BotId = Client.CurrentUser.Id;
			foreach (var g in e.Client.Guilds) {
				if (g.Value.Name == null)
					e.Client.GuildAvailable += async (z) => await InitGuildAsync(z.Guild).ConfigureAwait(false);
				else
					await InitGuildAsync(g.Value).ConfigureAwait(false);
			}
		}

		private Task InitGuildAsync(DiscordGuild guild) {
			BotChannel = guild.Channels.FirstOrDefault(c => c.Value.Name.Contains("bot"));
			return null;
		}
	}
}
