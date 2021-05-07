using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using Newtonsoft.Json;
using ZBot;

namespace DummyBot.ConsoleApp {
	public class DummyBot : Bot {

		public static new DummyBot Instance {get; private set;}
		public DummyBot(Dictionary<string, string> args) : base(args, useVoiceNext:true) {
			Instance = this;
		}

		public BotStateConfig State { get; private set; }

		internal async Task Init() {
			foreach (var g in State.Guilds) {
				await g.Initialize();
			}
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

		public GuildTask GetGuild(ulong guildId) => State.Guilds.FirstOrDefault(g => g.GuildId == guildId);

		/// <summary>
		/// Serialize bot State. TODO make it run every minute?
		/// </summary>
		/// <returns></returns>
		public async Task SaveStates() {
			try {
				if (State != null && State.Guilds.Count > 0)
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
		override public async Task ClientReadyAsync(DiscordClient c, DSharpPlus.EventArgs.ReadyEventArgs e) {

			if (!File.Exists("botState.json")) {
				State = new BotStateConfig();
			} else {
				//Deserealize bot last state
				using (var sr = new StreamReader("botState.json")) {
					State = JsonConvert.DeserializeObject<BotStateConfig>(
						await sr.ReadToEndAsync()
					);
					if (State == null) State = new BotStateConfig();
				}
			}

			//Should I discard instead of await this one. If it get's stuck it won't function anywhere past this method
#pragma warning disable CS1998
			c.GuildAvailable += async (c, e) => _ = InitGuildAsync(e.Guild).ConfigureAwait(false);
#pragma warning restore CS1998
		}

		public override void RegisterCommands() {
			CommandsNext.RegisterCommands<DummyCommands>();
		}

		/// <summary>
		/// Pickup from where we left off, add all users in lobbies
		/// </summary>
		/// <param name="guild"></param>
		/// <returns></returns>
		private async Task InitGuildAsync(DiscordGuild guild) {
			var g = State.Guilds.FirstOrDefault(g => g.GuildId == guild.Id);
			if (g != null) await g.Initialize(guild).ConfigureAwait(false);
		}
	}

	public class BotStateConfig {
		public List<GuildTask> Guilds { get; private set; } = new List<GuildTask>();
	}

	[DataContract]
	public class GuildTask : GuildConfig {
		public DiscordGuild Guild { get; private set; }
		public DiscordChannel LogChannel { get; internal set; }
		public DiscordChannel ConnectToVoice { get; internal set; }
		public DiscordMessage SetRandomReactionOnMessage { get; internal set; }
		public DiscordChannel SetRandomReactionOnChannel { get; internal set; }

		internal async Task Initialize(DiscordGuild guild = null) {
			if (guild != null) Guild = guild;
			//GuildId = guild.Id;

			ConnectToVoice = Guild.Channels.FirstOrDefault(c=>c.Value.Name == ConnectToVoiceName).Value;
			SetRandomReactionOnChannel = Guild.GetChannel(SetRandomReactionOnChannelId);
			if (SetRandomReactionOnChannel != null)
				SetRandomReactionOnMessage = await SetRandomReactionOnChannel?.GetMessageAsync(SetRandomReactionOnMessageId);

			try {
				var r = SetRandomReactionOnMessage.Reactions[new Random().Next(0, 2)];//Assuming Gender reactinos are always first
				await SetRandomReactionOnMessage.CreateReactionAsync(r.Emoji);
				_ = DummyBot.Instance.VoiceNextConfiguration.ConnectAsync(ConnectToVoice).ConfigureAwait(false);
			} catch (Exception e) {
				Console.WriteLine(e);
			}
		}
	}
	[DataContract]
	public class GuildConfig{
		[DataMember]
		public ulong LogChannelId { get; internal set; }
		[DataMember]
		public ulong GuildId { get; internal set; }
		[DataMember]
		public String ConnectToVoiceName { get; internal set; }
		[DataMember]
		public ulong SetRandomReactionOnMessageId { get; internal set; }
		[DataMember]
		public ulong SetRandomReactionOnChannelId { get; internal set; }
	}

}
