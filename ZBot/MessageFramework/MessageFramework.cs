using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus.Entities;
using OneOf;
using zLib;

namespace ZBot.MessageFramework {
	public static class MessageFramework {
		public static TimeSpan DefaultTimeout { get; set; } = TimeSpan.FromSeconds(30);
		public static TimeSpan DefaultTimeoutBeforeDelete { get; set; } = TimeSpan.FromSeconds(3);

		/// <summary>
		/// Will create a message on a given channel, or will find an existing in case of restoring a previous session,
		/// apply answer emojis and subscribe to emojis and text answers
		/// </summary>
		/// <param name="channel">A <see cref="DiscordChannel"/> where message should be</param>
		/// <param name="messageBody">A body of this message</param>
		/// <param name="answers">List of structs containing answers in form of emojis to click or written answers with callbacks</param>
		/// <param name="UserId">In case one user should answer this question</param>
		/// <param name="timeout">This message will be terminated after timeout</param>
		/// <param name="behaviour">What to do after timeout</param>
		/// <param name="existingMessageId">If there is a message already to subscribe to</param>
		/// <param name="reactionsAsButtons">Delete reaction rightaway for anonymosity or to use it as a button</param>
		/// <param name="waitForMultipleAnswers">If <see langword="false"/> it will unsubscribe after getting first answer</param>
		/// <returns></returns>
		public static async Task CreateMessage(DiscordChannel channel, string messageBody,
			IEnumerable<Answer> answers, ulong? UserId = null, MessageBehavior behaviour = MessageBehavior.Volatile,
			TimeSpan? timeout = null, string timeoutMessage = "Timed out", bool showTimeoutMessage = true,
			TimeSpan? timeoutBeforeDelete = null,
			ulong? existingMessageId = null, bool reactionsAsButtons = false, bool waitForMultipleAnswers = false) {

			DiscordMessage DMessage = null;
			if (timeout is null) timeout = DefaultTimeout;
			if (timeoutBeforeDelete is null) timeoutBeforeDelete = DefaultTimeoutBeforeDelete;
			//Find or create a message even if missing
			if (existingMessageId is ulong messageId) {
				DMessage = await channel.GetMessageAsync(messageId);
			} 
			if (DMessage == null) {
				DMessage = await channel.SendMessageAsync(messageBody);
				await SetReactions(DMessage, answers.Select(a => a.Emoji)).ConfigureAwait(false);
			}

			bool answered = false;
			bool unsubscribed = false;
			Func<Task> unsubscribeAndDismiss = async () => { };
			if (answers.Count() > 0) {
				var reactionAdded = new DSharpPlus.AsyncEventHandler<DSharpPlus.EventArgs.MessageReactionAddEventArgs>(async (e) => {
					if (e.Message.Id == DMessage.Id) {
						var answer = answers.FirstOrDefault(a => a.Emoji == e.Emoji);
						//TODO Findout what if it didn't find?
						if(reactionsAsButtons)
							await e.Message.DeleteReactionAsync(e.Emoji, e.User).ConfigureAwait(false);
						answered = true;
						await answer.InvokeFromEmoji(e.User).ConfigureAwait(false);
						//Should this be first to make sure it will unsubscribe immideately?
						if (!waitForMultipleAnswers) {
							unsubscribed = true;
							await unsubscribeAndDismiss().ConfigureAwait(false);
						}
					}
				});
				var messageAdded = new DSharpPlus.AsyncEventHandler<DSharpPlus.EventArgs.MessageCreateEventArgs>(async e => {
					//Checking if it from same channel and user if such is provided
					if (e.Channel.Id == DMessage.ChannelId && UserId.HasValue ? e.Author.Id == UserId.Value : true) {
						//Get list of all tokens ans Answer objects
						foreach (var answer in answers.SelectMany(a => a.StringTokens.Select(t => (Answer: a, Token: t)))) {
							if (e.Message.Content.Contains(answer.Token)) {
								answered = true;
								await answer.Answer.InvokeFromMessage(e.Message).ConfigureAwait(false);
								if (!waitForMultipleAnswers) {
									unsubscribed = true;
									await unsubscribeAndDismiss().ConfigureAwait(false);
								}
							}
						}
					}
				});
				unsubscribeAndDismiss = async () => {
					//Should we alse emplty this action? This one will be ready to be collected...
					if (unsubscribed) return;
					if (behaviour == MessageBehavior.Volatile || behaviour == MessageBehavior.KeepMessageAfterTimeout) {
						//unsubscribe later//TODO Check whether it actually works
						if (answers.Any(a => a.Emoji != null))
							Bot.Instance.Client.MessageReactionAdded -= reactionAdded;
						if (answers.Any(answers => answers.StringTokens.Length > 0))
							Bot.Instance.Client.MessageCreated -= messageAdded;
					}
					if (behaviour == MessageBehavior.Volatile) {
						//Should pause here
						await Task.Delay(timeoutBeforeDelete.Value.Milliseconds);
						await DMessage.DeleteAsync().ConfigureAwait(false);
					} else if (behaviour == MessageBehavior.KeepMessageAfterTimeout) {
						//TODO should we delete only bots reactions?
						await DMessage.DeleteAllReactionsAsync();
					}
				};
				//Subscribe to reactions and answers
				if (answers.Any(a=>a.Emoji != null))
					Bot.Instance.Client.MessageReactionAdded += reactionAdded;
				if (answers.Any(answers=>answers.StringTokens.Length > 0))
					Bot.Instance.Client.MessageCreated += messageAdded;
			}

			await Task.Delay(timeout.Value.Milliseconds);
			if (behaviour != MessageBehavior.Permanent) {
				await unsubscribeAndDismiss().ConfigureAwait(false);
				//Timeout message
				if (!answered && showTimeoutMessage) {
					await CreateMessage(channel, timeoutMessage, new Answer[0], timeout: TimeSpan.FromSeconds(3), showTimeoutMessage: false).ConfigureAwait(false);
				}
			}
		}

		private static async Task SetReactions(DiscordMessage message, IEnumerable<DiscordEmoji> emojis) {
			var existingReactions = message.Reactions.ToArray();
			var zippedReactions = existingReactions.Merge(emojis);
			Queue<(DiscordReaction reaction, DiscordEmoji emoji)> queuedReactions = new Queue<(DiscordReaction reaction, DiscordEmoji emoji)>(zippedReactions);
			var wrongReactions = false;
			while (queuedReactions.Count > 0) {
				var zip = queuedReactions.Dequeue();
				if (zip.reaction.Emoji != zip.emoji && !wrongReactions) wrongReactions = true;
				if (wrongReactions) {
					//remove the rest of reactions from message, add new ones
					if (zip.reaction != null)
						await message.DeleteReactionsEmojiAsync(zip.reaction.Emoji);
					if (zip.emoji != null)
						await message.CreateReactionAsync(zip.emoji);
				}
				//Should we clean non bot emojis in case it's an old message?
			}
		}

	}
}