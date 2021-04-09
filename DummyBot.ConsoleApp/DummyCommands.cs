using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using ZBot.DialogFramework;

namespace DummyBot.ConsoleApp {
	public class DummyCommands : BaseCommandModule {

		[Command("dummy-config")]
		[Aliases("config")]
		public async Task Config(CommandContext ctx) {

			await ctx.Message.DeleteAsync().ConfigureAwait(false);

			var isNew = !DummyBot.Instance.GuildRegistered(ctx.Guild.Id);
			if (isNew) {
				var newGuildTask = new GuildTask() { GuildId = ctx.Guild.Id };
				DummyBot.Instance.AddGuild(newGuildTask);

				await DummyBot.Instance.SaveStates().ConfigureAwait(false);
			}

			await MainMenu().ConfigureAwait(false);

			//Ask what to set up
			// Voice Channel to connect
			// Channel>Message id's to set random reaction
			async Task MainMenu() {
				//Menu message
				await DialogFramework.CreateQuestion(ctx.Channel,
					$"What we should set up?" + Environment.NewLine +
					$"{EmojiProvider.One} Voice channel to connect to " + Environment.NewLine +
					$"{EmojiProvider.Two} Message id to react to", new Answer[]
					{
					new Answer(EmojiProvider.One, new string[] { "one", "voice", "channel" }, async e => {
						await SetVoiceChannel().ConfigureAwait(false);
					}),
					new Answer(EmojiProvider.Two, new string[] { "two", "message", "react" }, async e => {
						await SetMessageChannel().ConfigureAwait(false);
					})},
					ctx.User.Id, timeoutBeforeDelete: TimeSpan.Zero, deleteAnswer: true).ConfigureAwait(false);
			}
			//Continue configuring or quit?
			async Task Continue() {
				//ask
				await DialogFramework.CreateQuestion(ctx.Channel,
					"Would you go over?",
					new Answer[] {
						new Answer(EmojiProvider.CheckMarkOnGreen, new string[] { "yes", "sure", "go" }, async e => {
							await MainMenu().ConfigureAwait(false);
					}),
						new Answer(EmojiProvider.CrossOnGreen, new string[] { "no", "stop", "done" }, async e=>{
							await DialogFramework.QuickVolatileMessage(ctx.Channel, "Thank you. We are done here. Have a nice day.").ConfigureAwait(false);
						})
					},
					ctx.User.Id, timeoutBeforeDelete: TimeSpan.Zero, deleteAnswer: true).ConfigureAwait(false);
			}
			//Setvoice channel
			async Task SetVoiceChannel() {
				//Message
				DiscordChannel channel = null;
				await DialogFramework.CreateQuestion(ctx.Channel,
					$"Enter channel name, to connect to.",
					message => {
						channel = ctx.Guild.Channels.FirstOrDefault(c => c.Value.Name.Contains(message)).Value;
						return channel != null;
					},
					async e => {
						var gt = DummyBot.Instance.GetGuild(ctx.Guild.Id);
						gt.ConnectToVoiceName = channel.Name;
						gt.ConnectToVoice = channel;

						//Apply changes in task
						await DummyBot.Instance.GetGuild(ctx.Guild.Id).Initialize().ConfigureAwait(false);


						//Continue to next step
						await Continue().ConfigureAwait(false);
					}, ctx.User.Id, timeoutBeforeDelete: TimeSpan.Zero, deleteAnswer: true,
					wrongAnswer: "Couldn't find that channel. Try again.").ConfigureAwait(false);
			}
			//Set channel id to react on a message
			async Task SetMessageChannel() {
				DiscordChannel channel = null;
				await DialogFramework.CreateQuestion(ctx.Channel,
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
					$"Paste Channel Id, where to set random reaction.", message => {
						if (ulong.TryParse(message, out var id))
							channel = ctx.Guild.GetChannel(id);
						return channel != null;
					},
					async e => {
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
						var gt = DummyBot.Instance.GetGuild(ctx.Guild.Id);
						gt.SetRandomReactionOnChannelId = channel.Id;
						gt.SetRandomReactionOnChannel = channel;


						//Continue to next step
						await SetMessage().ConfigureAwait(false);
					}, ctx.User.Id, timeoutBeforeDelete: TimeSpan.Zero, deleteAnswer: true,
					wrongAnswer: "Couldn't find that channel. Try again.").ConfigureAwait(false);
			}
			//Set message id to react to
			async Task SetMessage() {
				GuildTask gt = null;
				DiscordMessage msg = null;
				await DialogFramework.CreateQuestion(ctx.Channel,
					$"Paste Message Id, where to set random reaction.", message => {
						ulong.TryParse(message, out var id);
						gt = DummyBot.Instance.GetGuild(ctx.Guild.Id);
						msg = gt.SetRandomReactionOnChannel.GetMessageAsync(id).Result;
						return msg != null;
					}, async e => {
						gt.SetRandomReactionOnMessageId = msg.Id;
						gt.SetRandomReactionOnMessage = msg;

						//Apply changes in task
						_ = DummyBot.Instance.GetGuild(ctx.Guild.Id).Initialize();

						//Continue to next step
						await Continue().ConfigureAwait(false);
					}, ctx.User.Id, timeoutBeforeDelete: TimeSpan.Zero, deleteAnswer: true,
					wrongAnswer: "Couldn't find that message. Try again.").ConfigureAwait(false);
			}
		}

		[Command("post-here")]
		public async Task PostHere(CommandContext ctx, DiscordMessage message, [RemainingText] string text) {
			await DialogFramework.CreateQuestion(ctx.Channel, text, new Answer[] {
				new Answer(EmojiProvider.One, async e =>{
					await DialogFramework.QuickVolatileMessage(ctx.Channel, "thx", TimeSpan.FromSeconds(1)).ConfigureAwait(false);
				})
			}, existingMessage: message,
			behavior: MessageBehavior.Permanent, deleteAnswer: true, deleteAnswerTimeout: TimeSpan.Zero);
		}

		[Command("post-here")]
		public async Task PostHere(CommandContext ctx, [RemainingText] string text) {
			await DialogFramework.CreateQuestion(ctx.Channel, text, new Answer[] {
				new Answer(EmojiProvider.One, async e =>{ 
					await DialogFramework.QuickVolatileMessage(ctx.Channel, "thx", TimeSpan.FromSeconds(1)).ConfigureAwait(false); 
				})
			}, behavior: MessageBehavior.Permanent, deleteAnswer: true, deleteAnswerTimeout: TimeSpan.Zero);
		}
		[Command("question")]
		public async Task Question(CommandContext ctx, [RemainingText] string text) {

			await ctx.Message.DeleteAsync().ConfigureAwait(false);

			await DialogFramework.CreateQuestion(ctx.Channel, text, result => true, async e => {
				var q = await e.Question.Channel.GetMessageAsync(e.Question.Id);
				await q.ModifyAsync(q.Content + Environment.NewLine + e.Message.Content).ConfigureAwait(false);
			}, ctx.User.Id, MessageBehavior.Permanent, deleteAnswer:true, deleteAnswerTimeout:TimeSpan.Zero,waitForMultipleAnswers:true);
		}
	}
}