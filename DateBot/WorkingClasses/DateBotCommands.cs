using System;
using System.Linq;
using System.Security.Policy;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using Newtonsoft.Json;
using ZBot.DialogFramework;

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

			if (config_.LogChannelId == 0)
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
				if (config_.DateTextChannelId != default) {
					reinit = config.DateTextChannelId != config_.DateTextChannelId;
					config.DateTextChannelId = config_.DateTextChannelId;
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

		[Command("start")]
		public async Task Start(CommandContext ctx) {
			var config = DateBot.Instance.GetGuild(ctx.Guild.Id);
			_ = DialogFramework.QuickVolatileMessage(ctx.Channel, "Started activity");
			config.StartActivity();
		}
		[Command("stop")]
		public async Task Stop(CommandContext ctx) {
			var config = DateBot.Instance.GetGuild(ctx.Guild.Id);
			_ = DialogFramework.QuickVolatileMessage(ctx.Channel, "Stopped activity");
			config.StopActivity();
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
		[Command("config")]
		public async Task DialogConfig(CommandContext ctx) {
			//We need to set quite a few things in this menu, so it is broken down to smaller bits.
			//Many menus are the same, except few words and where values go

			var config = DateBot.Instance.GetGuild(ctx.Guild.Id) as GuildConfig;
			if (config == null) {
				//Add and set default stuff
				var gt = new GuildTask() { GuildId = ctx.Guild.Id,
					WelcomeMessageBody = $"Change this message to fit your audience",
					MaleEmojiId = EmojiProvider.MaleSign,
					FemaleEmojiId = EmojiProvider.FemaleSign,
					OptionEmojiIds = { EmojiProvider.One, EmojiProvider.Two, EmojiProvider.Three },
					PrivateMessageBody = $"Change this message to fit your audience",
					LikeEmojiId = EmojiProvider.Heart,
					CancelLikeEmojiId = EmojiProvider.HeartBlack,
					DisLikeEmojiId = EmojiProvider.HeartBroken,
					TimeEmojiId = EmojiProvider.Timer
				};

				DateBot.Instance.AddGuild(gt);
			}

			await ctx.Message.DeleteAsync().ConfigureAwait(false);

			await MainMenu(ctx).ConfigureAwait(false);
		}

		private async Task MainMenu(CommandContext ctx) {
			await DialogFramework.CreateQuestion(ctx.Channel,
				$"Choose what you want to set:\n" +
				$"{EmojiProvider.Rose} to set Date Category\n" +
				$"{EmojiProvider.Detective} to set Secret Category\n" +
				$"{EmojiProvider.Handsahke} to set Welcome message\n" +
				$"{EmojiProvider.ControlKnobs} to set Controls message\n" +
				$"{EmojiProvider.WhiteHeartInRed} to set Emojis\n" +
				$"{EmojiProvider.Timer} to set timeout length\n" +
				$"{EmojiProvider.ArrowsClockwise} to apply changes (reinit bot on this guild)\n" +
				$"{EmojiProvider.CrossOnGreen} to quit this dialog",
				new Answer[] {
				new Answer(EmojiProvider.Rose,e=>{
					_ = SetCategory(ctx ,"Date Category");
				}),
				new Answer(EmojiProvider.Detective,e=>{
					_ = SetCategory(ctx ,"Secret Category");
				}),
				new Answer(EmojiProvider.Handsahke,e=>{
					_ = SetMessage(ctx ,"Welcome");
				}),
				new Answer(EmojiProvider.ControlKnobs,e=>{
					_ = SetMessage(ctx ,"Controls");
				}),
				new Answer(EmojiProvider.WhiteHeartInRed,e=>{
					_ = SetEmoji(ctx);
				}),
				new Answer(EmojiProvider.Timer,e=>{
					_ = SetTimeout(ctx);
				}),
				new Answer(EmojiProvider.ArrowsClockwise,e=>{
					var gt = DateBot.Instance.GetGuild(ctx.Guild.Id);
					_ = gt.Initialize(ctx.Guild);
				}),
				new Answer(EmojiProvider.CrossOnGreen,e=>{
					DialogFramework.QuickVolatileMessage(ctx.Channel, "Thank you for interaction, bye.");
				})
				}, ctx.User.Id, deleteAnswer: true, deleteAnswerTimeout: TimeSpan.Zero,
				timeoutMessage:"Thank you for interaction, bye.").ConfigureAwait(false);
		}
		private async Task SetCategory(CommandContext ctx, string categoryType) {
			string info = "";
			if (categoryType == "Date Category")
				info = "creating new voice lobbies and returning activity members back from secret rooms";
			else if (categoryType == "Secret Category")
				info = "creating new secret rooms for dates";
			else return;
			var config = DateBot.Instance.GetGuild(ctx.Guild.Id) as GuildConfig;

			DiscordChannel cat = null;

			await DialogFramework.CreateQuestion(ctx.Channel,
				$"Paste {categoryType} Id, it will be used for {info}.", message => {
					ulong.TryParse(message, out var id);
					ctx.Guild.Channels.TryGetValue(id, out cat);
					return (cat != null && cat.IsCategory);
				}, e => {
					if (categoryType == "Date Category") {
						config.DateCategoryId = cat.Id;
						_ = SetMainTextChannelAsync(ctx, config);
					} else if (categoryType == "Secret Category") {
						config.DateSecretCategoryId = cat.Id;
						_ = MainMenu(ctx);
					}

					return Task.CompletedTask;
				}, ctx.User.Id, deleteAnswer:true, deleteAnswerTimeout: TimeSpan.Zero, wrongAnswer:"I'm looking for a ulong Id of a category");


			async Task SetMainTextChannelAsync(CommandContext ctx, GuildConfig config) {
				DiscordChannel channel = null;
				await DialogFramework.CreateQuestion(ctx.Channel,
					"Paste main text channel Id, where will happen all interaction with date activity.", 
					message => {
						ulong.TryParse(message, out var id);
						return ctx.Guild.Channels.TryGetValue(id, out channel);
					},
					e => {
						config.DateTextChannelId = channel.Id;
						return Task.CompletedTask;
					}, UserId: ctx.User.Id, deleteAnswer: true, deleteAnswerTimeout: TimeSpan.Zero,
					wrongAnswer: "Doesn't look like it's a channel Id").ConfigureAwait(false);

				_ = MainMenu(ctx);
			}
		}
		private async Task SetMessage(CommandContext ctx, string messageType) {
			string info = "";
			if (messageType == "Welcome")
				info = "setting gender, and age preferences";
			else if (messageType == "Controls")
				info = "conrolls during a date, setting likes or adding time";
			else return;
			var config = DateBot.Instance.GetGuild(ctx.Guild.Id) as GuildConfig;
			ctx.Guild.Channels.TryGetValue(config.DateTextChannelId, out var channel);

			await DialogFramework.CreateQuestion(ctx.Channel,
				$"Set {messageType} message content, it will be used for {info}", s => true, async e => {
					//TODO Check regex to have only numbers?
					if (ulong.TryParse(e.Message.Content, out var id)) {
						var message = await channel.GetMessageAsync(id);
						if (message == null) {
							//Error message not found
							_ = DialogFramework.QuickVolatileMessage(ctx.Channel, "This Seemed to be a message Id, but it wasn't found");
						} else {
							if (messageType == "Welcome")
								config.WelcomeMessageId = id;
							else if (messageType == "Controls")
								config.PrivateControlsMessageId = id;
						}
					} else {
						if (messageType == "Welcome")
							config.WelcomeMessageBody = e.Message.Content;
						else if (messageType == "Controls")
							config.PrivateMessageBody = e.Message.Content;
					}
				}, ctx.User.Id, deleteAnswer: true, deleteAnswerTimeout: TimeSpan.Zero, wrongAnswer: "Enter a message body, or paste existing messages Id");

			_ = MainMenu(ctx);
		}
		private async Task SetEmoji(CommandContext ctx) {
			var gt = DateBot.Instance.GetGuild(ctx.Guild.Id);

			await DialogFramework.CreateQuestion(ctx.Channel,
				$"Choose what Emoji to set\n" +
				$"{EmojiProvider.MaleSign} to set emoji for selecting male gender. Currently it is set to {gt.MaleEmojiId}\n" +
				$"{EmojiProvider.FemaleSign} to set emoji for selecting male gender. Currently it is set to {gt.FemaleEmojiId}\n" +
				$"{EmojiProvider.QuestionMark} to set emojs for age options. Keep spaces inbetween. Currently it is set to {string.Join(" ", gt.OptionEmojiIds)}\n" +
				$"{EmojiProvider.Heart} to set emojs for Like. Currently it is set to {gt.LikeEmojiId}\n" +
				$"{EmojiProvider.HeartBlack} to set emojs for canceling Like. Currently it is set to {gt.CancelLikeEmojiId}\n" +
				$"{EmojiProvider.HeartBroken} to set emojs for canceling Like. Currently it is set to {gt.DisLikeEmojiId}\n" +
				$"{EmojiProvider.Timer} to set emojs for canceling Like. Currently it is set to {gt.TimeEmojiId}\n" +
				$"{EmojiProvider.CrossOnGreen} to go back.",
				new Answer[] {
					new Answer(EmojiProvider.MaleSign, async e=> await Set("Male").ConfigureAwait(false)),
					new Answer(EmojiProvider.FemaleSign, async e=> await Set("Female").ConfigureAwait(false)),
					new Answer(EmojiProvider.QuestionMark, async e=> await Set("Options").ConfigureAwait(false)),
					new Answer(EmojiProvider.Heart, async e=> await Set("Like").ConfigureAwait(false)),
					new Answer(EmojiProvider.HeartBlack, async e=> await Set("Cancel Like").ConfigureAwait(false)),
					new Answer(EmojiProvider.HeartBroken, async e=> await Set("Dislike").ConfigureAwait(false)),
					new Answer(EmojiProvider.Timer, async e=> await Set("Time").ConfigureAwait(false)),
					new Answer(EmojiProvider.CrossOnGreen, async e=> await MainMenu(ctx).ConfigureAwait(false))
				}, ctx.User.Id);

			async Task Set(string emojiType) {
				var message = $"Enter an emoji for {emojiType}";
				if (emojiType == "Options") {
					message = $"Enter an emoji sequence for age options. Keep spaces inbetween.";
				}
				await DialogFramework.CreateQuestion(ctx.Channel, message, message => {
					//Seems like we get unicode here from message.Content, not name. Had some problems with it
					if (emojiType == "Options") {
						var split = message.Split(' ', System.StringSplitOptions.RemoveEmptyEntries);
						return split.All(m => DiscordEmoji.FromUnicode(DateBot.Instance.Client, m) != null);
					} else 
						return DiscordEmoji.FromUnicode(DateBot.Instance.Client, message) is DiscordEmoji;
					

				}, e => {
					switch (emojiType) {
						case "Male":
							gt.MaleEmojiId = DiscordEmoji.FromUnicode(DateBot.Instance.Client, e.Message.Content).GetDiscordName();
							break;
						case "Female":
							gt.FemaleEmojiId = DiscordEmoji.FromUnicode(DateBot.Instance.Client, e.Message.Content).GetDiscordName();
							break;
						case "Like":
							gt.LikeEmojiId = DiscordEmoji.FromUnicode(DateBot.Instance.Client, e.Message.Content).GetDiscordName();
							break;
						case "Cancel Like":
							gt.CancelLikeEmojiId = DiscordEmoji.FromUnicode(DateBot.Instance.Client, e.Message.Content).GetDiscordName();
							break;
						case "Dislike":
							gt.DisLikeEmojiId = DiscordEmoji.FromUnicode(DateBot.Instance.Client, e.Message.Content).GetDiscordName();
							break;
						case "Time":
							gt.TimeEmojiId = DiscordEmoji.FromUnicode(DateBot.Instance.Client, e.Message.Content).GetDiscordName();
							break;
						case "Options":
							gt.OptionEmojiIds = e.Message.Content.Split(' ', System.StringSplitOptions.RemoveEmptyEntries).
							Select(u=> DiscordEmoji.FromUnicode(DateBot.Instance.Client,u).GetDiscordName()).ToList();
							break;
						default:
							break;
					}
					//Return back to the Emoji menu
					_ = SetEmoji(ctx);
					return Task.CompletedTask;
				}, ctx.User.Id, deleteAnswer: true, deleteAnswerTimeout: TimeSpan.Zero, wrongAnswer: "This isn't looking right.");

			}
		}
		private async Task SetTimeout(CommandContext ctx) {
			var config = DateBot.Instance.GetGuild(ctx.Guild.Id) as GuildConfig;

			float time = default;

			await DialogFramework.CreateQuestion(ctx.Channel,
				$"Set timeout in minutes in form of `2,5` for 2 and a half minutes", message => {
					return float.TryParse(message, out time);
				}, e => {
					config.SecretRoomTime = (int)(time * 60000f);
					return Task.CompletedTask;
				}, ctx.User.Id, deleteAnswer: true, deleteAnswerTimeout: TimeSpan.Zero, wrongAnswer: "This isn't a float value");

			_ = MainMenu(ctx);
		}
	}
}
