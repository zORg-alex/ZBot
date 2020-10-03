using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls.Primitives;
using System.Xaml.Schema;
using DiscordRPC;
using DiscordRPC.Message;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Newtonsoft.Json;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Information;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Math;
using zLib;
using Timer = System.Timers.Timer;

namespace DateBot.Base {
	[DataContract]
	public partial class GuildTask : GuildConfig {

		public DiscordGuild Guild { get; set; }
		public DiscordChannel LogChannel { get; set; }
		public DiscordMessage LogMessage { get; set; }
		public DiscordChannel DateRootCategory { get; set; }
		public DiscordChannel DateSecretCategory { get; set; }
		public DiscordChannel DateLobby { get; internal set; }
		public DiscordMessage WelcomeMessage { get; private set; }
		public DiscordMessage PrivateControlsMessage { get; private set; }
		public List<DiscordChannel> DateVoiceLobbies { get; set; } = new List<DiscordChannel>();
		public List<DiscordUser> UsersInLobbies { get; set; } = new List<DiscordUser>();
		private List<DiscordChannel> SecretRooms { get; set; } = new List<DiscordChannel>();
		private List<PairInSecretRoom> PairsInSecretRooms { get; set; } = new List<PairInSecretRoom>();

		public DiscordEmoji MaleEmoji { get; set; }
		public DiscordEmoji FemaleEmoji { get; set; }
		public List<DiscordEmoji> OptionEmojis { get; } = new List<DiscordEmoji>();
		public DiscordEmoji LikeEmoji { get; set; }
		public DiscordEmoji DisLikeEmoji { get; set; }
		public DiscordEmoji CancelLikeEmoji { get; set; }
		public DiscordEmoji TimeEmoji { get; set; }

		public void DebugLogWriteLine(string message) {
			Console.Write($"\n{message}");
			//LogMessage?.ModifyAsync(LogMessage.Content + '\n' + message);
		}
		public void DebugLogWrite(string message) {
			Console.Write(message);
			//LogMessage?.ModifyAsync(LogMessage.Content + message );
		}

		DiscordOverwriteBuilder SecretRoomOverwriteBuilder { get; set; }
		//DiscordOverwriteBuilder PersonalChannelOverwriteBuilder { get; set; }
		public Timer UpdateTimer { get; private set; }
		public bool Initialized { get; private set; }
		public Task InitTask { get; internal set; }
		public Task CurrentMatchingTask { get; private set; }
		public Task CurrentCombLobbiesTask { get; private set; }

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
			//DebugLogWriteLine($"Initiating {guild} GuildTask... ");
			//Init guild
			Guild = guild;
			GuildId = guild.Id;
			SecretRoomOverwriteBuilder = new DiscordOverwriteBuilder();
			SecretRoomOverwriteBuilder.For(Guild.EveryoneRole);
			SecretRoomOverwriteBuilder.Deny(Permissions.AccessChannels);
			//PersonalChannelOverwriteBuilder = new DiscordOverwriteBuilder();
			//PersonalChannelOverwriteBuilder.Allow(Permissions.AccessChannels);

			LogChannel = guild.GetChannel(LogChannelId);
			if (LogChannel != null)
				LogMessage = await LogChannel.SendMessageAsync($"Begin of log {DateTime.Now.ToString()}\n");

			//Init RootCategory
			Guild.Channels.TryGetValue(DateCategoryId, out DiscordChannel cat);
			if (cat == null) {
				string m = $"Initialize failed. No Category found on + {DateCategoryId}";
				//DebugLogWriteLine(m);
				throw new Exception(m);
			}
			DateRootCategory = cat;
			Guild.Channels.TryGetValue(DateSecretCategoryId, out DiscordChannel secCat);
			DateSecretCategory = secCat;

			//DebugLogWrite("Initiating voice channels... ");
			//VoiceChannels
			DateVoiceLobbies.Clear();
			SecretRooms.Clear();
			DateVoiceLobbies.AddRange(GetVoiceLobbies());
			SecretRooms.AddRange(GetSecretRooms());
			await VoiceChannelsInitAsync().ConfigureAwait(false);
			if (DateVoiceLobbies.Count == 0 || DateVoiceLobbies.All(c => c.Users.Count() > 0)) await AddLastEmptyVoiceLobby().ConfigureAwait(false);

			//DebugLogWrite("Initiating Emojis... ");
			//GetEmojis
			MaleEmoji = DiscordEmoji.FromName(DateBot.Instance.Client, MaleEmojiId);
			FemaleEmoji = DiscordEmoji.FromName(DateBot.Instance.Client, FemaleEmojiId);
			OptionEmojis.Clear();
			OptionEmojis.AddRange(OptionEmojiIds.Select(id=>DiscordEmoji.FromName(DateBot.Instance.Client, id)));
			LikeEmoji = DiscordEmoji.FromName(DateBot.Instance.Client, LikeEmojiId);
			DisLikeEmoji = DiscordEmoji.FromName(DateBot.Instance.Client, DisLikeEmojiId);
			TimeEmoji = DiscordEmoji.FromName(DateBot.Instance.Client, TimeEmojiId);
			CancelLikeEmoji = DiscordEmoji.FromName(DateBot.Instance.Client, CancelLikeEmojiId);

			//DebugLogWrite("Initiating Users in lobbies... ");
			//Check and add users in lobbies
			foreach (var u in UsersInLobbies.ToArray()) {
				if (!AllUserStates.ContainsKey(u.Id)) {
					AllUserStates.Add(u.Id, new UserState() { UserId = u.Id, LastEnteredLobbyTime = DateTime.Now });
				}
			}

			//DebugLogWrite("Initiating Welcome message... ");
			//Check for welcome message TODO add option emojis
			DateLobby = DateRootCategory.Children.FirstOrDefault(c => c.Id == DateLobbyId);
			await WelcomeMessageInit().ConfigureAwait(false);
			_ = PrivateControlsMessageInit().ConfigureAwait(false);

			//UpdateTimer = new Timer(TimeSpan.FromSeconds(30).TotalMilliseconds) { AutoReset = true };
			//UpdateTimer.Elapsed += UpdateTimer_Elapsed;
			//UpdateTimer.Start();

			//DebugLogWrite("finished");
			InitTask = null;
			Initialized = true;

			if (UsersInLobbies.Count > 0) TryStartMatchingTask();
		}

		async Task PrivateControlsMessageInit() {
			try {
				PrivateControlsMessage = await DateLobby.GetMessageAsync(PrivateControlsMessageId);
			} catch (Exception) { }
			if (PrivateControlsMessage == null) {
				//Add welcome message and default reactions
				PrivateControlsMessage = await DateLobby.SendMessageAsync(PrivateMessageBody);
				PrivateControlsMessageId = PrivateControlsMessage.Id;
				_ = applyDefaultReactions().ConfigureAwait(false);
				_ = PrivateControlsMessage.PinAsync().ConfigureAwait(false);
			} else {
				var pc = DateLobby.GetMessageAsync(PrivateControlsMessageId).Result;
				foreach (var r in pc.Reactions.ToArray()) {
					if (r.Emoji == LikeEmoji || r.Emoji == DisLikeEmoji || r.Emoji == CancelLikeEmoji) {
						foreach (var u in pc.GetReactionsAsync(r.Emoji).Result.Where(r => r.Id != DateBot.Instance.BotId)) {
							_ = PrivateControlsMessage.DeleteReactionAsync(r.Emoji, u).ConfigureAwait(false);
							_ = ApplyPrivateReactionsAsync(u, r.Emoji).ConfigureAwait(false);
						}
					} else
						await PrivateControlsMessage.DeleteReactionsEmojiAsync(r.Emoji).ConfigureAwait(false);
				}
				_ = applyDefaultReactions().ConfigureAwait(false);
			}

			async Task applyDefaultReactions() {
				await PrivateControlsMessage.CreateReactionAsync(LikeEmoji).ConfigureAwait(false);
				await PrivateControlsMessage.CreateReactionAsync(CancelLikeEmoji).ConfigureAwait(false);
				await PrivateControlsMessage.CreateReactionAsync(DisLikeEmoji).ConfigureAwait(false);
				await PrivateControlsMessage.CreateReactionAsync(TimeEmoji).ConfigureAwait(false);
			}
		}

		private async Task ApplyPrivateReactionsAsync(DiscordUser u, DiscordEmoji emoji) {
			AllUserStates.TryGetValue(u.Id, out var uState);
			var pair = PairsInSecretRooms.FirstOrDefault(p => p.Users.Contains(u));
			if (pair != null) {
				var users = pair.Users;
				var user = users.FirstOrDefault(x => x.Id == u.Id);
				var mate = users.FirstOrDefault(x => x.Id != u.Id);
				if (mate != null) {
					AllUserStates.TryGetValue(mate.Id, out var mateState);
					if (emoji == LikeEmoji) {
						if (!uState.LikedUserIds.Contains(mate.Id))
							uState.LikedUserIds.Add(mate.Id);
						uState.DislikedUserIds.Remove(mate.Id);
						if (mateState.LikedUserIds.Contains(u.Id)) {
							//Mutual like
							//TODO arrange a couple a private channel
							var userDmc = await user.CreateDmChannelAsync();
							var mateDmc = await mate.CreateDmChannelAsync();
							var emb = new DiscordEmbedBuilder();

							emb.WithTitle(string.Format(DMLikeMessageTitle, mate.Mention));
							emb.WithColor(DiscordColor.Lilac);
							emb.WithDescription(string.Format(DMLikeMessage, mate.Mention));
							emb.WithImageUrl(mate.AvatarUrl);
							await userDmc.SendMessageAsync(embed: emb.Build());

							emb.WithTitle(string.Format(DMLikeMessageTitle, user.Mention));
							emb.WithDescription(string.Format(DMLikeMessage, user.Mention));
							emb.WithImageUrl(user.AvatarUrl);
							await mateDmc.SendMessageAsync(embed: emb.Build());
						}
					} else if (emoji == TimeEmoji) {
						uState.AddTime = true;
						if (mateState.AddTime) {
							uState.EnteredPrivateRoomTime = uState.EnteredPrivateRoomTime.Value.AddMilliseconds(SecretRoomTime);
						}

					} else if (emoji == CancelLikeEmoji) {
						//cancel like/dislike
						uState.LikedUserIds.Remove(mate.Id);
						uState.DislikedUserIds.Remove(mate.Id);
					} else if (emoji == DisLikeEmoji) {
						if (!uState.DislikedUserIds.Contains(mate.Id))
							uState.DislikedUserIds.Add(mate.Id);
						uState.LikedUserIds.Remove(mate.Id);
					}
				}
			}
		}

		//private void UpdateTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e) {

		//	//Return from lobbies that timed out
		//	foreach (var p in PairsInSecretRooms.Where(pp => pp.Timeout > DateTime.Now)) {
		//		Task.WaitAll(p.Users.Select(async u => await DateVoiceLobbies[0].PlaceMemberAsync(u).ConfigureAwait(false)).ToArray());
		//	}
		//	//Try match
		//	TryMatchUsers();
		//}

		/// <summary>
		/// Gets or creates welcome message and refreshes reaction emojis
		/// </summary>
		/// <returns></returns>
		async Task WelcomeMessageInit() {

			try {
				WelcomeMessage = await DateLobby.GetMessageAsync(WelcomeMessageId);
			} catch (Exception) { }
			if (WelcomeMessage == null) {
				//Add welcome message and default reactions
				WelcomeMessage = await DateLobby.SendMessageAsync(WelcomeMessageBody);
				WelcomeMessageId = WelcomeMessage.Id;
				_ = applyDefaultReactions().ConfigureAwait(false);
				_ = WelcomeMessage.PinAsync().ConfigureAwait(false);
			} else {
				var wm = DateLobby.GetMessageAsync(WelcomeMessageId).Result;
				foreach (var r in wm.Reactions.ToArray()) {
					if (r.Emoji == MaleEmoji || r.Emoji == FemaleEmoji || OptionEmojis.Contains(r.Emoji)) {
						foreach (var u in wm.GetReactionsAsync(r.Emoji).Result.Where(r => r.Id != DateBot.Instance.BotId)) {
							ApplyGenderAndOptionReactions(u, r.Emoji);
							_ = WelcomeMessage.DeleteReactionAsync(r.Emoji, u).ConfigureAwait(false);
						}
					} else
						await WelcomeMessage.DeleteReactionsEmojiAsync(r.Emoji).ConfigureAwait(false);
				}
				_ = applyDefaultReactions().ConfigureAwait(false);
			}

			async Task applyDefaultReactions() {
				await WelcomeMessage.CreateReactionAsync(MaleEmoji);
				await WelcomeMessage.CreateReactionAsync(FemaleEmoji);
				foreach (var optionEmoji in OptionEmojis.ToArray()) {
					await WelcomeMessage.CreateReactionAsync(optionEmoji);
				}
			}
		}
		/// <summary>
		/// Checks lobbies, cleans them, refreshes users, checks secret rooms updates timeouts, cleans them
		/// TODO change secret room behaviour
		/// </summary>
		/// <returns></returns>
		async Task VoiceChannelsInitAsync() {
			//Clean lobbies
			await CombLobbies().ConfigureAwait(false);
			//check users in lobbies
			RefreshUsersInLobbies();
			//check secret rooms
			PairsInSecretRooms.Clear();
			foreach (var r in SecretRooms.ToArray()) {
				if (r.Users.Count() > 1) {
					AllUserStates.TryGetValue(r.Users.First().Id, out var uState);
					var pair = new PairInSecretRoom() { SecretRoom = r, Users = r.Users.ToList() };
					//Try get timeout
					if (uState != null && uState.EnteredPrivateRoomTime.HasValue) pair.Timeout = uState.EnteredPrivateRoomTime.Value.AddMilliseconds(SecretRoomTime);
					else pair.Timeout = DateTime.Now;
					PairsInSecretRooms.Add(pair);
					_ = TimeoutDisband(pair)
						.ConfigureAwait(false);
				} else {
					SecretRooms.Remove(r);
					try {
						await r.DeleteAsync().ConfigureAwait(false);
					} catch (Exception) { }
				}
			}
		}

		private void RefreshUsersInLobbies() {
			UsersInLobbies.Clear();
			DateVoiceLobbies.ForEach(l => UsersInLobbies.AddRange(l.Users));
		}

		private IEnumerable<DiscordChannel> GetVoiceLobbies(bool refreshRoot = false) {
			if (refreshRoot) DateRootCategory = Guild.GetChannel(DateRootCategory.Id);
			return DateRootCategory.Children.Where(c => c.Type == ChannelType.Voice && c.Name.ToLower().Contains("lobby"));
		}

		private IEnumerable<DiscordChannel> GetSecretRooms(bool refreshRoot = false) {
			if (refreshRoot) DateSecretCategory = Guild.GetChannel(DateSecretCategory.Id);
			return DateSecretCategory.Children.Where(c => c.Type == ChannelType.Voice && c.Name.ToLower().Contains("secret"));
		}

		private async Task AddLastEmptyVoiceLobby() {
			var c = await Guild.CreateChannelAsync($"Date Voice Lobby {DateVoiceLobbies.Count}", ChannelType.Voice, DateRootCategory,
				overwrites: new List<DiscordOverwriteBuilder>() {
					new DiscordOverwriteBuilder() {  Allowed = Permissions.AccessChannels }.For(Guild.EveryoneRole)
				});
			//await c.AddOverwriteAsync(Guild.EveryoneRole, Permissions.AccessChannels);
			DateVoiceLobbies.Add(c);
		}

		public void ApplyGenderAndOptionReactions(DiscordUser user, DiscordEmoji emoji) {
			AllUserStates.TryGetValue(user.Id, out var uState);
			if (uState == null) {
				uState = new UserState() { UserId = user.Id };
				AllUserStates.Add(user.Id, uState);
			}
			if (emoji == MaleEmoji) {
				uState.Gender = GenderEnum.Male;
				WelcomeMessage.DeleteReactionAsync(MaleEmoji, user);
				uState.AgeOptions = 0;
			} else if (emoji == FemaleEmoji) {
				uState.Gender = GenderEnum.Female;
				WelcomeMessage.DeleteReactionAsync(FemaleEmoji, user);
				uState.AgeOptions = 0;
			} else {
				var option = OptionEmojis.IndexOf(emoji);
				uState.AgeOptions ^= option; //toggle age group
			}
		}

		/// <summary>
		/// User changed state in voice lobbies (joined/left)
		/// </summary>
		/// <param name="e"></param>
		/// <returns></returns>
		public async Task VoiceStateUpdated(VoiceStateUpdateEventArgs e) {
			InitTask?.Wait();
			//DebugLogWriteLine("VoiceStateUpdated: ");

			if (e.After?.Channel != e.Before?.Channel) { //Channel changed
				var beforeInLobbies = DateVoiceLobbies.Contains(e.Before?.Channel);
				var afterInLobbies = DateVoiceLobbies.Contains(e.After?.Channel);
				var beforeInSecretRooms = SecretRooms.Contains(e.Before?.Channel);
				var afterInSecretRooms = SecretRooms.Contains(e.After?.Channel);

				if (!beforeInLobbies && !beforeInSecretRooms && afterInLobbies) {
					//User connected to lobbies
					DebugLogWriteLine($"User {e.User} connected to {e.Channel} ");
					if (!UsersInLobbies.Contains(e.User)) UsersInLobbies.Add(e.User);
					if (!AllUserStates.ContainsKey(e.User.Id))
						AllUserStates.Add(e.User.Id, new UserState() { UserId = e.User.Id, LastEnteredLobbyTime = DateTime.Now });
					//Start matching session
					TryStartMatchingTask();

				} else if ((beforeInLobbies || beforeInSecretRooms) && !afterInLobbies && !afterInSecretRooms) {
					//User left activity
					DebugLogWriteLine($"User {e.User} left activity ");
					if (beforeInLobbies) {
						UsersInLobbies.Remove(e.User);
						RemoveStateFor(e.User);
						TryCombLobbies();
					}
					//remove disband secret room if one left TODO check logic
					if (beforeInSecretRooms) {
						RemoveStateFor(e.User);
						//DebugLogWrite($"trying to disbnad {e.Before.Channel}... ");
						await disbandRemoveSecretRoom(e.Before.Channel).ConfigureAwait(false);
						//DebugLogWrite($"{e.Before.Channel} removed/disbanded");
					}

				} else if (beforeInLobbies && afterInLobbies) {
					DebugLogWriteLine($"User {e.User} switched lobbies ");
					//User switched Lobbies
					TryCombLobbies();

				} else if (beforeInLobbies && afterInSecretRooms) {
					DebugLogWriteLine($"User {e.User} moved to {e.After.Channel} ");
					//Moved to secret room
					UsersInLobbies.Remove(e.User);
					TryCombLobbies();

				} else if (beforeInSecretRooms && afterInLobbies) {
					//Returned from secret room
					RemoveStateFor(e.User);
					DebugLogWriteLine($"User {e.User} returned to lobby, trying to disbnad {e.Before.Channel}... ");
					if (!UsersInLobbies.Contains(e.User)) UsersInLobbies.Add(e.User);
					await disbandRemoveSecretRoom(e.Before.Channel).ConfigureAwait(false);
					//DebugLogWrite($"{e.Before.Channel} removed/disbanded. Starting MatchingTask");
					//Start matching session
					TryStartMatchingTask();
				}
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
		}

		private void RemoveStateFor(DiscordUser User) {
			AllUserStates.TryGetValue(User.Id, out var uState);
			uState.EnteredPrivateRoomTime = null;
			PairsInSecretRooms.RemoveAll(p => p.Users.Contains(User));

		}

		private void TryCombLobbies() {
			if (CurrentCombLobbiesTask == null)
				//await (CurrentMatchingTask = MatchingTask()).ConfigureAwait(false);
				{
				CurrentCombLobbiesTask = CombLobbies();
				CurrentCombLobbiesTask.ConfigureAwait(false);
			}
		}

		private void TryStartMatchingTask() {
			if (CurrentMatchingTask == null)
				//await (CurrentMatchingTask = MatchingTask()).ConfigureAwait(false);
				{
				CurrentMatchingTask = MatchingTask();
				CurrentMatchingTask.ConfigureAwait(false);
			}
		}

		private async Task MatchingTask() {
			//Wait some time
			DebugLogWriteLine("MatchingTask started, waiting");
			await Task.Delay(30000);
			//Get all users
			DebugLogWriteLine("MatchingTask Matching");
			DateVoiceLobbies.Clear();
			DateVoiceLobbies.AddRange(GetVoiceLobbies(true));
			RefreshUsersInLobbies();
			//Match users
			//Move pairs
			TryMatchUsers();

			//Removeitself or restart
			DateVoiceLobbies.Clear();
			DateVoiceLobbies.AddRange(GetVoiceLobbies(true));
			RefreshUsersInLobbies();
			if (UsersInLobbies.Count > 4) {
				//Restart? if not same gender
				CurrentMatchingTask = MatchingTask();
				_ = CurrentMatchingTask.ConfigureAwait(false);
			} else
				CurrentMatchingTask = null;
		}

		private void TryMatchUsers() {
			if (UsersInLobbies.Count > 1) {
				var userStatePairsInLobbies = UsersInLobbies.Join(AllUserStates,
					u => u.Id, s => s.Key,
					(u, s) => new UserStateDiscordUserPair {
						User = Guild.GetMemberAsync(s.Key).Result,
						State = s.Value
					}).ToArray();
				var r = new Random(DateTime.Now.Millisecond);
				var matches = (from a in userStatePairsInLobbies.OrderBy(u=>r.Next())
							   from b in userStatePairsInLobbies
							   select Match(a.State, b.State))
								.Where(m => m.A.State != m.B.State && m.A.State.Gender != m.B.State.Gender && m.A.State.Gender != GenderEnum.None && m.B.State.Gender != GenderEnum.None)
					.OrderByDescending(m => m.Match)
					.ToList();

				DebugLogWriteLine($"Match {string.Join(", ",userStatePairsInLobbies.Select(p=>p.User.Username))} users,\n {string.Join(", ", matches.Select(m=>m.A.User.DisplayName + ":" + m.B.User.DisplayName + " " + m.Match))} matches");
				//Move to private rooms
				bool exit = exitConditions(matches);
				while (matches.Count() > 0 && !exit) {
					var pair = matches.First();
					matches.RemoveAll(m => m.A.State == pair.A.State
					|| m.A.State == pair.B.State
					|| m.B.State == pair.A.State
					|| m.B.State == pair.B.State
					);
					MoveToPrivateLobbyAsync(pair)
						.ContinueWith(t=>TimeoutDisband(t.Result))
						.ConfigureAwait(false);

					exit = exitConditions(matches);
				}
			}

			bool exitConditions(List<UsersPairMatch> matches) => (matches.All(m => m.Match == 0 || m.A.State.Gender == m.B.State.Gender));
		}

		private UsersPairMatch Match(UserState A, UserState B) {
			float match = A.Gender != B.Gender ? 1f : 0f;
			var girl = (A.Gender == GenderEnum.Female) ? A : (B.Gender == GenderEnum.Female) ? B : null;
			var boy = (A.Gender == GenderEnum.Male) ? A : (B.Gender == GenderEnum.Male) ? B : null;
			if (girl != null && boy != null) {
				if (girl.AgeOptions != 0 && boy.AgeOptions != 0 && (girl.AgeOptions & boy.AgeOptions) == boy.AgeOptions)
					match += 1f;
				if (girl.LikedUserIds.Contains(boy.UserId) && boy.LikedUserIds.Contains(girl.UserId)) match += .5f;
				if (girl.DislikedUserIds.Contains(boy.UserId) || boy.DislikedUserIds.Contains(girl.UserId)) match -= .5f;
				if (girl.LastMatches.Contains(boy.UserId) || boy.LastMatches.Contains(girl.UserId)) match -= .5f * (girl.LastMatches.IndexOf(boy.UserId));
			}
			return new UsersPairMatch() {
				A = new UserStateDiscordUserPair() { State = A, User = Guild.GetMemberAsync(A.UserId).Result },
				B = new UserStateDiscordUserPair() { State = B, User = Guild.GetMemberAsync(B.UserId).Result },
				Match = match
			};
		}

		private async Task<PairInSecretRoom> MoveToPrivateLobbyAsync(UsersPairMatch pair) {
			//DebugLogWriteLine($"Moving {pair.A} and {pair.B} to room... ");
			UsersInLobbies.Remove(pair.A.User);
			UsersInLobbies.Remove(pair.B.User);

			var privateRoom = await Guild.CreateChannelAsync($"Secret Room {Guid.NewGuid().ToString()}", ChannelType.Voice, DateSecretCategory
				, overwrites: new DiscordOverwriteBuilder[] { SecretRoomOverwriteBuilder });
			SecretRooms.Add(privateRoom);
			DebugLogWrite("room created... ");


			var timeout = DateTime.Now.AddMilliseconds(SecretRoomTime);
			pair.A.State.EnteredPrivateRoomTime = pair.B.State.EnteredPrivateRoomTime = timeout;

			var p = new PairInSecretRoom() {
				Users = new List<DiscordMember>() { pair.A.User, pair.B.User },
				SecretRoom = privateRoom,
				Timeout = timeout
			};
			PairsInSecretRooms.Add(p);

			DebugLogWrite("moving... ");
			_ = privateRoom.PlaceMemberAsync(pair.A.User).ConfigureAwait(false);
			_ = privateRoom.PlaceMemberAsync(pair.B.User).ConfigureAwait(false);


			pair.A.State.AddMatch(pair.B.User.Id);
			pair.B.State.AddMatch(pair.A.User.Id);

			DebugLogWrite("finished");
			return p;
		}

		[System.Diagnostics.DebuggerNonUserCode]
		private DateTime UpdateTimeout(List<DiscordMember> Users) {
			try {
				AllUserStates.TryGetValue(Users[0].Id, out var uStateA);
				AllUserStates.TryGetValue(Users[1].Id, out var uStateB);

				return (uStateA.EnteredPrivateRoomTime < uStateB.EnteredPrivateRoomTime ? uStateA.EnteredPrivateRoomTime : uStateB.EnteredPrivateRoomTime).Value;
			} catch (Exception) { }
			return DateTime.Now;
		}

		private async Task TimeoutDisband(PairInSecretRoom pair) {
			DateTime? timeout = UpdateTimeout(pair.Users);
			if (pair.Users.Count == 2) {
				do {
					DebugLogWrite($" Now: {DateTime.Now} Timeout: {timeout} secrettime: {TimeSpan.FromMilliseconds(SecretRoomTime)}");

					await Task.Delay(Math.Max((int)(timeout.Value - DateTime.Now).TotalMilliseconds - 61000, 0));
					if (!PairsInSecretRooms.Contains(pair)) return; //quit if disbanded outside

					timeout = UpdateTimeout(pair.Users);
					foreach (var p in pair.Users) {
						await p.SendMessageAsync($"{(timeout.Value - DateTime.Now).TotalMinutes.ToString("G2")} min left for {string.Join(", ", pair.Users.Select(p => p.DisplayName))}").ConfigureAwait(false);
					}

					await Task.Delay(Math.Max((int)(timeout.Value - DateTime.Now).TotalMilliseconds + 100, 0));

					if (!PairsInSecretRooms.Contains(pair)) return; //quit if disbanded outside
					timeout = UpdateTimeout(pair.Users);
					if (DateTime.Now < timeout.Value)
						foreach (var p in pair.Users) {
							_ = p.SendMessageAsync($"{(timeout.Value - DateTime.Now).TotalMinutes.ToString("G2")} min left for {string.Join(", ", pair.Users.Select(p => p.DisplayName))}").ConfigureAwait(false);
						}
				} while (DateTime.Now < timeout.Value);
			}
			//return if timer off
			//else
			DebugLogWriteLine($"Timeout Disbanding {pair.Users[0]} and {pair.Users[1]}");
			foreach (var p in pair.Users.ToArray()) {
				//Return participants to lobby 0
				_ = DateVoiceLobbies[0].PlaceMemberAsync(p).ConfigureAwait(false);
			}
		}


		private async Task CombLobbies() {
			await Task.Delay(1000);
			//Comb lobbies
			DateVoiceLobbies.Clear();
			DateVoiceLobbies.AddRange(GetVoiceLobbies(true));
			DateVoiceLobbies.Where(l => l.Id != DateVoiceLobbies.Last().Id && l.Users.Count() == 0)
				.ToList()
				.ForEach(async l => {
					DateVoiceLobbies.Remove(l);
					await l.DeleteAsync();
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

		internal void MessageReactionAdded(MessageReactionAddEventArgs e) {
			if (e.User.Id == DateBot.Instance.BotId) return;
			try {
				if (e.Message.Id == WelcomeMessage.Id) {
					if (e.Emoji.Id == MaleEmoji.Id || e.Emoji.Id == FemaleEmoji.Id || OptionEmojis.Contains(e.Emoji)) {
						ApplyGenderAndOptionReactions(e.User, e.Emoji);
					}
					_ = e.Message.DeleteReactionAsync(e.Emoji, e.User).ConfigureAwait(false);
				} else if (e.Message.Id == PrivateControlsMessageId) {
					if (e.Emoji.Id == LikeEmoji.Id || e.Emoji.Id == DisLikeEmoji.Id || e.Emoji.Id == TimeEmoji.Id) {
						_ = ApplyPrivateReactionsAsync(e.User, e.Emoji).ConfigureAwait(false);
					}
					_ = e.Message.DeleteReactionAsync(e.Emoji, e.User).ConfigureAwait(false);
				}
			} catch (Exception) { }
		}

		//internal void MessageReactionRemoved(MessageReactionRemoveEventArgs e) {
		//	if (e.User.Id == DateBot.Instance.BotId) return;
		//	if (e.Message.ChannelId == DateSecretCategoryId) {
		//		if (e.Emoji.Id == LikeEmoji.Id || e.Emoji.Id == DisLikeEmoji.Id || e.Emoji.Id == TimeEmoji.Id) {
		//			SecretChannelReaction(e.Emoji, e.User, e.Message, false);
		//		}
		//	}
		//}
	}
}