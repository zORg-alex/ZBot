using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using MongoDB.Driver;
using Newtonsoft.Json;

namespace DateBot.Base {
	/// <summary>
	/// DateBot commands
	/// </summary>
	public class DateBotCommands : BaseCommandModule {
		[Command("date-bot-config")]
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
				var config = DateBot.Instance.GetGuild(ctx.Guild.Id);

				config.DateLobbyId = config_.DateLobbyId;
				config.DateCategoryId = config_.DateCategoryId;
				config.MaleEmojiId = config_.MaleEmojiId;
				config.FemaleEmojiId = config_.FemaleEmojiId;
				config.WelcomeMessageBody = config_.WelcomeMessageBody;
				config.PrivateMessageBody = config_.PrivateMessageBody;
				config.LogChannelId = config_.LogChannelId;

				await config.Initialize(ctx.Guild).ConfigureAwait(false);
			}
		}
	}
}
