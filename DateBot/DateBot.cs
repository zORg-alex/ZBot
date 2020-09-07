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

namespace DateBot.Base {
	public class DateBot {
		public static DateBot Instance { get; protected set; }
		public DiscordClient Client { get; protected set; }

		public ulong BotId { get; private set; }
		public DiscordUser BotUser { get; private set; }

		public CommandsNextExtension CommandsNext { get; protected set; }
		public InteractivityExtension InteractivityConfiguration { get; protected set; }
		/// <summary>
		/// Serializable Bot State
		/// </summary>
		public BotStateConfig State { get; private set; }

		public DateBot() {
			if (Instance != null)
				throw new InvalidOperationException("Instance is already running");
			Instance = this;
			RunAsync().ConfigureAwait(false);
		}
		/// <summary>
		/// Returns true if guild is in State
		/// </summary>
		/// <param name="guildId"></param>
		/// <returns></returns>
		internal bool GuildRegistered(ulong guildId) => State.Guilds.Any(g => g.GuildId == guildId);

		internal void AddGuild(GuildTask config) {
			State.Guilds.Add(config);
			//OnBeforeExit().ConfigureAwait(false);
		}

		internal GuildTask GetGuild(ulong guildId) => State.Guilds.FirstOrDefault(g => g.GuildId == guildId);

		/// <summary>
		/// Initializes the bot Asynchronously
		/// </summary>
		/// <returns></returns>
		public async Task RunAsync() {
			//Read bot connection config
			DSharpBotConfig connectionConfig = JsonConvert.DeserializeObject<DSharpBotConfig>(
				await new StreamReader("config.json").ReadToEndAsync()
			);

			//Deserealize bot last state
			using (var sr = new StreamReader("botState.json")) {
				State = JsonConvert.DeserializeObject<BotStateConfig>(
					await sr.ReadToEndAsync()
				);
				if (State == null) State = new BotStateConfig();
			}

			Client = new DiscordClient(new DiscordConfiguration {
				Token = connectionConfig.Token,
				TokenType = TokenType.Bot,
				AutoReconnect = true
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

			CommandsNext.RegisterCommands<DateBotCommands>();
			CommandsNext.RegisterCommands<SomeCommands>();

			await Client.ConnectAsync();
		}

		/// <summary>
		/// Serialize bot State. TODO make it run every minute?
		/// </summary>
		/// <returns></returns>
		public async Task SaveStates() {
			using (var sr = new StreamWriter("botState.json")) {
				await sr.WriteAsync( JsonConvert.SerializeObject(State));
			}
		}

		/// <summary>
		/// When Bot is up, set up event handlers for guilds and voice channel updates.
		/// </summary>
		/// <param name="e"></param>
		/// <returns></returns>
		private async Task OnClientReadyAsync(DSharpPlus.EventArgs.ReadyEventArgs e) {

			BotId = Client.CurrentUser.Id;
			BotUser = Client.CurrentUser;
			foreach (var g in e.Client.Guilds) {
				if (g.Value.Name == null)
					e.Client.GuildAvailable += async (z) => await InitGuildAsync(z.Guild).ConfigureAwait(false);
				else
					await InitGuildAsync(g.Value).ConfigureAwait(false);
			}

			Client.VoiceStateUpdated += Client_VoiceStateUpdated;
			Client.MessageReactionAdded += Client_MessageReactionAdded;

			//????
			State.Guilds.ForEach(g => {
				DiscordGuild guild;
				Client.Guilds.TryGetValue(g.GuildId, out guild);
				g.Guild = guild;
			});
		}

		/// <summary>
		/// Pickup from where we left off, add all users in lobbies
		/// </summary>
		/// <param name="guild"></param>
		/// <returns></returns>
		private async Task InitGuildAsync(DiscordGuild guild) {
			var g = State.Guilds.FirstOrDefault(g => g.Guild.Id == guild.Id);
			if (g != null) await g.Initialize(guild).ConfigureAwait(false);
		}

		/// <summary>
		/// Call on Guild Reaction added
		/// </summary>
		/// <param name="e"></param>
		/// <returns></returns>
		private async Task Client_MessageReactionAdded(MessageReactionAddEventArgs e) {
			var g = State.Guilds.FirstOrDefault(g => g.Guild.Id == e.Guild.Id);
			if (g != null) {
				await g.MessageReactionAdded(e).ConfigureAwait(false);
			}
		}

		/// <summary>
		/// VoiceChannel update redirects to a registered guild
		/// </summary>
		/// <param name="e"></param>
		/// <returns></returns>
		private async Task Client_VoiceStateUpdated(VoiceStateUpdateEventArgs e) {
			var g = State.Guilds.FirstOrDefault(g => g.Guild.Id == e.Guild.Id);
			if (g!= null) {
				await g.VoiceStateUpdated(e).ConfigureAwait(false);
			}
		}
	}
}
