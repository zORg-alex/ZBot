using System;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using ZBot.DialogFramework;

namespace DummyBot.ConsoleApp {
	public class DummyCommands : BaseCommandModule {

		[Command("dummy-config")]
		[Aliases("config")]
		public async Task ReadConfig(CommandContext ctx) {

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
				await MessageFramework.CreateMessage(ctx.Channel,
					$"What we should set up?" + Environment.NewLine +
					$"{GildEmoji.One} Voice channel to connect to " + Environment.NewLine +
					$"{GildEmoji.Two} Message id to react to",
					new Answer[] { 
					new Answer(GildEmoji.One, new string[] { "one", "voice", "channel" }, async e=>{
						await SetVoiceChannel().ConfigureAwait(false);
						return true;
					}), new Answer(GildEmoji.Two, new string[] { "two", "message", "react"}, async e=>{
						await SetMessageChannel().ConfigureAwait(false);
						return true;
					})},
					ctx.User.Id, timeoutBeforeDelete: TimeSpan.Zero, deleteAnswer: true).ConfigureAwait(false);
			}
			//Continue configuring or quit?
			async Task Continue() {
				//ask
				await MessageFramework.CreateMessage(ctx.Channel,
					"Would you go over?",
					new Answer[] {
						new Answer(GildEmoji.CheckMarkOnGreen, new string[] { "yes", "sure", "go" }, async e => {
							await MainMenu().ConfigureAwait(false);
							return true;
					}),
						new Answer(GildEmoji.CrossOnGreen, new string[] { "no", "stop", "done" }, async e=>{
							await MessageFramework.QuickVolatileMessage(ctx.Channel, "Thank you. We are done here. Have a nice day.").ConfigureAwait(false);
							return true;
						})
					},
					ctx.User.Id, timeoutBeforeDelete: TimeSpan.Zero, deleteAnswer: true).ConfigureAwait(false);
			}
			//Setvoice channel
			async Task SetVoiceChannel() {
				//Message
				await MessageFramework.CreateMessage(ctx.Channel,
					$"Enter channel name, to connect to.",
					async e => {
						var channel = ctx.Guild.Channels.FirstOrDefault(c => c.Value.Name.Contains(e.Message.Content)).Value;
						if (channel != null) {
							//ok
							var gt = DummyBot.Instance.GetGuild(ctx.Guild.Id);
							gt.ConnectToVoiceName = channel.Name;
							gt.ConnectToVoice = channel;

							//Apply changes in task
							await DummyBot.Instance.GetGuild(ctx.Guild.Id).Initialize().ConfigureAwait(false);


							//Continue to next step
							await Continue().ConfigureAwait(false);
							return true;
						} else
							//retry
							return false;
					}, ctx.User.Id, timeoutBeforeDelete: TimeSpan.Zero, deleteAnswer: true,
					wrongAnswer: "Couldn't find that channel. Try again.").ConfigureAwait(false);
			}
			//Set channel id to react on a message
			async Task SetMessageChannel() {
				await MessageFramework.CreateMessage(ctx.Channel,
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
					$"Paste Channel Id, where to set random reaction.", async e => {
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
						ulong.TryParse(e.Message.Content, out var id);
						var channel = ctx.Guild.GetChannel(id);
						if (channel != null) {
							var gt = DummyBot.Instance.GetGuild(ctx.Guild.Id);
							gt.SetRandomReactionOnChannelId = id;
							gt.SetRandomReactionOnChannel = channel;


							//Continue to next step
							await SetMessage().ConfigureAwait(false);
							return true;
						} else
							return false;
					}, ctx.User.Id, timeoutBeforeDelete: TimeSpan.Zero, deleteAnswer: true,
					wrongAnswer: "Couldn't find that channel. Try again.").ConfigureAwait(false);
			}
			//Set message id to react to
			async Task SetMessage() {
				await MessageFramework.CreateMessage(ctx.Channel,
					$"Paste Message Id, where to set random reaction.", async e => {
						ulong.TryParse(e.Message.Content, out var id);
						var gt = DummyBot.Instance.GetGuild(ctx.Guild.Id);
						var message = await gt.SetRandomReactionOnChannel.GetMessageAsync(id);
						if (message != null) {
							gt.SetRandomReactionOnMessageId = id;
							gt.SetRandomReactionOnMessage = message;

							//Apply changes in task
							await DummyBot.Instance.GetGuild(ctx.Guild.Id).Initialize().ConfigureAwait(false);


							//Continue to next step
							await Continue().ConfigureAwait(false);
							return true;
						} else {
							return false;
						}
					}, ctx.User.Id, timeoutBeforeDelete: TimeSpan.Zero, deleteAnswer: true,
					wrongAnswer: "Couldn't find that message. Try again.").ConfigureAwait(false);
			}
		}
	}
}