using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Newtonsoft.Json;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Information;
using zLib;

namespace DateBot.Base {
	[DataContract]
	public partial class GuildTask : GuildConfig {

		//Users, Lobbies
		public DiscordChannel DateLobby { get; internal set; }
		public List<DiscordChannel> DateVoiceLobbies { get; set; } = new List<DiscordChannel>();
		private List<DiscordChannel> PrivateRooms { get; set; } = new List<DiscordChannel>();
		public List<DiscordUser> UsersInLobbies { get; set; } = new List<DiscordUser>();
		public DiscordChannel LogChannel { get; set; }
		public DiscordMessage LogMessage { get; set; }

		public DiscordEmoji MaleEmoji { get; set; }
		public DiscordEmoji FemaleEmoji { get; set; }

		public void DebugLog(string message) {
			LogMessage?.ModifyAsync(LogMessage.Content + message + '\n');
		}

		public DiscordMessage WelcomeMessage { get; private set; }

		public DiscordGuild Guild { get; set; }
		public DiscordChannel DateRootCategory { get; set; }
		/// <summary>
		/// Init Guild. Mirror all registered users to serializable dictionary
		/// </summary>
		public GuildTask() {
		}

		/// <summary>
		/// Gets all channels, members in them and set's up event handlers
		/// </summary>
		/// <param name="guild"></param>
		/// <returns></returns>
		internal async Task Initialize(DiscordGuild guild) {
			Guild = guild;
			GuildId = guild.Id;

			LogChannel = guild.GetChannel(LogChannelId);
			if (LogChannel != null)
				LogMessage = await LogChannel.SendMessageAsync($"Begin of log {DateTime.Now.ToString()}\n");

			guild.Channels.TryGetValue(DateCategoryId, out DiscordChannel cat);
			if (cat == null) {
				string m = $"Initialize failed. No Category found on + {DateCategoryId}";
				DebugLog(m);
				throw new Exception(m);
			}
			DateRootCategory = cat;

			DateVoiceLobbies.AddRange(GetVoiceLobbies());
			PrivateRooms.AddRange(GetPrivateRooms());
			await CleanupLobbies();

			MaleEmoji = DiscordEmoji.FromName(DateBot.Instance.Client, MaleEmojiId);
			FemaleEmoji = DiscordEmoji.FromName(DateBot.Instance.Client, FemaleEmojiId);

			UsersInLobbies.Clear();
			//Add users in lobbies
			DateVoiceLobbies.ForEach(l => UsersInLobbies.AddRange(l.Users));
			foreach (var u in UsersInLobbies) {
				if (!AllUserStates.ContainsKey(u.Id)) {
					AllUserStates.Add(u.Id, new UserState() { UserId = u.Id, LastEnteredLobbyTime = DateTime.Now });
				}
			}

			//Check for welcome message
			DateLobby = DateRootCategory.Children.FirstOrDefault(c => c.Id == DateLobbyId);
			WelcomeMessage = await DateLobby.GetMessageAsync(WelcomeMessageId);
			if (WelcomeMessage == null) {
				//Add welcome message
				WelcomeMessage = await DateLobby.SendMessageAsync(WelcomeMessageBody);
				await WelcomeMessage.CreateReactionAsync(MaleEmoji).ConfigureAwait(false);
				await WelcomeMessage.CreateReactionAsync(FemaleEmoji).ConfigureAwait(false);
				await WelcomeMessage.PinAsync().ConfigureAwait(false);
			} else {
				foreach (var r in WelcomeMessage.GetReactionsAsync(MaleEmoji).Result) {
					ApplyGenderReactions(r, MaleEmoji);
					await WelcomeMessage.DeleteReactionsEmojiAsync(MaleEmoji).ConfigureAwait(false);
				}
				foreach (var r in WelcomeMessage.GetReactionsAsync(FemaleEmoji).Result) {
					ApplyGenderReactions(r, FemaleEmoji);
					await WelcomeMessage.DeleteReactionsEmojiAsync(FemaleEmoji).ConfigureAwait(false);
				}
				//Cleanup reactions
				WelcomeMessage.DeleteAllReactionsAsync().Wait();
				await WelcomeMessage.CreateReactionAsync(MaleEmoji).ConfigureAwait(false);
				await WelcomeMessage.CreateReactionAsync(FemaleEmoji).ConfigureAwait(false);
				//Cleanup rooms
				await CleanupLobbies();
			}
		}

		private IEnumerable<DiscordChannel> GetVoiceLobbies() {
			return DateRootCategory.Children.Where(c => c.Type == ChannelType.Voice && c.Name.ToLower().Contains("lobby"));
		}

		private IEnumerable<DiscordChannel> GetPrivateRooms() {
			return DateRootCategory.Children.Where(c => c.Type == ChannelType.Voice && c.Name.ToLower().Contains("secret"));
		}

		/// <summary>
		/// User changed state in voice lobbies (joined/left)
		/// </summary>
		/// <param name="e"></param>
		/// <returns></returns>
		public async Task VoiceStateUpdated(VoiceStateUpdateEventArgs e) {
			if (e.After?.Channel != e.Before?.Channel) { //Channel changed
				var contBeforeChannel = DateVoiceLobbies.Contains(e.Before?.Channel);
				var contAfterChannel = DateVoiceLobbies.Contains(e.After?.Channel);

				if (!contBeforeChannel && contAfterChannel) {
					//User connected to lobbies
					if (!UsersInLobbies.Contains(e.User))
						UsersInLobbies.Add(e.User);
					if (!AllUserStates.ContainsKey(e.User.Id))
						AllUserStates.Add(e.User.Id, new UserState() { UserId = e.User.Id });
					AllUserStates.TryGetValue(e.User.Id, out var uState);
					uState.LastEnteredLobbyTime = DateTime.Now;

					//Try Match
					await TryMatch();

				} else if (contBeforeChannel && !contAfterChannel) {
					//User left lobbies
					AllUserStates.TryGetValue(e.User.Id, out var uState);
					uState.LastEnteredLobbyTime = default;
					if (UsersInLobbies.Contains(e.User))
						UsersInLobbies.Remove(e.User);
				}
				if(contBeforeChannel || contAfterChannel) {
					await CleanupLobbies();
				}
			}
		}

		private async Task CleanupLobbies() {
			//Comb lobbies
			DateVoiceLobbies.Where(l => l.Id != DateVoiceLobbies.Last().Id && l.Users.Count() == 0)
				.ToList()
				.ForEach(async l => {
					DateVoiceLobbies.Remove(l);
					await l.DeleteAsync();
				});
			int i = 0;
			DateVoiceLobbies.ForEach(l => l.ModifyAsync(c => c.Name = $"Date Voice Lobby {i++}"));
			if (DateVoiceLobbies.Count == 0 || DateVoiceLobbies.Last().Users.Count() > 0) {
				//Add empty at the end
				await AddLastEmptyVoicceLobby();
			}

			try {
				foreach (var c in PrivateRooms.Where(c => (c.Users.Count() == 0 && (c.CreationTimestamp - DateTime.Now).TotalSeconds > 15))) {
					await c.DeleteAsync().ConfigureAwait(false);
				}
				PrivateRooms.RemoveAll(c => c.Users.Count() == 0);
			} catch (Exception) { }
		}

		private async Task AddLastEmptyVoicceLobby() {
			var c = await Guild.CreateChannelAsync($"Date Voice Lobby {DateVoiceLobbies.Count}", ChannelType.Voice, DateRootCategory);
			await c.AddOverwriteAsync(Guild.EveryoneRole, Permissions.AccessChannels);
			DateVoiceLobbies.Add(
				c
			);
		}

		/// <summary>
		/// Trying to match all users in lobbies
		/// </summary>
		/// <returns></returns>
		public async Task TryMatch() {
			//Choose matching direction
			var allUserStatePairs = UsersInLobbies.Join(AllUserStates, u => u.Id, s => s.Key, (u, s) => new UserStateDiscordUserPair { User = u, State = s.Value }).ToArray();
			var boys = allUserStatePairs.Where(p => p.State.Gender == GenderEnum.Male).ToArray();
			var girls = allUserStatePairs.Where(p => p.State.Gender == GenderEnum.Female).ToArray();
			if (boys.Length == 0 || girls.Length == 0) return;

			//Match all boys => all girls
			var matchesList = boys.Join(girls, a => true, b => true, (a, b) => new UsersPairMatch() { A = a, B = b, Match = MatchWeight(a, b) })
				.OrderBy(m => m.Match).ToList();

			while(matchesList.Count > 0) {
				var match = matchesList.First();
				matchesList.RemoveAll(m=>m.A.User.Id == match.A.User.Id || m.B.User.Id == match.B.User.Id);
				await MoveToPrivateLobbyAsync(match).ConfigureAwait(false);
			}
		}

		private async Task MoveToPrivateLobbyAsync(UsersPairMatch pair) {
			UsersInLobbies.Remove(pair.A.User);
			UsersInLobbies.Remove(pair.B.User);

			var privateRoom = await Guild.CreateChannelAsync("Secret Room", ChannelType.Voice, DateRootCategory);
			await privateRoom.AddOverwriteAsync(Guild.EveryoneRole, deny: Permissions.AccessChannels & Permissions.CreateInstantInvite);
			PrivateRooms.Add(privateRoom);

			pair.A.State.EnteredPrivateRoomTime = pair.B.State.EnteredPrivateRoomTime = DateTime.Now;

			await privateRoom.PlaceMemberAsync(pair.A.User as DiscordMember).ConfigureAwait(false);
			await privateRoom.PlaceMemberAsync(pair.B.User as DiscordMember).ConfigureAwait(false);

			//Launch timer
			if (PrivateRoomUpdater == null) { 
				PrivateRoomUpdater = new Timer(async s => await UpdatePrivateRooms(), null, 0, TimeSpan.FromSeconds(31).Milliseconds);
				PrivateRoomTimerRunning = true;
			} else if (!PrivateRoomTimerRunning) {
				PrivateRoomUpdater.Change(TimeSpan.FromSeconds(31).Milliseconds, TimeSpan.FromSeconds(31).Milliseconds);
				PrivateRoomTimerRunning = true;
			}
		}

		private Timer PrivateRoomUpdater { get; set; }
		private bool PrivateRoomTimerRunning { get; set; }

		private async Task UpdatePrivateRooms() {
			PrivateRooms.Where(r => r.Users.Count() == 0).ToList().ForEach(async r => await r.DeleteAsync().ConfigureAwait(false));
			PrivateRooms.RemoveAll(r => r.Users.Count() == 0);
			foreach (var r in PrivateRooms) {
				var user = r.Users.FirstOrDefault();
				if (user != null) {
					AllUserStates.TryGetValue(user.Id, out var uState);
					if (uState != null && uState.EnteredPrivateRoomTime.HasValue) {
						var time = (uState.EnteredPrivateRoomTime.Value - DateTime.Now).Milliseconds;
						if (time > SecretRoomTime) {
							//Break room, return pair into lobby
							var lobby0 = DateVoiceLobbies[0];
							foreach (var u in r.Users) {
								await lobby0.PlaceMemberAsync(u).ConfigureAwait(false);
							}
						}
					}
				}
			}
			if (PrivateRooms.Count == 0) {
				//Pause timer
				PrivateRoomUpdater.Change(Timeout.Infinite, Timeout.Infinite);
				PrivateRoomTimerRunning = false;
			}
		}

		/// <summary>
		/// Match evaluation
		/// </summary>
		/// <param name="a"></param>
		/// <param name="b"></param>
		/// <returns></returns>
		private float MatchWeight(UserStateDiscordUserPair a, UserStateDiscordUserPair b) {
			return a.State.LikedUserIds.Contains(b.State.UserId)?.5f:0f + (b.State.LikedUserIds.Contains(a.State.UserId)?.5f:0f);
		}

		internal async Task MessageReactionAdded(MessageReactionAddEventArgs e) {
			if (e.Message.Id == WelcomeMessage.Id && (e.Emoji.Id == MaleEmoji.Id || e.Emoji.Id == FemaleEmoji.Id)) {
				ApplyGenderReactions(e.User, e.Emoji);
			} else
				await e.Message.DeleteReactionsEmojiAsync(e.Emoji).ConfigureAwait(false);
		}

		public void ApplyGenderReactions(DiscordUser user, DiscordEmoji emoji) {
			if (user.Id == DateBot.Instance.BotId) return;
			AllUserStates.TryGetValue(user.Id, out var uState);
			if (uState == null) {
				uState = new UserState() { UserId = user.Id };
				AllUserStates.Add(user.Id, uState);
			}
			if (emoji == MaleEmoji) {
				uState.Gender = GenderEnum.Male;
				WelcomeMessage.DeleteReactionAsync(MaleEmoji, user);
			} else if (emoji == FemaleEmoji) {
				uState.Gender = GenderEnum.Female;
				WelcomeMessage.DeleteReactionAsync(FemaleEmoji, user);
			}
		}
	}

	public enum GenderEnum { None = 0, Male = 1, Female = 2 }
}
