using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using DSharpPlus.Entities;
using OneOf;
using zLib;

namespace ZBot.MessageFramework {
	public static class MessageFramework {
		public static TimeSpan DefaultTimeout { get; set; } = TimeSpan.FromSeconds(30);
		public static TimeSpan DefaultTimeoutBeforeDelete { get; set; } = TimeSpan.FromSeconds(3);
		public static TimeSpan DefaultDeleteAnswerTimeout { get; set; } = TimeSpan.FromSeconds(3);

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
		/// <param name="deleteAnswer">Delete reaction rightaway for anonymosity or to use it as a button</param>
		/// <param name="waitForMultipleAnswers">If <see langword="false"/> it will unsubscribe after getting first answer</param>
		/// <returns></returns>
		public static async Task CreateMessage(DiscordChannel channel, string messageBody,
			Answer[] answers, ulong? UserId = null, MessageBehavior behaviour = MessageBehavior.Volatile,
			TimeSpan? timeout = null, string timeoutMessage = "Timed out", bool showTimeoutMessage = true,
			TimeSpan? timeoutBeforeDelete = null, string wrongAnswer = "This isn't what I'm looking for. Acceptable keywords are: {0}",
			ulong? existingMessageId = null, bool deleteAnswer = false, TimeSpan? deleteAnswerTimeout = null, bool waitForMultipleAnswers = false) {

			Console.WriteLine("CreateMessage started");

			DiscordMessage DMessage = null;
			if (timeout is null) timeout = DefaultTimeout;
			if (timeoutBeforeDelete is null) timeoutBeforeDelete = DefaultTimeoutBeforeDelete;
			if (deleteAnswerTimeout is null) deleteAnswerTimeout = DefaultDeleteAnswerTimeout;
			//Find or create a message even if missing
			if (existingMessageId is ulong messageId) {
				DMessage = await channel.GetMessageAsync(messageId);
			} 
			if (DMessage == null) {
				DMessage = await channel.SendMessageAsync(messageBody);
				await SetReactions(DMessage, answers.Where(a=>a.Emoji != null)).ConfigureAwait(false);
			}

			bool answered = false;
			bool unsubscribed = false;
			//Should this do? It's ether suppress CS1998 or do this. Unless this will have some unpredictable result. Anyway, this Task should be .ConfigureAwait(false) 
			Func<Task> unsubscribeAndDismiss = async () => { await Task.Yield(); };
			PausableTimer timeoutTimer = new PausableTimer(timeout.Value.TotalMilliseconds);
			timeoutTimer.AutoReset = false;

			if (answers.Count() > 0) {
				Console.WriteLine("answers > 0");
				var reactionAdded = new DSharpPlus.AsyncEventHandler<DSharpPlus.EventArgs.MessageReactionAddEventArgs>(async (e) => {
					//Ignore own reactions and other messages
					if (e.User.IsCurrent || e.Message.Id != DMessage.Id) return;
					Console.WriteLine("Reaction added");

					var answer = answers.FirstOrDefault(a => a.Emoji == e.Emoji);

					Console.WriteLine($"answer.Emoji {answer.Emoji}");
					//TODO Findout what if it didn't find? Or use foreach, like in messages
					if (deleteAnswer)
						await ((Func<Task>)(async () => {
							Console.WriteLine($"deleteAnswer");
							await Task.Delay((int)deleteAnswerTimeout.Value.TotalMilliseconds);
							await e.Message.DeleteReactionAsync(e.Emoji, e.User);
						}))().ConfigureAwait(false);

					timeoutTimer.Pause();
					answered = await answer.InvokeFromEmoji(e.User);
					Console.WriteLine($"answered {answered}");

					if (!waitForMultipleAnswers && answered) {
						Console.WriteLine($"waitForMultipleAnswers = {waitForMultipleAnswers}");
						timeoutTimer.Stop();
						timeoutTimer.Dispose();
						await unsubscribeAndDismiss().ConfigureAwait(false);
						return;
					} else
						timeoutTimer.Release();
				});
				var messageAdded = new DSharpPlus.AsyncEventHandler<DSharpPlus.EventArgs.MessageCreateEventArgs>(async e => {
					//Ignore other channels and other users activity
					if (e.Channel.Id != DMessage.ChannelId || e.Author.IsCurrent || UserId.HasValue ? e.Author.Id != UserId.Value : false) return;
					Console.WriteLine($"messageAdded");

					//Get list of all tokens ans Answer objects
					foreach (var pair in answers.SelectMany(a => a.StringTokens.Select(t => (Answer: a, Token: t)))) {
						if (e.Message.Content.ToLower().Contains(pair.Token)) {

							Console.WriteLine($"Message.Content.ToLower().Contains(\"{pair.Token}\")");
							timeoutTimer.Pause();
							answered = await pair.Answer.InvokeFromMessage(e.Message);
							Console.WriteLine($"deleteAnswer = {deleteAnswer}");
							if (deleteAnswer)
								//Doing it afterwards is simpler
								await e.Message.DeleteAsync().ConfigureAwait(false);

							//what to do if answer wasn't accepted? should be handled internally? Quick response

							if (!waitForMultipleAnswers && answered) {
								Console.WriteLine($"!waitForMultipleAnswers && !answered = {!waitForMultipleAnswers && !answered}\nUnsubscribe");
								timeoutTimer.Stop();
								timeoutTimer.Dispose();
								await unsubscribeAndDismiss().ConfigureAwait(false);
								return;
							} else
								timeoutTimer.Release();
						}
					}
					//Seems like answer wasn't recognised. Should it have an event to expire? Or make a new method for that?

					Console.WriteLine($"wrong answer");
					await QuickVolatileMessage(channel, string.Format(wrongAnswer,
						string.Join(", ", answers.Where(a => a.StringTokens.Length > 0).SelectMany(a => a.StringTokens))),
						TimeSpan.FromSeconds(10)).ConfigureAwait(false);
				});
				unsubscribeAndDismiss = async () => {
					Console.WriteLine($"unsubscribeAndDismiss");
					//Should we also empty this action? This one will be ready to be collected...
					if (unsubscribed) return;
					Console.WriteLine($"unsubscribed {unsubscribed}");
					Console.WriteLine($"behaviour {behaviour}");
					if (behaviour == MessageBehavior.Volatile || behaviour == MessageBehavior.KeepMessageAfterTimeout) {
						//unsubscribe later//TODO Check whether it actually works
						Console.WriteLine($"unsubscribing");
						if (answers.Any(a => a.Emoji != null))
							Bot.Instance.Client.MessageReactionAdded -= reactionAdded;
						if (answers.Any(answers => answers.StringTokens.Length > 0))
							Bot.Instance.Client.MessageCreated -= messageAdded;
					}
					if (behaviour == MessageBehavior.Volatile) {
						//Should pause here
						Console.WriteLine($"wait and selete message");
						await Task.Delay((int)timeoutBeforeDelete.Value.TotalMilliseconds);
						await DMessage.DeleteAsync().ConfigureAwait(false);
					} else if (behaviour == MessageBehavior.KeepMessageAfterTimeout) {
						//TODO should we delete only bots reactions?
						Console.WriteLine($"delete reactions");
						await DMessage.DeleteAllReactionsAsync();
					}
				};
				//Subscribe to reactions and answers

				Console.WriteLine($"subscribe");
				if (answers.Any(a=>a.Emoji != null))
					Bot.Instance.Client.MessageReactionAdded += reactionAdded;
				if (answers.Any(answers=>answers.StringTokens.Length > 0))
					Bot.Instance.Client.MessageCreated += messageAdded;
			}
			timeoutTimer.Start();

			timeoutTimer.Elapsed += async (s, e) => {
				Console.WriteLine($"timeout");
				if (behaviour != MessageBehavior.Permanent && !answered) {
					await unsubscribeAndDismiss().ConfigureAwait(false);
					//Timeout message
					if (!answered && showTimeoutMessage) {
						await QuickVolatileMessage(channel, timeoutMessage, TimeSpan.FromSeconds(3)).ConfigureAwait(false);
					}
				}
			};
		}

		/// <summary>
		/// Will create a message on a given channel, or will find an existing in case of restoring a previous session,
		/// and subscribe to any text answers that will follow until timeout.
		/// </summary>
		/// <param name="channel">A <see cref="DiscordChannel"/> where message should be</param>
		/// <param name="messageBody">A body of this message</param>
		/// <param name="onAnyAnswer">callback fired on any text answer</param>
		/// <param name="UserId">In case one user should answer this question</param>
		/// <param name="timeout">This message will be terminated after timeout</param>
		/// <param name="behaviour">What to do after timeout</param>
		/// <param name="existingMessageId">If there is a message already to subscribe to</param>
		/// <param name="deleteAnswer">Delete reaction rightaway for anonymosity or to use it as a button</param>
		/// <param name="waitForMultipleAnswers">If <see langword="false"/> it will unsubscribe after getting first answer</param>
		/// <returns></returns>
		public static async Task CreateMessage(DiscordChannel channel, string messageBody,
			Func<Answer.AnswerArgs, Task<bool>> onAnyAnswer, ulong? UserId = null, MessageBehavior behaviour = MessageBehavior.Volatile,
			TimeSpan? timeout = null, string timeoutMessage = "Timed out", bool showTimeoutMessage = true,
			TimeSpan? timeoutBeforeDelete = null, string wrongAnswer = "This isn't what I'm looking for. Acceptable keywords are: {0}",
			ulong? existingMessageId = null, bool deleteAnswer = false, TimeSpan? deleteAnswerTimeout = null, bool waitForMultipleAnswers = false) =>

			await CreateMessage(channel, messageBody, new Answer[] { 
					new Answer(null, new string[] { "" }, onAnyAnswer) },
				UserId, behaviour, timeout, timeoutMessage, showTimeoutMessage, timeoutBeforeDelete,
				wrongAnswer, existingMessageId, deleteAnswer, deleteAnswerTimeout, waitForMultipleAnswers);

		private static async Task SetReactions(DiscordMessage message, IEnumerable<Answer> answers, bool processOld = false, bool deleteOld = true) {
			var existingReactions = message.Reactions;
			var answerEmojis = answers.Select(a => a.Emoji).ToArray();

			bool reactionsOk = existingReactions.Count() == answers.Count() ?
				existingReactions.Select(r => r.Emoji).Zip(answerEmojis).All(p => p.First == p.Second) : false;

			if (reactionsOk) {
				//Clean others
				foreach (var p in existingReactions.SelectMany(r => message.GetReactionsAsync(r.Emoji).Result.Select((u) => (User: u, Reaction: r)))) {
					if (processOld)
						await answers.FirstOrDefault(a => a.Emoji == p.Reaction.Emoji).InvokeFromEmoji(p.User);
					if (deleteOld)
						await message.DeleteReactionAsync(p.Reaction.Emoji, p.User);
				}
			} else {
				//Clear, replace
				await message.DeleteAllReactionsAsync();
				foreach (var emoji in answerEmojis) {
					await message.CreateReactionAsync(emoji);
				}
			}
		}

		private static TimeSpan DefaultQuickMessageTimeout { get; set; } = TimeSpan.FromSeconds(5);
		public static async Task QuickVolatileMessage(DiscordChannel channel, string message, TimeSpan? timeout = null) {
			if (!timeout.HasValue)
				timeout = DefaultQuickMessageTimeout;

			var DMessage = await channel.SendMessageAsync(message);
			await Task.Delay((int)timeout.Value.TotalMilliseconds);

			await DMessage.DeleteAsync();
		}
	}
}