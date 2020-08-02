using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using Newtonsoft.Json;
namespace ZBot {
	public class Bot {
		private CancellationTokenSource _cts { get; set; }
		public DiscordClient Client { get; private set; }
		public CommandsNextExtension Commands { get; private set; }

		public List<GuildTask> GuildsConfigs { get; private set; } = new List<GuildTask>();

		public static Bot Instance { get; private set; }
		public ulong Id { get; internal set; }

		public Bot() {
			Instance = this;
		}

		public async Task RunAsync() {

			_cts = new CancellationTokenSource();

			var json = string.Empty;
			using (var fs = File.OpenRead("config.json"))
			using (var sr = new StreamReader(fs, new UTF8Encoding(false)))
				json = await sr.ReadToEndAsync().ConfigureAwait(false);

			dynamic jsonconfig = JsonConvert.DeserializeObject<object>(json);
			var config = new DiscordConfiguration() {
				Token = jsonconfig.token,
				TokenType = TokenType.Bot,
				AutoReconnect = true,
				LogLevel = LogLevel.Debug,
				UseInternalLogHandler = true
			};
			Client = new DiscordClient(config);
			Client.Ready += OnClientReadyAsync;

			var commandsConfig = new CommandsNextConfiguration() {
				StringPrefixes = new string[] { jsonconfig.prefix },
				EnableMentionPrefix = true,
				EnableDms = true
			};

			Commands = Client.UseCommandsNext(commandsConfig);
			Commands.RegisterCommands<SomeCommands>();

			Client.DebugLogger.LogMessageReceived += LogMessageAction;

			await Client.ConnectAsync();

			//await Task.Delay(-1);//cheap fix to not quit
		}

		public System.EventHandler<DSharpPlus.EventArgs.DebugLogMessageEventArgs> LogMessageAction { get; set; }

		private async Task OnClientReadyAsync(DSharpPlus.EventArgs.ReadyEventArgs e) {

			Id = Client.CurrentUser.Id;
			foreach (var g in e.Client.Guilds) {
				//is guild available?
				if (g.Value.Name == null)
					e.Client.GuildAvailable += async (z) => await InitGuildAsync(z.Guild).ConfigureAwait(false);
				else
					await InitGuildAsync(g.Value).ConfigureAwait(false);
			}
		}

		private async Task InitGuildAsync( DiscordGuild g) {
			var gc = new GuildTask(_cts);
			gc.Guild = g;

			gc.DateRootCategory = g.Channels.FirstOrDefault(c => c.Value.Type == ChannelType.Category && c.Value.Name.ToLower().Contains("dating")).Value;

			gc.DateLobby = gc.DateRootCategory.Children.FirstOrDefault(c => c.Type == ChannelType.Text &&
				!c.Name.ToLower().Contains("secret") &&
				c.Name.ToLower().Contains("lobby"));

			gc.VoiceLobbies.AddRange(gc.DateRootCategory.Children.Where(c => c.Type == ChannelType.Voice &&
				!c.Name.ToLower().Contains("secret") &&
				c.Name.ToLower().Contains("lobby")));

			if (gc.VoiceLobbies.Count == 0) {
				gc.LobbyCounter = 0;
				await AddVoiceLobbyAsync(gc);
			} else gc.UpdateLobbyCounter();

			gc.RefreshLobbyMembers();

			GuildsConfigs.Add(gc);

#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
			gc.RunTask().ConfigureAwait(false);
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
		}

		public async Task AddVoiceLobbyAsync(GuildTask gc) {
			var l = await gc.Guild.CreateVoiceChannelAsync("Date Voice Lobby " + ++gc.LobbyCounter, gc.DateRootCategory);
			//await l.ModifyPositionAsync(gc.DateLobby.Position + 1);
			gc.VoiceLobbies.Add(l);
		}
	}
}
