using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using ZBot.DialogFramework;
using DateBot.Base.Match;
using ZBot;

namespace DateBot.Base {
	/// <summary>
	/// Guild task
	/// </summary>
	public partial class DateBotGuildTask {
		public IDateBotGuildState State { get; }
		public List<PairInSecretRoom> PairsInSecretRooms { get; private set; } = new List<PairInSecretRoom>();

		public DateBotGuildTask(IDateBotGuildState state) {
			State = state;
		}

		/// <summary>
		/// Used to create secret rooms
		/// </summary>
		public DiscordOverwriteBuilder SecretRoomOverwriteBuilder { get; private set; }

		/// <summary>
		/// Set true if activity is on, in this guild
		/// </summary>
		public bool Active { get { return State.Active; } private set { State.Active = value; } }
		public Task InitTask { get; private set; }
		public Task CurrentCombLobbiesTask { get; private set; }
		public Task MatchingTask { get; private set; }

		/// <summary>
		/// Start initialization
		/// </summary>
		/// <returns></returns>
		public async Task Initialize(DiscordClient c) {
			//Cancel if is initializing
			if (InitTask != null) { return; }

			InitTask = _initialize(c);
			await InitTask.ContinueWith(t => { 
				InitTask = null;
			}).ConfigureAwait(false);
		}

		private async Task _initialize(DiscordClient c) {
			c.Guilds.TryGetValue(State.GuildId, out _guild);
			Guild = _guild;

			if (State.MaleRoleId != 0)
				MaleRole = Guild.GetRole(State.MaleRoleId);
			if (State.FemaleRoleId != 0)
				FemaleRole = Guild.GetRole(State.FemaleRoleId);

			SecretRoomOverwriteBuilder = new DiscordOverwriteBuilder();
			SecretRoomOverwriteBuilder.For(Guild.EveryoneRole);
			SecretRoomOverwriteBuilder.Deny(Permissions.AccessChannels);

			//Init RootCategory
			Guild.Channels.TryGetValue(State.DateCategoryId, out DiscordChannel cat);
			if (cat == null) {
				string m = $"Initialize failed. No Category found with id {State.DateCategoryId}";
				throw new Exception(m);
			}
			_dateCategory = cat;

			//Init SecretCategory
			Guild.Channels.TryGetValue(State.DateCategoryId, out DiscordChannel scat);
			if (scat == null) {
				string m = $"Initialize failed. No Category found with id {State.DateCategoryId}";
				throw new Exception(m);
			}
			_dateSecretCategory = scat;

			//VoiceChannels
			await VoiceChannelsInitAsync().ConfigureAwait(false);
			if (DateVoiceLobbies.Count == 0 || DateVoiceLobbies.All(c => c.Users.Count() > 0)) await AddLastEmptyVoiceLobby().ConfigureAwait(false);

			//GetEmojis
			MaleEmoji = Emoji.GetEmojiFromText(DateBot.Instance.Client, State.MaleEmojiId);
			FemaleEmoji = Emoji.GetEmojiFromText(DateBot.Instance.Client, State.FemaleEmojiId);
			OptionEmojis.Clear();
			OptionEmojis.AddRange(State.OptionEmojiIds.Select(id => Emoji.GetEmojiFromText(DateBot.Instance.Client, id)));
			LikeEmoji = Emoji.GetEmojiFromText(DateBot.Instance.Client, State.LikeEmojiId);
			DislikeEmoji = Emoji.GetEmojiFromText(DateBot.Instance.Client, State.DisLikeEmojiId);
			TimeEmoji = Emoji.GetEmojiFromText(DateBot.Instance.Client, State.TimeEmojiId);
			CancelLikeEmoji = Emoji.GetEmojiFromText(DateBot.Instance.Client, State.CancelLikeEmojiId);

			//Check and add users in lobbies
			foreach (var u in UsersInLobbies.ToArray()) {
				AddOrGetUserState(u).LastEnteredLobbyTime = DateTime.Now;
			}

			//Init messages in text channel
			DateTextChannel = Guild.GetChannel(State.DateTextChannelId);
			await WelcomeMessageInit().ConfigureAwait(false);
			_ = PrivateControlsMessageInit().ConfigureAwait(false);

			if (UsersInLobbies.Count > 0) _ = TryStartMatchingTask();
		}

		/// <summary>
		/// Gets or creates welcome message and refreshes reaction emojis
		/// </summary>
		/// <returns></returns>
		public async Task WelcomeMessageInit() {
			//What to do with previously created question, that still is monitoring answers?
			var existingWelcomeMessage = DateTextChannel.GetMessageAsync(State.WelcomeMessageId);
			//TODO Would be nice to have an override for one action with many emojis, passing emoji in for simplicity
			var answers = new Answer[2 + OptionEmojis.Count()];
			answers[0] = new Answer(MaleEmoji, e => {
				//TODO doesn't add those that are not yet in activity

				UserState uState = AddOrGetUserState(e.User);
				uState.Gender = GenderEnum.Male;
				uState.AgeOptions = 0;
				if (MaleRole != null || FemaleRole != null) {
					var member = Guild.GetMemberAsync(uState.UserId).Result;
					if (MaleRole != null)
						member.GrantRoleAsync(MaleRole);
					if (FemaleRole != null)
						member.RevokeRoleAsync(FemaleRole);
				}
			});
			answers[1] = new Answer(FemaleEmoji, e => {
				UserState uState = AddOrGetUserState(e.User);
				uState.Gender = GenderEnum.Female;
				uState.AgeOptions = 0;
				if (MaleRole != null || FemaleRole != null) {
					var member = Guild.GetMemberAsync(uState.UserId).Result;
					if (MaleRole != null)
						member.RevokeRoleAsync(MaleRole);
					if (FemaleRole != null)
						member.GrantRoleAsync(FemaleRole);
				}
			});
			for (int i = 2; i < answers.Length; i++) {
				var index = i;
				answers[index] = new Answer(OptionEmojis[index - 2], e => {
					UserState uState = AddOrGetUserState(e.User);
					var option = 1 << (index - 2);
					if (uState.Gender == GenderEnum.Female)
						uState.AgeOptions ^= option; //toggle age group
					else
						uState.AgeOptions = option;
				});
			}

			Task.WaitAny(new Task[] { existingWelcomeMessage, Task.Delay(5000) });
			WelcomeMessage = await DialogFramework.CreateQuestion(DateTextChannel, State.WelcomeMessageBody, answers,
				existingMessage: !existingWelcomeMessage.IsFaulted ? existingWelcomeMessage.Result : null,
				behavior: MessageBehavior.Permanent, deleteAnswer: true, deleteAnswerTimeout: TimeSpan.Zero);
		}

		public async Task PrivateControlsMessageInit() {
			var existingControlsMessage = DateTextChannel.GetMessageAsync(State.PrivateControlsMessageId);
			var answers = new Answer[] {
				new Answer(LikeEmoji, e=>{
					_ = ApplyPrivateReactionsAsync(e.User, LikeEmoji).ConfigureAwait(false);
				}),
				new Answer(CancelLikeEmoji, e=>{
					_ = ApplyPrivateReactionsAsync(e.User, CancelLikeEmoji).ConfigureAwait(false);
				}),
				new Answer(DislikeEmoji, e=>{
					_ = ApplyPrivateReactionsAsync(e.User, DislikeEmoji).ConfigureAwait(false);
				}),
				new Answer(TimeEmoji, e=>{
					_ = ApplyPrivateReactionsAsync(e.User, TimeEmoji).ConfigureAwait(false);
				})
			};

			Task.WaitAny(new Task[] { existingControlsMessage, Task.Delay(5000) });
			PrivateControlsMessage = await DialogFramework.CreateQuestion(DateTextChannel, State.PrivateMessageBody, answers,
				existingMessage: !existingControlsMessage.IsFaulted ? existingControlsMessage.Result : null,
				behavior: MessageBehavior.Permanent, deleteAnswer: true, deleteAnswerTimeout: TimeSpan.Zero).ConfigureAwait(false);
		}

		private async Task ApplyPrivateReactionsAsync(DiscordUser u, DiscordEmoji emoji) {
			UserState uState = AddOrGetUserState(u);
			var pair = PairsInSecretRooms.FirstOrDefault(p => p.Users.Contains(u));
			if (pair != null) {
				var users = pair.Users;
				var user = users.FirstOrDefault(x => x.Id == u.Id);
				var mate = users.FirstOrDefault(x => x.Id != u.Id);
				if (mate != null) {
					UserState mateState = AddOrGetUserState(mate);
					if (emoji == LikeEmoji) {
						if (!uState.LikedUserIds.Contains(mate.Id))
							uState.LikedUserIds.Add(mate.Id);
						uState.DislikedUserIds.Remove(mate.Id);
						if (mateState.LikedUserIds.Contains(u.Id)) {
							//Mutual like
							var userDmc = await user.CreateDmChannelAsync();
							var mateDmc = await mate.CreateDmChannelAsync();
							var emb = new DiscordEmbedBuilder();

							emb.WithTitle(string.Format(State.DMLikeMessageTitle, mate.Mention));
							emb.WithColor(DiscordColor.Lilac);
							emb.WithDescription(string.Format(State.DMLikeMessage, mate.Mention));
							emb.WithImageUrl(mate.AvatarUrl);
							await userDmc.SendMessageAsync(embed: emb.Build());

							emb.WithTitle(string.Format(State.DMLikeMessageTitle, user.Mention));
							emb.WithDescription(string.Format(State.DMLikeMessage, user.Mention));
							emb.WithImageUrl(user.AvatarUrl);
							await mateDmc.SendMessageAsync(embed: emb.Build());
						}
					} else if (emoji == TimeEmoji) {
						uState.AddTime = true;
						if (mateState.AddTime) {
							uState.EnteredPrivateRoomTime = uState.EnteredPrivateRoomTime.Value.AddMilliseconds(State.SecretRoomTime);
						}

					} else if (emoji == CancelLikeEmoji) {
						//cancel like/dislike
						uState.LikedUserIds.Remove(mate.Id);
						uState.DislikedUserIds.Remove(mate.Id);
					} else if (emoji == DislikeEmoji) {
						if (!uState.DislikedUserIds.Contains(mate.Id))
							uState.DislikedUserIds.Add(mate.Id);
						uState.LikedUserIds.Remove(mate.Id);
					}
				}
			}
		}

		private IEnumerable<DiscordChannel> GetVoiceLobbies(bool refreshRoot = false) {
			if (refreshRoot) DateLobbyCategory = Guild.GetChannel(State.DateCategoryId);
			return DateLobbyCategory.Children.Where(c => c.Type == ChannelType.Voice && c.Name.ToLower().Contains("lobby"));
		}

		private IEnumerable<DiscordChannel> GetSecretRooms(bool refreshRoot = false) {
			if (refreshRoot) DateSecretCategory = Guild.GetChannel(State.DateSecretCategoryId);
			return DateSecretCategory.Children.Where(c => c.Type == ChannelType.Voice && c.Name.ToLower().Contains("secret"));
		}
		private async Task AddLastEmptyVoiceLobby() {
			var c = await Guild.CreateChannelAsync($"Date Voice Lobby {DateVoiceLobbies.Count}", ChannelType.Voice, DateLobbyCategory,
				overwrites: new List<DiscordOverwriteBuilder>() {
					new DiscordOverwriteBuilder() {  Allowed = Permissions.AccessChannels }.For(Guild.EveryoneRole)
				});
			//await c.AddOverwriteAsync(Guild.EveryoneRole, Permissions.AccessChannels);
			DateVoiceLobbies.Add(c);
		}
		private void RefreshUsersInLobbies() {
			var users = new List<DiscordUser>();
			DateVoiceLobbies.ForEach(l => users.AddRange(l.Users));
			UsersInLobbies = users;
		}

		private async Task TryCombLobbies() {
			if (CurrentCombLobbiesTask == null) {
				await (CurrentCombLobbiesTask = CombLobbies().ContinueWith(t => {
					CurrentCombLobbiesTask = null;
				})).ConfigureAwait(false);
			}
		}
		private async Task CombLobbies() {
			//Do we really need this?
			//await Task.Delay(1000);
			//Comb lobbies
			DateVoiceLobbies.Clear();
			DateVoiceLobbies.AddRange(GetVoiceLobbies(true));
			DateVoiceLobbies.Where(l => l.Id != DateVoiceLobbies.Last().Id && l.Users.Count() == 0)
				.ToList()
				.ForEach(async l => {
					DateVoiceLobbies.Remove(l);
					try {
						if (Guild.Channels.TryGetValue(l.Id, out var channel))
							await channel.DeleteAsync();
					} catch (Exception e) { }
				});
			int i = 0;
			foreach (var l in DateVoiceLobbies.ToArray()) {
				await l.ModifyAsync(c => c.Name = $"Date Voice Lobby {i++}");
			}
			if (DateVoiceLobbies.Count == 0 || DateVoiceLobbies.Last().Users.Count() > 0) {
				//Add empty at the end
				await AddLastEmptyVoiceLobby();
			}

			CurrentCombLobbiesTask = null;
		}
		private void ReturnUserToFirstLobbyAvailable(DiscordMember p) {

			_ = DateVoiceLobbies.FirstOrDefault(l => l.UserLimit == 0 || l.UserLimit < l.Users.Count())
				.PlaceMemberAsync(p).ConfigureAwait(false);
		}
		private UserState AddOrGetUserState(DiscordUser User) {
			State.AllUserStates.TryGetValue(User.Id, out var uState);
			if (uState == null) {
				uState = new UserState() { UserId = User.Id };
				State.AllUserStates.Add(User.Id, uState);
				//Add gender based on role, if present
				if (MaleRole != null || FemaleRole != null) {
					var member = Guild.GetMemberAsync(uState.UserId).Result;
					if (MaleRole != null && member.Roles.Contains(MaleRole))
						uState.Gender = GenderEnum.Male;
					if (FemaleRole != null && member.Roles.Contains(FemaleRole))
						uState.Gender = GenderEnum.Female;
				}
			}
			return uState;
		}


		/// <summary>
		/// Checks lobbies, cleans them, refreshes users, checks secret rooms updates timeouts, cleans them
		/// TODO change secret room behaviour
		/// </summary>
		/// <returns></returns>
		async Task VoiceChannelsInitAsync() {
			//Clean lobbies
			_ = TryCombLobbies();
			//check users in lobbies
			RefreshUsersInLobbies();
			//check secret rooms
			SecretRooms = GetSecretRooms().ToList();
			PairsInSecretRooms.Clear();
			foreach (var r in SecretRooms.ToArray()) {
				if (r.Users.Count() > 1) {
					var uState = AddOrGetUserState(r.Users.First());
					var pair = new PairInSecretRoom() { SecretRoom = r, Users = r.Users.ToList() };
					//Try get timeout
					if (uState != null && uState.EnteredPrivateRoomTime.HasValue) pair.Timeout = uState.EnteredPrivateRoomTime.Value.AddMilliseconds(State.SecretRoomTime);
					else pair.Timeout = DateTime.Now;
					PairsInSecretRooms.Add(pair);
					//_ = TimeoutDisband(pair)
						//.ConfigureAwait(false);
				} else {
					SecretRooms.Remove(r);
					try {
						await r.DeleteAsync().ConfigureAwait(false);
					} catch (Exception) { }
				}
			}
		}

		internal void StartActivity() {
			Active = true;
			_ = TryStartMatchingTask();
		}

		internal void StopActivity() {
			Active = false;
		}

		internal async Task VoiceStateUpdated(VoiceStateUpdateEventArgs e) {
			Console.WriteLine("VoiceStateUpdated awaits InitTask");
			InitTask?.Wait();
			Console.WriteLine("VoiceStateUpdated resumed");


			if (e.After?.Channel != e.Before?.Channel) { //Channel changed
				var beforeInLobbies = DateVoiceLobbies.Contains(e.Before?.Channel);
				var afterInLobbies = DateVoiceLobbies.Contains(e.After?.Channel);
				var beforeInSecretRooms = SecretRooms.Contains(e.Before?.Channel);
				var afterInSecretRooms = SecretRooms.Contains(e.After?.Channel);

				if (!beforeInLobbies && !beforeInSecretRooms && afterInLobbies) {
					//User connected to lobbies
					Console.WriteLine($"User {e.User} connected to {e.Channel} ");
					userConnected_AddToActivity(e.User);
					//Start matching session
					_ = TryStartMatchingTask();

				} else if ((beforeInLobbies || beforeInSecretRooms) && !afterInLobbies && !afterInSecretRooms) {
					//User left activity
					Console.WriteLine($"User {e.User} left activity ");
					if (beforeInLobbies) {
						UsersInLobbies.Remove(e.User);
						removeStateFor(e.User);
					}
					//remove disband secret room if one left TODO check logic
					if (beforeInSecretRooms) {
						removeStateFor(e.User);
						//DebugLogWrite($"trying to disbnad {e.Before.Channel}... ");
						await disbandRemoveSecretRoom(e.Before.Channel).ConfigureAwait(false);
						//DebugLogWrite($"{e.Before.Channel} removed/disbanded");
					}

				} else if (beforeInLobbies && afterInLobbies) {
					Console.WriteLine($"User {e.User} switched lobbies ");
					//User switched Lobbies

				} else if (beforeInLobbies && afterInSecretRooms) {
					Console.WriteLine($"User {e.User} moved to {e.After.Channel} ");
					//Moved to secret room
					UsersInLobbies.Remove(e.User);

				} else if (beforeInSecretRooms && afterInLobbies) {
					//Returned from secret room
					removeStateFor(e.User);
					Console.WriteLine($"User {e.User} returned to lobby, trying to disbnad {e.Before.Channel}... ");
					userConnected_AddToActivity(e.User);
					await disbandRemoveSecretRoom(e.Before.Channel).ConfigureAwait(false);

					//Start matching session
					_ = TryStartMatchingTask();
				}

				_ = TryCombLobbies();
			}
			async Task disbandRemoveSecretRoom(DiscordChannel channel) {
				//Remove empty room, if 1 user left, move him into lobby 0, later room will be removed
				if (channel.Users.Count() == 0)
					try { //usually shouldn't throw, but in debug
						await channel.DeleteAsync().ConfigureAwait(false);
					} catch {
						//DebugLogWrite($"Exception while deleting ");
					}
				else if (channel.Users.Count() == 1) {
					try {
						var moveMember = await Guild.GetMemberAsync(channel.Users.First().Id);
						await DateVoiceLobbies[0].PlaceMemberAsync(moveMember);
					} catch {
						//DebugLogWrite($"Exception while moving member ");
					}
				}
			}
			void userConnected_AddToActivity(DiscordUser User) {
				AddOrGetUserState(User).LastEnteredLobbyTime = DateTime.Now;
			}
			void removeStateFor(DiscordUser User) {
				var uState = AddOrGetUserState(User);
				uState.EnteredPrivateRoomTime = null;
				PairsInSecretRooms.RemoveAll(p => p.Users.Contains(User));
			}
		}

		private async Task TryStartMatchingTask(bool force = false) {
			if (MatchingTask == null || force) {
				await (MatchingTask = _prepareMatching().ContinueWith(t => {
					MatchingTask = null;
				})).ConfigureAwait(false);
			}
		}

		private async Task _prepareMatching() {
			Console.WriteLine("_match waits for delay");
			await Task.Delay(30000);
			if (!Active) {
				Console.WriteLine("_match canceled. Active is false");
				return;
			}

			Console.WriteLine("MatchingTask Matching");
			_refreshDateVoiceLobbies();
			RefreshUsersInLobbies();

			//Match
			MatchUsersInLobbies();

			//Removeitself or restart
			_refreshDateVoiceLobbies();
			RefreshUsersInLobbies();
			if (UsersInLobbies.Count > 4) {
				Console.WriteLine("Try restart MatchingTask");
				//Restart? if not same gender
				_ = TryStartMatchingTask(true);
			}

			void _refreshDateVoiceLobbies() {
				DateVoiceLobbies.Clear();
				DateVoiceLobbies.AddRange(GetVoiceLobbies(true));
			}
		}

		private void MatchUsersInLobbies() {
			if (UsersInLobbies.Count == 0) return;

			var participants = UsersInLobbies.Select(u=> (DiscortUser: u, State: State.AllUserStates[u.Id]));
			var matches = Matcher.MatchUsers(
				participants.Where(u => u.State.Gender == GenderEnum.Male).Select(u =>
					new MatchUser(
						u.State.UserId,
						u.State.LikedUserIds.ToArray(),
						u.State.DislikedUserIds.ToArray(),
						u.State.AgeOptions,
						u.State.LastMatches.ToArray()
						)),
				participants.Where(u => u.State.Gender == GenderEnum.Female).Select(u =>
				new MatchUser(
					u.State.UserId,
					u.State.LikedUserIds.ToArray(),
					u.State.DislikedUserIds.ToArray(),
					u.State.AgeOptions,
					u.State.LastMatches.ToArray()
					)));

			foreach (var match in matches) {
				MoveToPrivateLobbyAsync(participants.FirstOrDefault(p=>p.DiscortUser.Id == match[0].Id),
					participants.FirstOrDefault(p=>p.DiscortUser.Id == match[1].Id))
					.ContinueWith(t => TimeoutDisband(t.Result))
					.ConfigureAwait(false);
			}
		}

		private async Task<PairInSecretRoom> MoveToPrivateLobbyAsync((DiscordUser DiscortUser, UserState State) a, (DiscordUser DiscortUser, UserState State) b) {

			UsersInLobbies.Remove(a.DiscortUser);
			UsersInLobbies.Remove(b.DiscortUser);

			//Create new private room
			var privateRoom = await Guild.CreateChannelAsync($"Secret Room {Guid.NewGuid().ToString()}", ChannelType.Voice, DateSecretCategory
				, overwrites: new DiscordOverwriteBuilder[] { SecretRoomOverwriteBuilder });
			SecretRooms.Add(privateRoom);
			Console.WriteLine("room created... ");

			
			var timeout = DateTime.Now.AddMilliseconds(State.SecretRoomTime);
			//In case of restarting bot will be able to pick things up from this data
			a.State.EnteredPrivateRoomTime = b.State.EnteredPrivateRoomTime = timeout;
			var amember = await Guild.GetMemberAsync(a.State.UserId);
			var bmember = await Guild.GetMemberAsync(b.State.UserId);
			var p = new PairInSecretRoom() {
				Users = new List<DiscordMember>() { amember, bmember },
				SecretRoom = privateRoom,
				Timeout = timeout
			};

			//register pair for controls message reactions
			PairsInSecretRooms.Add(p);

			Console.WriteLine("moving... ");
			_ = privateRoom.PlaceMemberAsync(amember).ConfigureAwait(false);
			_ = privateRoom.PlaceMemberAsync(bmember).ConfigureAwait(false);


			a.State.AddMatch(b.State.UserId);
			b.State.AddMatch(a.State.UserId);

			Console.WriteLine("finished");

			return p;
		}
		private async Task TimeoutDisband(PairInSecretRoom pair) {
			DateTime? timeout = _updateTimeout(pair.Users);
			if (pair.Users.Count != 2) return;
			do {
				Console.WriteLine($" Now: {DateTime.Now} Timeout: {timeout} secrettime: {TimeSpan.FromMilliseconds(State.SecretRoomTime)}");

				await Task.Delay(Math.Max((int)(timeout.Value - DateTime.Now).TotalMilliseconds - 61000, 0));
				if (!PairsInSecretRooms.Contains(pair)) return; //quit if disbanded outside

				timeout = _updateTimeout(pair.Users);
				foreach (var p in pair.Users) {
					try {
						_ = p.SendMessageAsync(
							$"{(timeout.Value - DateTime.Now).TotalMinutes.ToString("G2")} min left for" +
							$" {string.Join(", ", pair.Users.Select(p => p.DisplayName))}");
					} catch (Exception e) { 
						Console.WriteLine(e.Message);
					}
				}

				await Task.Delay(Math.Max((int)(timeout.Value - DateTime.Now).TotalMilliseconds + 100, 0));

				if (!PairsInSecretRooms.Contains(pair)) return; //quit if disbanded outside
				timeout = _updateTimeout(pair.Users);
				if (DateTime.Now < timeout.Value)
					foreach (var p in pair.Users) {
						_ = p.SendMessageAsync(
							$"{(timeout.Value - DateTime.Now).TotalMinutes.ToString("G2")} min left for" +
							$" {string.Join(", ", pair.Users.Select(p => p.DisplayName))}").ConfigureAwait(false);
					}
			} while (DateTime.Now < timeout.Value);

			//return if timer off
			//else
			Console.WriteLine($"Timeout Disbanding {pair.Users[0]} and {pair.Users[1]}");
			foreach (var p in pair.Users.ToArray()) {
				//Return participants to lobby 0
				ReturnUserToFirstLobbyAvailable(p);
			}

			DateTime _updateTimeout(List<DiscordMember> Users) {
				try {
					var uStateA = AddOrGetUserState(Users[0]);
					var uStateB = AddOrGetUserState(Users[1]);

					return (uStateA.EnteredPrivateRoomTime < uStateB.EnteredPrivateRoomTime ? uStateA.EnteredPrivateRoomTime : uStateB.EnteredPrivateRoomTime).Value;
				} catch (Exception) { }
				return DateTime.Now;
			}
		}

	}
}
