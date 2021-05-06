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
		public IDateBotStateProvider State { get; private set; }

		public List<DateBotGuildTask> GuildTasks { get; private set; } = new List<DateBotGuildTask>();

		public DateBot(Dictionary<string, string> args, IDateBotStateProvider stateProvider) :base(args) {
			if (Instance != null) throw new Exception("One DateBot already existing!");
			Instance = this;
			State = stateProvider;
		}

		/// <summary>
		/// Returns true if guild is in State
		/// </summary>
		/// <param name="guildId"></param>
		/// <returns></returns>
		internal bool GuildRegistered(ulong guildId) => State.GuildStates.Any(g => g.GuildId == guildId);

		/// <summary>
		/// Add new Guild to config and start new GuildTask
		/// </summary>
		/// <param name="config"></param>
		internal void AddGuild(IDateBotGuildState config) {
			State.GuildStates.Add(config);
			var gt = new DateBotGuildTask(config);
			GuildTasks.Add(gt);
			_ = gt.Initialize();
		}

		internal DateBotGuildTask GetGuildTask(ulong guildId) => GuildTasks.FirstOrDefault(g => g.Guild.Id == guildId);

		public override void RegisterCommands() {
			CommandsNext.RegisterCommands<DateBotCommands>();
			CommandsNext.RegisterCommands<SomeCommands>();
		}

		/// <summary>
		/// Serialize bot State.
		/// </summary>
		/// <returns></returns>
		public async Task SaveStates() {
			await State.SaveAsync().ConfigureAwait(false);
		}

		/// <summary>
		/// When Bot is up, set up event handlers for guilds and voice channel updates.
		/// </summary>
		/// <param name="e"></param>
		/// <returns></returns>
		override public async Task ClientReadyAsync(DSharpPlus.EventArgs.ReadyEventArgs e) {

			await State.LoadAsync().ConfigureAwait(false);

			//Should I discard instead of await this one. If it get's stuck it won't function anywhere past this method
#pragma warning disable CS1998
			e.Client.GuildAvailable += async (e) => _ = InitGuildAsync(e.Guild).ConfigureAwait(false);
#pragma warning restore CS1998

			Client.VoiceStateUpdated += Client_VoiceStateUpdated;
		}

		/// <summary>
		/// Add new Guild Task.
		/// Pickup from where we left off, add all users in lobbies
		/// </summary>
		/// <param name="guild"></param>
		/// <returns></returns>
		private async Task InitGuildAsync(DiscordGuild guild) {
			var st = State.GuildStates.FirstOrDefault(g => g.GuildId == guild.Id);
			if (st == null) {
				st = new DateBotGuildState() { GuildId = guild.Id };
				State.AddGuildState(st);
			}
			var gt = new DateBotGuildTask(st);
			GuildTasks.Add(gt);
			await gt.Initialize().ConfigureAwait(false);
		}

		/// <summary>
		/// VoiceChannel update redirects to a registered guild
		/// </summary>
		/// <param name="e"></param>
		/// <returns></returns>
		private async Task Client_VoiceStateUpdated(VoiceStateUpdateEventArgs e) {
			var g = GetGuildTask(e.Guild.Id);
			if (g!= null) {
				await g.VoiceStateUpdated(e).ConfigureAwait(false);
			}
		}
	}
}
