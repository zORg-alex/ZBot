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
				
				//TODO decide what level of initializatino is necessary
				if(config_.DateLobbyId != default) config.DateLobbyId = config_.DateLobbyId;
				if (config_.DateCategoryId != default) config.DateCategoryId = config_.DateCategoryId;
				if (config_.DateSecretCategoryId != default) config.DateSecretCategoryId = config_.DateSecretCategoryId;
				if (config_.MaleEmojiId != default) config.MaleEmojiId = config_.MaleEmojiId;
				if (config_.FemaleEmojiId != default) config.FemaleEmojiId = config_.FemaleEmojiId;
				if (config_.OptionEmojiIds.Count != default) config.OptionEmojiIds = config_.OptionEmojiIds;
				if (config_.LikeEmojiId != default) config.LikeEmojiId = config_.LikeEmojiId;
				if (config_.DisLikeEmojiId != default) config.DisLikeEmojiId = config_.DisLikeEmojiId;
				if (config_.TimeEmojiId != default) config.TimeEmojiId = config_.TimeEmojiId;
				if (config_.SecretRoomTime != default) config.SecretRoomTime = config_.SecretRoomTime;
				if (config_.WelcomeMessageBody != default) config.WelcomeMessageBody = config_.WelcomeMessageBody;
				if (config_.WelcomeMessageId != default) config.WelcomeMessageId = config_.WelcomeMessageId;
				if (config_.PrivateMessageBody != default) config.PrivateMessageBody = config_.PrivateMessageBody;
				if (config_.LogChannelId != default) config.LogChannelId = config_.LogChannelId;

				await ((GuildTask)config).Initialize(ctx.Guild).ConfigureAwait(false);
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
