using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using DSharpPlus.Entities;
using zLib;
namespace ZBot {
	public class GuildTask {
		public GuildTask(CancellationTokenSource cts) {
			_cts = cts;
			LobbyMembers.CollectionChanged += LobbyMembers_CollectionChanged;
		}

		private CancellationTokenSource _cts { get; set; }

		public DiscordGuild Guild { get; set; }
		public DiscordChannel DateRootCategory { get; set; }
		public DiscordChannel DateLobby { get; set; }
		public int LobbyCounter { get; set; } = 0;
		public List<DiscordChannel> VoiceLobbies { get; } = new List<DiscordChannel>();
		public ObservableCollection<DiscordMember> LobbyMembers { get; } = new ObservableCollection<DiscordMember>();
		public Dictionary<ulong, DateMemberConfig> LobbyMemberDictionary = new Dictionary<ulong, DateMemberConfig>();
		private Dictionary<ulong, DateMemberConfig> allMembersConfigs = new Dictionary<ulong, DateMemberConfig>();
		private void LobbyMembers_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e) {
			if (e.NewItems != null) foreach (DiscordMember n in e.NewItems) {
					if (!allMembersConfigs.ContainsKey(n.Id)) {
						allMembersConfigs.Add(n.Id, new DateMemberConfig() { MemberId = n.Id, Member = n });
					}
					if (!LobbyMemberDictionary.ContainsKey(n.Id)) LobbyMemberDictionary.Add(n.Id, allMembersConfigs.GetValueOrDefault(n.Id));
				}
			if (e.OldItems != null) foreach (DiscordMember o in e.OldItems) {
					if (LobbyMemberDictionary.ContainsKey(o.Id)) LobbyMemberDictionary.Remove(o.Id);
				}
		}

		public static DiscordEmoji MaleEmoji { get; private set; }
		public static DiscordEmoji FemaleEmoji { get; private set; }


		public List<DateMemberConfig> AllMemberConfigs { get; } = new List<DateMemberConfig>();
		public DiscordMessage WelcomeMessage { get; private set; }

		public async Task RunTask() {

			MaleEmoji = Guild.Emojis.FirstOrDefault(e => e.Value.Name.ToLower().Contains("male")).Value;
			FemaleEmoji = Guild.Emojis.FirstOrDefault(e => e.Value.Name.ToLower().Contains("female")).Value;

			if (MaleEmoji == null) MaleEmoji = DiscordEmoji.FromName(Bot.Instance.Client, ":male_sign:");
			if (FemaleEmoji == null) FemaleEmoji = DiscordEmoji.FromName(Bot.Instance.Client, ":female_sign:");

			while (!_cts.IsCancellationRequested) {
				//remove deleted lobbies
				VoiceLobbies.Where(l => !Guild.Channels.ContainsKey(l.Id)).ToList().ForEach(l => VoiceLobbies.Remove(l));

				//Check if new Lobby is needed, clean unused
				var emptyLobbies = VoiceLobbies.Where(l => l.Users.Count() == 0);
				if (emptyLobbies.Count() > 1) {
					var i = 0;
					foreach (var l in emptyLobbies.OrderByDescending(z => z.Name)) {
						i++;
						if (i < emptyLobbies.Count()) {
							VoiceLobbies.Remove(l);
							await l.DeleteAsync().ConfigureAwait(false);
						}
					}
					UpdateLobbyCounter();
				}

				UpdateLobbyCounter();

				if (emptyLobbies.Count() < 1) {
					await Bot.Instance.AddVoiceLobbyAsync(this);
				}

				//Check Welcome pinned, record genders
				var pinned = await DateLobby.GetPinnedMessagesAsync();
				if (WelcomeMessage == null) {
					var _welcomeMessage = pinned.FirstOrDefault(m => m.Author.IsBot && m.Author.Id == Bot.Instance.Id);
					if (_welcomeMessage == null) {
						var m = await DateLobby.SendMessageAsync(MessageConfig.WelcomeMessage);
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
						m.PinAsync();
						m.CreateReactionAsync(MaleEmoji);
						m.CreateReactionAsync(FemaleEmoji);
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
						WelcomeMessage = m;
					} else WelcomeMessage = _welcomeMessage;
				}
				var mReacted = await WelcomeMessage.GetReactionsAsync(MaleEmoji);
				var fReacted = await WelcomeMessage.GetReactionsAsync(FemaleEmoji);
				foreach (var m in mReacted) {
					if (!m.IsBot) {
						var boy = allMembersConfigs.GetValueOrDefault(m.Id);
						boy.Gender = GenderEnum.Male;
					}
				}
				foreach (var f in fReacted) {
					if (!f.IsBot) {
						var girl = allMembersConfigs.GetValueOrDefault(f.Id);
						girl.Gender = GenderEnum.Female;
					}
				}
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
				if (mReacted.Count > 1 || fReacted.Count > 1) {
					WelcomeMessage.DeleteReactionsEmojiAsync(MaleEmoji);
					await WelcomeMessage.CreateReactionAsync(MaleEmoji);
					WelcomeMessage.DeleteReactionsEmojiAsync(FemaleEmoji);
					WelcomeMessage.CreateReactionAsync(FemaleEmoji);
				}
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed


				//Check matches in lobbies
				RefreshLobbyMembers();
				var boys = LobbyMemberDictionary.Where(m => m.Value.Gender == GenderEnum.Male);
				var girls = LobbyMemberDictionary.Where(m => m.Value.Gender == GenderEnum.Female);

				var pairs = boys.Select(b => girls.Select(g => new KeyValuePair<DateMemberConfig, DateMemberConfig>(b.Value, g.Value)));

				await Task.Delay(TimeSpan.FromSeconds(15));
			}
		}

		public void UpdateLobbyCounter() {
			LobbyCounter = VoiceLobbies.Max(l => {
				int i;
				if (int.TryParse(Regex.Match(l.Name, @"\d+$").Value, out i))
					return i;
				else return 0;
			});
		}

		public void RefreshLobbyMembers() {
			LobbyMembers.Clear();
			VoiceLobbies.ForEach(v => LobbyMembers.AddRange(v.Users));
		}
	}
}