using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Newtonsoft.Json;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Information;
using zLib;

namespace DateBot.Base {
	[DataContract]
	public class GuildTask : GuildConfig {

		//Users, Lobbies
		public DiscordChannel DateLobby { get; internal set; }
		public List<DiscordChannel> DateVoiceLobbies { get; } = new List<DiscordChannel>();
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

			DateVoiceLobbies.AddRange(DateRootCategory.Children.Where(c => c.Type == ChannelType.Voice &&
				!c.Name.ToLower().Contains("secret") &&
				c.Name.ToLower().Contains("lobby")));
			if (DateVoiceLobbies.Count == 0)
				await AddLastEmptyVoicceLobby();

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
			var pinned = await DateLobby.GetPinnedMessagesAsync();
			WelcomeMessage = pinned.FirstOrDefault(m => m.Author.Id == DateBot.Instance.BotId);
			if (WelcomeMessage == null) {
				//Add welcome message
				WelcomeMessage = await DateLobby.SendMessageAsync(WelcomeMessageBody);
				await WelcomeMessage.CreateReactionAsync(MaleEmoji).ConfigureAwait(false);
				await WelcomeMessage.CreateReactionAsync(FemaleEmoji).ConfigureAwait(false);
				await WelcomeMessage.PinAsync().ConfigureAwait(false);
			} else {
				foreach (var r in WelcomeMessage.Reactions) {
					if (r.Emoji == MaleEmoji || r.Emoji == FemaleEmoji) {
						var users = await WelcomeMessage.GetReactionsAsync(r.Emoji);
						foreach (var u in users) {
							ApplyGenderReactions(u, r.Emoji);
						}
					} else
						await WelcomeMessage.DeleteReactionsEmojiAsync(r.Emoji).ConfigureAwait(false);
				}
			}
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
					await TryMatch().ConfigureAwait(false);

				} else if (contBeforeChannel && !contAfterChannel) {
					//User left lobbies TODO check if not in private room
					AllUserStates.TryGetValue(e.User.Id, out var uState);
					uState.LastEnteredLobbyTime = default;
					if (UsersInLobbies.Contains(e.User))
						UsersInLobbies.Remove(e.User);
				}
				if(contBeforeChannel || contAfterChannel) {
					//Comb lobbies
					DateVoiceLobbies.Where(l => l.Id != DateVoiceLobbies.Last().Id && l.Users.Count() == 0)
						.ToList()
						.ForEach(async l => {
							DateVoiceLobbies.Remove(l);
							await l.DeleteAsync();
						});
					int i = 0;
					DateVoiceLobbies.ForEach(l => l.ModifyAsync(c=>c.Name = $"Date Voice Lobby {i++}"));
					if (DateVoiceLobbies.Last().Users.Count() > 0) {
						//Add empty at the end
						await AddLastEmptyVoicceLobby();
					}
				}
			}
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
			var allUserStatePairs = UsersInLobbies.Join(AllUserStates, u => u.Id, s => s.Key, (u, s) => new UserStatePair { User = u, State = s.Value }).ToArray();
			var boys = allUserStatePairs.Where(p => p.State.Gender == GenderEnum.Male).ToArray();
			var girls = allUserStatePairs.Where(p => p.State.Gender == GenderEnum.Female).ToArray();
			SortedList<float, UserStatePair[]> matchedList;
			if (boys.Length < girls.Length) {
				//Match boys => girls
				matchedList = MatchUnidirectionally(boys, girls);
			} else {
				//Match girls => boys
				matchedList = MatchUnidirectionally(girls, boys);
			}
			while (matchedList.Count > 0) {
				var match = matchedList.Values[0];
				await MoveToPrivateLobbyAsync(match);
				var rem = matchedList.Values.Where(m => m[1].State.UserId == match[1].State.UserId);
				foreach (var r in rem) {
					matchedList.RemoveAt(matchedList.IndexOfValue(r));
				}
			}
		}

		private async Task MoveToPrivateLobbyAsync(UserStatePair[] userStatePairs) {
			var privateRoom = await Guild.CreateChannelAsync("Secret Room", ChannelType.Voice, DateRootCategory);
			await privateRoom.PlaceMemberAsync(userStatePairs[0].User as DiscordMember).ConfigureAwait(false);
			await privateRoom.PlaceMemberAsync(userStatePairs[1].User as DiscordMember).ConfigureAwait(false);
		}

		public class UserStatePair {
			public DiscordUser User { get; set; }
			public UserState State { get; set; }
		}

		public SortedList<float, UserStatePair[]> MatchUnidirectionally( UserStatePair[] smallSet, UserStatePair[] biggerSet) {
			SortedList<float, UserStatePair[]> result = new SortedList<float, UserStatePair[]>();

			foreach (var u in smallSet)
				foreach (var m in biggerSet.Select(bu => new { eval = MatchWeight(u, bu), match = bu }))
					result.Add(m.eval, new UserStatePair[2] {u, m.match});
			

			return result;
		}

		/// <summary>
		/// Match evaluation
		/// </summary>
		/// <param name="a"></param>
		/// <param name="b"></param>
		/// <returns></returns>
		private float MatchWeight(UserStatePair a, UserStatePair b) {
			return a.State.LikedUserIds.Contains(b.State.UserId)?.5f:0f + (b.State.LikedUserIds.Contains(a.State.UserId)?.5f:0f);
		}

		internal async Task MessageReactionAdded(MessageReactionAddEventArgs e) {
			if (e.Message.Id == WelcomeMessage.Id && (e.Emoji.Id == MaleEmoji.Id || e.Emoji.Id == FemaleEmoji.Id)) {
				if (e.User.Id != DateBot.Instance.BotId)
					ApplyGenderReactions(e.User, e.Emoji);
			} else
				await e.Message.DeleteReactionsEmojiAsync(e.Emoji).ConfigureAwait(false);
		}

		public void ApplyGenderReactions(DiscordUser user, DiscordEmoji emoji) {
			AllUserStates.TryGetValue(user.Id, out var uState);
			if (uState != null)
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
