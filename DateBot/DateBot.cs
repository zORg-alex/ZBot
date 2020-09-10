using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Newtonsoft.Json;
using ZBot;

namespace DateBot.Base {
	public class DateBot: ZBot.Bot {
		public new static DateBot Instance { get; protected set; }
		/// <summary>
		/// Serializable Bot State
		/// </summary>
		public BotStateConfig State { get; private set; }

		public DateBot(Dictionary<string, string> args) :base(args) {
			Instance = this;
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

		public override void RegisterCommands() {
			CommandsNext.RegisterCommands<DateBotCommands>();
			CommandsNext.RegisterCommands<SomeCommands>();
		}

		/// <summary>
		/// Serialize bot State. TODO make it run every minute?
		/// </summary>
		/// <returns></returns>
		public async Task SaveStates() {
			try {
				using (var sr = new StreamWriter("botState.json")) {
					await sr.WriteAsync(JsonConvert.SerializeObject(State));
				}
			} catch (Exception e) { Console.WriteLine(e); }
		}

		/// <summary>
		/// When Bot is up, set up event handlers for guilds and voice channel updates.
		/// </summary>
		/// <param name="e"></param>
		/// <returns></returns>
		override public async Task ClientReadyAsync(DSharpPlus.EventArgs.ReadyEventArgs e) {

			//Deserealize bot last state
			using (var sr = new StreamReader("botState.json")) {
				State = JsonConvert.DeserializeObject<BotStateConfig>(
					await sr.ReadToEndAsync()
				);
				if (State == null) State = new BotStateConfig();
			}

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
