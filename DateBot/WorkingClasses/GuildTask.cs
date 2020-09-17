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
using Timer = System.Timers.Timer;

namespace DateBot.Base {
	[DataContract]
	public partial class GuildTask : GuildConfig {

		public DiscordGuild Guild { get; set; }
		public DiscordChannel LogChannel { get; set; }
		public DiscordMessage LogMessage { get; set; }
		public DiscordChannel DateRootCategory { get; set; }
		public DiscordChannel DateLobby { get; internal set; }
		public DiscordMessage WelcomeMessage { get; private set; }
		public List<DiscordChannel> DateVoiceLobbies { get; set; } = new List<DiscordChannel>();
		public List<DiscordUser> UsersInLobbies { get; set; } = new List<DiscordUser>();
		private List<DiscordChannel> SecretRooms { get; set; } = new List<DiscordChannel>();
		private List<PairInSecretRoom> PairsInSecretRooms { get; set; } = new List<PairInSecretRoom>();

		public DiscordEmoji MaleEmoji { get; set; }
		public DiscordEmoji FemaleEmoji { get; set; }

		public void DebugLog(string message) {
			LogMessage?.ModifyAsync(LogMessage.Content + message + '\n');
		}


		public Timer UpdateTimer { get; private set; }
		public bool Initialized { get; private set; }
		public Task InitTask { get; internal set; }

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

			DateVoiceLobbies.Clear();
			SecretRooms.Clear();
			DateVoiceLobbies.AddRange(GetVoiceLobbies());
			SecretRooms.AddRange(GetSecretRooms());
			VoiceChannelsInitAsync();
			if (DateVoiceLobbies.Count == 0 || DateVoiceLobbies.All(c => c.Users.Count() > 0)) await AddLastEmptyVoiceLobby().ConfigureAwait(false);

			MaleEmoji = DiscordEmoji.FromName(DateBot.Instance.Client, MaleEmojiId);
			FemaleEmoji = DiscordEmoji.FromName(DateBot.Instance.Client, FemaleEmojiId);

			foreach (var u in UsersInLobbies) {
				if (!AllUserStates.ContainsKey(u.Id)) {
					AllUserStates.Add(u.Id, new UserState() { UserId = u.Id, LastEnteredLobbyTime = DateTime.Now });
				}
			}

			//Check for welcome message
			DateLobby = DateRootCategory.Children.FirstOrDefault(c => c.Id == DateLobbyId);
			await WelcomeMessageInit().ConfigureAwait(false);

			UpdateTimer = new Timer(TimeSpan.FromSeconds(30).TotalMilliseconds) { AutoReset = true };
			UpdateTimer.Elapsed += UpdateTimer_Elapsed;
			UpdateTimer.Start();

			InitTask = null;
		}

		private void UpdateTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e) {

			//Return from lobbies that timed out
			foreach (var p in PairsInSecretRooms.Where(pp => pp.Timeout > DateTime.Now)) {
				Task.WaitAll(p.Users.Select(async u => await DateVoiceLobbies[0].PlaceMemberAsync(u).ConfigureAwait(false)).ToArray());
			}
			//Try match
			if (UsersInLobbies.Count > 1) {
				var userStatePairsInLobbies = UsersInLobbies.Join(AllUserStates,
					u => u.Id, s => s.Key,
					(u, s) => new UserStateDiscordUserPair {
						User = Guild.GetMemberAsync(s.Key).Result,
						State = s.Value
					}).ToArray();
				var matches = (from a in userStatePairsInLobbies
								from b in userStatePairsInLobbies
								select Match(a.State, b.State))
								.Where(m=>m.A.State != m.B.State && m.A.State.Gender != m.B.State.Gender)
					.OrderByDescending(m => m.Match)
					.ToList();

				//Move to private rooms
				bool exit = false;
				while (matches.Count() > 0 && !exit) {
					var pair = matches.First();
					matches.RemoveAll(m => m.A.State == pair.A.State
					|| m.A.State == pair.B.State
					|| m.B.State == pair.A.State
					|| m.B.State == pair.B.State
					);
					MoveToPrivateLobbyAsync(pair).ConfigureAwait(false).GetAwaiter();

					if (matches.All(m => m.Match == 0 || m.A.State.Gender == m.B.State.Gender))
						exit = true;
				}
			}
		}

		private UsersPairMatch Match(UserState A, UserState B) {
			float match = A.Gender != B.Gender ? 1f : 0f;
			var girl = (A.Gender == GenderEnum.Female) ? A : (B.Gender == GenderEnum.Female) ? B : null;
			var boy = (A.Gender == GenderEnum.Male) ? A : (B.Gender == GenderEnum.Male) ? B : null;
			if (girl != null && boy != null)
				if (girl.AgeOptions.Count > 0 && boy.AgeOptions.Count > 0 && girl.AgeOptions.Contains(boy.AgeOptions.FirstOrDefault()))
					match += 1f;
			return new UsersPairMatch() {
				A = new UserStateDiscordUserPair() { State = A, User = Guild.GetMemberAsync(A.UserId).Result },
				B = new UserStateDiscordUserPair() { State = B, User = Guild.GetMemberAsync(B.UserId).Result },
				Match = match
			};
		}

		private async Task MoveToPrivateLobbyAsync(UsersPairMatch pair) {
			UsersInLobbies.Remove(pair.A.User);
			UsersInLobbies.Remove(pair.B.User);

			var privateRoom = await Guild.CreateChannelAsync("Secret Room", ChannelType.Voice, DateRootCategory);
			await privateRoom.AddOverwriteAsync(Guild.EveryoneRole, deny: Permissions.AccessChannels & Permissions.CreateInstantInvite);
			SecretRooms.Add(privateRoom);

			pair.A.State.EnteredPrivateRoomTime = pair.B.State.EnteredPrivateRoomTime = DateTime.Now;

			await privateRoom.PlaceMemberAsync(pair.A.User).ConfigureAwait(false);
			await privateRoom.PlaceMemberAsync(pair.B.User).ConfigureAwait(false);

			PairsInSecretRooms.Add(new PairInSecretRoom() {
				Users = new List<DiscordMember>() { pair.A.User, pair.B.User },
				SecretRoom = privateRoom,
				Timeout = DateTime.Now.AddMilliseconds(SecretRoomTime)
			});
		}

		async Task WelcomeMessageInit() {

			try {
				WelcomeMessage = await DateLobby.GetMessageAsync(WelcomeMessageId);
			} catch (Exception) { }
			if (WelcomeMessage == null) {
				//Add welcome message
				WelcomeMessage = await DateLobby.SendMessageAsync(WelcomeMessageBody);
				WelcomeMessageId = WelcomeMessage.Id;
				await WelcomeMessage.CreateReactionAsync(MaleEmoji).ConfigureAwait(false);
				await WelcomeMessage.CreateReactionAsync(FemaleEmoji).ConfigureAwait(false);
				await WelcomeMessage.PinAsync().ConfigureAwait(false);
			} else {
				var wm = DateLobby.GetMessageAsync(WelcomeMessageId).Result;
				foreach (var r in wm.Reactions.ToArray()) {
					if (r.Emoji == MaleEmoji || r.Emoji == FemaleEmoji) {
						foreach (var u in wm.GetReactionsAsync(r.Emoji).Result.Where(r => r.Id != DateBot.Instance.BotId)) {
							ApplyGenderReactions(u, r.Emoji);
						}
					} else
						await WelcomeMessage.DeleteReactionsEmojiAsync(r.Emoji).ConfigureAwait(false);
				}
				if (WelcomeMessage.GetReactionsAsync(MaleEmoji).Result.Count == 0)
					await WelcomeMessage.CreateReactionAsync(MaleEmoji).ConfigureAwait(false);
				if (WelcomeMessage.GetReactionsAsync(FemaleEmoji).Result.Count == 0)
					await WelcomeMessage.CreateReactionAsync(FemaleEmoji).ConfigureAwait(false);
			}
		}

		async Task VoiceChannelsInitAsync() {
			UsersInLobbies.Clear();
			DateVoiceLobbies.ForEach(l => UsersInLobbies.AddRange(l.Users));
			await CombLobbies().ConfigureAwait(false);
			PairsInSecretRooms.Clear();
			foreach (var r in SecretRooms) {
				if (r.Users.Count() == 0) await r.DeleteAsync().ConfigureAwait(false);
				else {
					AllUserStates.TryGetValue(r.Users.First().Id, out var uState);
					var pair = new PairInSecretRoom() { SecretRoom = r, Users = r.Users.ToList() };
					if (uState != null && uState.EnteredPrivateRoomTime.HasValue) pair.Timeout = uState.EnteredPrivateRoomTime.Value.AddMilliseconds(SecretRoomTime);
					else pair.Timeout = DateTime.Now;
					PairsInSecretRooms.Add(pair);
				}
			}
		}

		private IEnumerable<DiscordChannel> GetVoiceLobbies() {
			return DateRootCategory.Children.Where(c => c.Type == ChannelType.Voice && c.Name.ToLower().Contains("lobby"));
		}

		private IEnumerable<DiscordChannel> GetSecretRooms() {
			return DateRootCategory.Children.Where(c => c.Type == ChannelType.Voice && c.Name.ToLower().Contains("secret"));
		}

		private async Task AddLastEmptyVoiceLobby() {
			var c = await Guild.CreateChannelAsync($"Date Voice Lobby {DateVoiceLobbies.Count}", ChannelType.Voice, DateRootCategory,
				overwrites: new List<DiscordOverwriteBuilder>() {
					new DiscordOverwriteBuilder() {  Allowed = Permissions.AccessChannels }.For(Guild.EveryoneRole)
				});
			//await c.AddOverwriteAsync(Guild.EveryoneRole, Permissions.AccessChannels);
			DateVoiceLobbies.Add(c);
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

		/// <summary>
		/// User changed state in voice lobbies (joined/left)
		/// </summary>
		/// <param name="e"></param>
		/// <returns></returns>
		public async Task VoiceStateUpdated(VoiceStateUpdateEventArgs e) {
			//TODO test
			InitTask?.Wait();

			if (e.After?.Channel != e.Before?.Channel) { //Channel changed
				var beforeInLobbies = DateVoiceLobbies.Contains(e.Before?.Channel);
				var afterInLobbies = DateVoiceLobbies.Contains(e.After?.Channel);
				var beforeInSecretRooms = SecretRooms.Contains(e.Before?.Channel);
				var afterInSecretRooms = SecretRooms.Contains(e.After?.Channel);

				if (!beforeInLobbies && !beforeInSecretRooms && afterInLobbies) {
					//User connected to lobbies
					if (!UsersInLobbies.Contains(e.User)) UsersInLobbies.Add(e.User);
					if (!AllUserStates.ContainsKey(e.User.Id))
						AllUserStates.Add(e.User.Id, new UserState() { UserId = e.User.Id, LastEnteredLobbyTime = DateTime.Now });

				} else if ((beforeInLobbies || beforeInSecretRooms) && !afterInLobbies && !afterInSecretRooms) {
					//User left activity
					if (beforeInLobbies) {
						UsersInLobbies.Remove(e.User);
						await CombLobbies();
					}
					//remove disband secret room if one left TODO check logic
					if (beforeInSecretRooms)
						if (e.Before.Channel.Users.Count() == 0)
							await e.Before.Channel.DeleteAsync().ConfigureAwait(false);
						else if (e.Before.Channel.Users.Count() == 1) {
							var moveMember = await Guild.GetMemberAsync(e.Before.Channel.Users.First().Id);
							await DateVoiceLobbies[0].PlaceMemberAsync(moveMember);
						}

				} else if (beforeInLobbies && afterInLobbies) {
					//User switched Lobbies
					await CombLobbies();

				} else if (beforeInLobbies && afterInSecretRooms) {
					//Moved to secret room
					UsersInLobbies.Remove(e.User);

				} else if (beforeInSecretRooms && afterInLobbies) {
					//Returned from secret room
					if (!UsersInLobbies.Contains(e.User)) UsersInLobbies.Add(e.User);
					if (e.Before.Channel.Users.Count() == 0)
						try {
							await e.Before.Channel.DeleteAsync().ConfigureAwait(false);
						} catch (Exception) { }
					else if (e.Before.Channel.Users.Count() == 1) {
						var moveMember = await Guild.GetMemberAsync(e.Before.Channel.Users.First().Id);
						await DateVoiceLobbies[0].PlaceMemberAsync(moveMember);
					}

				}
			}
		}

		private async Task CombLobbies() {
			//Comb lobbies
			DateVoiceLobbies.Clear();
			DateVoiceLobbies.AddRange(GetVoiceLobbies());
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
				await AddLastEmptyVoiceLobby();
			}
		}

		internal async Task MessageReactionAdded(MessageReactionAddEventArgs e) {
			if (e.Message.Id == WelcomeMessage.Id && (e.Emoji.Id == MaleEmoji.Id || e.Emoji.Id == FemaleEmoji.Id)) {
				ApplyGenderReactions(e.User, e.Emoji);
			} else
				await e.Message.DeleteReactionsEmojiAsync(e.Emoji).ConfigureAwait(false);
		}
	}
}