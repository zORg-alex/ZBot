using System.Linq;
using System.Security.Policy;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using Newtonsoft.Json;

namespace DateBot.Base {
	/// <summary>
	/// DateBot commands
	/// </summary>
	public class DateBotCommands : BaseCommandModule {
		[Command("date-bot-config")]
		[Aliases("config")]
		public async Task ReadConfig(CommandContext ctx, string json) {
			json = json.Replace("```", string.Empty);
			var isNew = !DateBot.Instance.GuildRegistered(ctx.Guild.Id);

			GuildTask config_ =
				JsonConvert.DeserializeObject<GuildTask>(json);

			if(config_.LogChannelId == 0)
				config_.LogChannelId = ctx.Channel.Id;

			if (isNew) {
				DateBot.Instance.AddGuild(config_);

				config_.Initialize(ctx.Guild).Wait();
				await DateBot.Instance.SaveStates().ConfigureAwait(false);
			} else {
				var config = DateBot.Instance.GetGuild(ctx.Guild.Id) as GuildConfig;

				bool regenerateWelcomeMessage = false;
				bool reinit = false;
				var gt = ((GuildTask)config);
				//TODO decide what level of initialization is necessary
				if (config_.DateLobbyId != default) {
					reinit = config.DateLobbyId != config_.DateLobbyId;
					config.DateLobbyId = config_.DateLobbyId;
				}
				if (config_.DateCategoryId != default) {
					reinit = config.DateCategoryId != config_.DateCategoryId;
					config.DateCategoryId = config_.DateCategoryId;
				}
				if (config_.DateSecretCategoryId != default) {
					reinit = config.DateSecretCategoryId != config_.DateSecretCategoryId;
					config.DateSecretCategoryId = config_.DateSecretCategoryId;
				}
				if (config_.MaleEmojiId != default) {
					regenerateWelcomeMessage = config.MaleEmojiId != config_.MaleEmojiId;
					config.MaleEmojiId = config_.MaleEmojiId;
				}
				if (config_.FemaleEmojiId != default) {
					regenerateWelcomeMessage = config.FemaleEmojiId != config_.FemaleEmojiId;
					config.FemaleEmojiId = config_.FemaleEmojiId;
				}
				if (config_.OptionEmojiIds.Count != default) {
					regenerateWelcomeMessage = config.OptionEmojiIds != config_.OptionEmojiIds;
					config.OptionEmojiIds = config_.OptionEmojiIds;
				}
				if (config_.LikeEmojiId != default) {
					regenerateWelcomeMessage = config.LikeEmojiId != config_.LikeEmojiId;
					config.LikeEmojiId = config_.LikeEmojiId;
				}
				if (config_.DisLikeEmojiId != default) {
					regenerateWelcomeMessage = config.DisLikeEmojiId != config_.DisLikeEmojiId;
					config.DisLikeEmojiId = config_.DisLikeEmojiId;
				}
				if (config_.TimeEmojiId != default) {
					regenerateWelcomeMessage = config.TimeEmojiId != config_.TimeEmojiId;
					config.TimeEmojiId = config_.TimeEmojiId;
				}
				if (config_.SecretRoomTime != default) {
					gt.ChangeTimeout(config_.SecretRoomTime);
				}
				if (config_.WelcomeMessageBody != default && config.WelcomeMessageBody != config_.WelcomeMessageBody) {
					config.WelcomeMessageBody = config_.WelcomeMessageBody;
					await gt.WelcomeMessage.ModifyAsync(config.WelcomeMessageBody);
				}
				if (config_.WelcomeMessageId != default) config.WelcomeMessageId = config_.WelcomeMessageId;
				if (config_.PrivateMessageBody != default && config.PrivateMessageBody != config_.PrivateMessageBody) {
					config.PrivateMessageBody = config_.PrivateMessageBody;
					await gt.PrivateControlsMessage.ModifyAsync(config.PrivateMessageBody);
				}
				if (config_.LogChannelId != default) config.LogChannelId = config_.LogChannelId;

				if (reinit) await gt.Initialize(ctx.Guild).ConfigureAwait(false);
				else if (regenerateWelcomeMessage) {
					await gt.WelcomeMessageInit();
					await gt.PrivateControlsMessageInit();
				}
			}
		}
		[Command("date-bot-adduser")]
		[Aliases("adduser")]
		public async Task AddUser(CommandContext ctx, ulong id, int gender, int age, params ulong[] likedIds) {
			var gt = DateBot.Instance.State.Guilds.FirstOrDefault(g => g.GuildId == ctx.Guild.Id);
			gt.AllUserStates.TryGetValue(id, out var uState);
			if (uState == null) {
				uState = new UserState() { UserId = id, Gender = (GenderEnum)gender, AgeOptions = age, LikedUserIds = likedIds.ToList() };
				gt.AllUserStates.Add(id, uState);
			} else {
				uState.Gender = (GenderEnum)gender;
				uState.AgeOptions = age;
				uState.AddLike(likedIds);
			}
		}
	}
}
