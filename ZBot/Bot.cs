using System;
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
using ZBot;

namespace DateBotBase {
	public class Bot {
		public static Bot Instance { get; protected set; }
		public DiscordClient Client { get; protected set; }

		public ulong BotId { get; private set; }
		public DiscordUser BotUser { get; private set; }

		public CommandsNextExtension CommandsNext { get; protected set; }
		public InteractivityExtension InteractivityConfiguration { get; protected set; }

		public Bot() {
			if (Instance != null)
				throw new InvalidOperationException("Instance is already running");
			Instance = this;
			RunAsync().ConfigureAwait(false);
		}
		/// <summary>
		/// Initializes the bot Asynchronously
		/// </summary>
		/// <returns></returns>
		public async Task RunAsync() {
			//Read bot connection config
			BotConfig connectionConfig = JsonConvert.DeserializeObject<BotConfig>(
				await new StreamReader("config.json").ReadToEndAsync()
			);

			Client = new DiscordClient(new DiscordConfiguration {
				Token = connectionConfig.token,
				TokenType = TokenType.Bot,
				AutoReconnect = true
			});
			Client.Ready += OnClientReadyAsync;

			CommandsNext = Client.UseCommandsNext(
				new CommandsNextConfiguration {
					StringPrefixes = new string[] { connectionConfig.prefix },
					EnableMentionPrefix = true,
					EnableDms = true
				});

			InteractivityConfiguration = Client.UseInteractivity(
				new InteractivityConfiguration {
					Timeout = TimeSpan.FromSeconds(30)
				});

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
