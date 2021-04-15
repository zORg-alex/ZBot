using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using zLib;

namespace ZBot.DialogFramework {
	public static class DialogFramework {
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
			IEnumerable<Answer> answers, ulong? UserId = null, MessageBehavior behaviour = MessageBehavior.Volatile,
			TimeSpan? timeout = null, string timeoutMessage = "Timed out", bool showTimeoutMessage = true,
			TimeSpan? timeoutBeforeDelete = null, string wrongAnswer = "This isn't what I'm looking for. Acceptable keywords are: {0}",
			DiscordMessage existingMessage = null, bool deleteAnswer = false, TimeSpan? deleteAnswerTimeout = null, bool waitForMultipleAnswers = false) {


			SetDefaultValuesForTimeouts(ref timeout, ref timeoutBeforeDelete, ref deleteAnswerTimeout);

			//Find or create a message even if missing
			DiscordMessage DMessage = await GetOrCreateMessage(channel, messageBody, existingMessage).ConfigureAwait(false);
			
			await SetReactions(DMessage, answers.Where(a=>a.Emoji != null),true).ConfigureAwait(false);

			bool answered = false;
			bool unsubscribed = false;
			//Should this do? It's ether suppress CS1998 or do this. Unless this will have some unpredictable result. Anyway, this Task should be .ConfigureAwait(false) 
			Func<Task> unsubscribeAndDismiss = async () => { await Task.Yield(); };
			PausableTimer timeoutTimer = new PausableTimer(timeout.Value.TotalMilliseconds);
			timeoutTimer.AutoReset = false;

			//Some optimizations
			var answersFromTokens = answers.SelectMany(a=>a.StringTokens.Select(t=>(Answer:a,Token:t))).ToArray();
			var answersFromEmojiIds = answers.ToDictionary(a=>a.Emoji);

			if (answers.Count() > 0) {
				var reactionAdded = new DSharpPlus.AsyncEventHandler<DSharpPlus.EventArgs.MessageReactionAddEventArgs>(async (e) => {
					//Ignore own reactions and other messages
					if (e.User.IsCurrent || e.Message.Id != DMessage.Id) return;
					//TODO Findout what if it didn't find? Or use foreach, like in messages
					if (deleteAnswer)
						await ((Func<Task>)(async () => {
							await Task.Delay((int)deleteAnswerTimeout.Value.TotalMilliseconds);
							await e.Message.DeleteReactionAsync(e.Emoji, e.User);
						}))().ConfigureAwait(false);

					if (answersFromEmojiIds.ContainsKey(e.Emoji)) {

						var answer = answersFromEmojiIds[e.Emoji];

						timeoutTimer.Pause();

						//Do we need to check if there is another answer set already, just in case of hickup
						//TODO It's a reaction answer. Shouldn't it be true by default?
						answered = true;
						_ = answer.InvokeFromEmoji(e.User, DMessage);
					}

					if (!waitForMultipleAnswers && answered) {
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

					//Get list of all tokens ans Answer objects
					foreach (var pair in answersFromTokens) {
						if (e.Message.Content.ToLower().Contains(pair.Token)) {

							timeoutTimer.Pause();
							//???Do we really need to get answer value if we already found a token?
							answered = pair.Answer.ValidateAnswer(e.Message.Content);
							_ = pair.Answer.InvokeFromMessage(e.Message, DMessage);
							if (deleteAnswer)
								//Doing it afterwards is simpler
								await e.Message.DeleteAsync().ConfigureAwait(false);

							//what to do if answer wasn't accepted? should be handled internally? Quick response

							if (!waitForMultipleAnswers && answered) {
								timeoutTimer.Stop();
								timeoutTimer.Dispose();
								await unsubscribeAndDismiss().ConfigureAwait(false);
								return;
							} else
								timeoutTimer.Release();
						}
					}
					//Seems like answer wasn't recognised. Should it have an event to expire? Or make a new method for that?

					await QuickVolatileMessage(channel, string.Format(wrongAnswer,
						string.Join(", ", answers.Where(a => a.StringTokens.Length > 0).SelectMany(a => a.StringTokens))),
						TimeSpan.FromSeconds(10)).ConfigureAwait(false);
				});
				unsubscribeAndDismiss = async () => {
					//Should we also empty this action? This one will be ready to be collected...
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
						await Task.Delay((int)timeoutBeforeDelete.Value.TotalMilliseconds);
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
			timeoutTimer.Start();

			timeoutTimer.Elapsed += async (s, e) => {
				//Do we need to dispose it? Or just leave it?
				//In case of permanent behavior it will just skip and won't unsubscribe, just what we need
				timeoutTimer.Stop();
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
			Func<string, bool> validateAnswer, Func<Answer.AnswerArgs, Task> onAnyAnswer, ulong? UserId = null, MessageBehavior behaviour = MessageBehavior.Volatile,
			TimeSpan? timeout = null, string timeoutMessage = "Timed out", bool showTimeoutMessage = true,
			TimeSpan? timeoutBeforeDelete = null, string wrongAnswer = "This isn't what I'm looking for. Acceptable keywords are: {0}",
			DiscordMessage existingMessage = null, bool deleteAnswer = false, TimeSpan? deleteAnswerTimeout = null, bool waitForMultipleAnswers = false) =>

			await CreateMessage(channel, messageBody, new Answer[] { 
					new Answer(null, new string[] { "" }, validateAnswer, onAnyAnswer) },
				UserId, behaviour, timeout, timeoutMessage, showTimeoutMessage, timeoutBeforeDelete,
				wrongAnswer, existingMessage, deleteAnswer, deleteAnswerTimeout, waitForMultipleAnswers);

		private static async Task SetReactions(DiscordMessage message, IEnumerable<Answer> answers, bool processOld = false, bool deleteOld = true) {
			var existingReactions = message.Reactions;
			var answerEmojis = answers.Select(a => a.Emoji).ToArray();

			bool reactionsOk = existingReactions.Count() == answers.Count() ?
				existingReactions.Select(r => r.Emoji).Zip(answerEmojis).All(p => p.First.Id == p.Second.Id) : false;

			if (reactionsOk) {
				//Clean others
				foreach (var p in existingReactions.SelectMany(r => message.GetReactionsAsync(r.Emoji).Result.Select((u) => (User: u, Reaction: r)))) {
					if (p.User.IsCurrent) continue;
					if (processOld && answerEmojis.Contains(p.Reaction.Emoji))
						await answers.FirstOrDefault(a => a.Emoji == p.Reaction.Emoji).InvokeFromEmoji(p.User, message);
					if (deleteOld)
						await message.DeleteReactionAsync(p.Reaction.Emoji, p.User);
				}
			} else {
				//Clear, replace
				await message.DeleteAllReactionsAsync();
				foreach (var emoji in answerEmojis) {
					await Task.Delay(100);//Will it suffice?
					await message.CreateReactionAsync(emoji);
				}
			}
		}

		private static TimeSpan DefaultQuickMessageTimeout { get; set; } = TimeSpan.FromSeconds(5);
		public static async Task QuickVolatileMessage(DiscordChannel channel, string message, TimeSpan? timeout = null) {
			if (!timeout.HasValue)
				timeout = DefaultQuickMessageTimeout;

			var Message = await channel.SendMessageAsync(message).ConfigureAwait(false);
			await Task.Delay((int)timeout.Value.TotalMilliseconds);

			await Message.DeleteAsync();
		}

		public static async Task<DiscordMessage> CreateQuestion(DiscordChannel channel, string messageBody, IEnumerable<Answer> answers,
			ulong? UserId = null, MessageBehavior behavior = MessageBehavior.Volatile,
			TimeSpan? timeout = null, string timeoutMessage = "Timeout", bool showTimeoutMessage = true,
			TimeSpan? timeoutBeforeDelete = null, string wrongAnswer = "This isn't what I'm looking for.",
			DiscordMessage existingMessage = null, bool deleteAnswer = false, TimeSpan? deleteAnswerTimeout = null, bool waitForMultipleAnswers = false) {

			SetDefaultValuesForTimeouts(ref timeout, ref timeoutBeforeDelete, ref deleteAnswerTimeout);
			DiscordMessage Question = await GetOrCreateMessage(channel, messageBody, existingMessage).ConfigureAwait(false);

			//Some optimizations
			var answersFromTokens = answers.SelectMany(a => a.StringTokens.Select(t => (Answer: a, Token: t))).ToArray();
			var answersFromEmojiIds = answers.ToDictionary(a => a.Emoji);

			_ = SetReactions(Question, answers.Where(a => a.Emoji != null), true);

			bool answered = false;
			bool unsubscribed = false;

			PausableTimer timeoutTimer = new PausableTimer(timeout.Value.TotalMilliseconds);
			timeoutTimer.AutoReset = false;

			timeoutTimer.Elapsed += async (s, e) => {
				//Do we need to dispose it? Or just leave it?
				//In case of permanent behavior it will just skip and won't unsubscribe, just what we need
				timeoutTimer.Stop();
				if (behavior != MessageBehavior.Permanent && !answered) {
					_ = UnsubscribeAndDismiss();
					//Timeout message
					if ((!answered || waitForMultipleAnswers) && showTimeoutMessage) {
						await QuickVolatileMessage(channel, timeoutMessage, TimeSpan.FromSeconds(3)).ConfigureAwait(false);
					}
				}
			};

			if (answersFromEmojiIds.Count > 0)
				Bot.Instance.Client.MessageReactionAdded += ReactionAdded;
			if (answersFromTokens.Count() > 0)
				Bot.Instance.Client.MessageCreated += MessageAdded;
			Bot.Instance.Client.MessageDeleted += OnDeleted;
			//Do I need to even start it in case of Permanent behavior?
			timeoutTimer.Start();

			//This will hold this method from returning until it will receive right answer or get deleted
			//This will give an opportunity to nest dialogs one after another
			while (!unsubscribed && behavior != MessageBehavior.Permanent) {
				await Task.Delay(100);
			}
			//Permanent fall through and return it's message object
			return Question;


			async Task ReactionAdded(MessageReactionAddEventArgs e) {
				//Ignore own reactions and other messages
				if (e.User.IsCurrent || e.Message.Id != Question.Id) return;

				if (deleteAnswer)
					await((Func<Task>)(async () => {
						await Task.Delay((int)deleteAnswerTimeout.Value.TotalMilliseconds);
						await e.Message.DeleteReactionAsync(e.Emoji, e.User);
					}))().ConfigureAwait(false);

				if (answersFromEmojiIds.ContainsKey(e.Emoji)) {

					var answer = answersFromEmojiIds[e.Emoji];

					timeoutTimer.Pause();

					answered = true;
					_ = answer.InvokeFromEmoji(e.User, Question);
				}

				//There is a case when you need to keep it working, like a reaction role thing
				if (!waitForMultipleAnswers && answered && behavior != MessageBehavior.Permanent) {
					timeoutTimer.Stop();
					timeoutTimer.Dispose();
					await UnsubscribeAndDismiss().ConfigureAwait(false);
					return;
				} else
					timeoutTimer.Release();
			}

			async Task MessageAdded(MessageCreateEventArgs e) {
				//Ignore other channels and other users activity
				if (e.Channel.Id != Question.ChannelId || e.Author.IsCurrent || UserId.HasValue ? e.Author.Id != UserId.Value : false) return;

				//Get list of all tokens ans Answer objects
				foreach (var pair in answersFromTokens) {
					if (e.Message.Content.ToLower().Contains(pair.Token)) {

						timeoutTimer.Pause();

						//If there is a blank token, we will need a validation
						answered = pair.Answer.ValidateAnswer(e.Message.Content);
						_ = pair.Answer.InvokeFromMessage(e.Message, Question);
						if (deleteAnswer)
							//Doing it afterwards is simpler
							await e.Message.DeleteAsync().ConfigureAwait(false);

						//what to do if answer wasn't accepted? should be handled internally? Quick response

						if (!waitForMultipleAnswers && answered) {
							timeoutTimer.Stop();
							timeoutTimer.Dispose();
							await UnsubscribeAndDismiss().ConfigureAwait(false);
							return;
						} else
							timeoutTimer.Release();
					}
				}
				//Seems like answer wasn't recognised. Should it have an event to expire? Or make a new method for that?

				if (!answered)
					await QuickVolatileMessage(channel, string.Format(wrongAnswer,
						string.Join(", ", answers.Where(a => a.StringTokens.Length > 0).SelectMany(a => a.StringTokens))),
						TimeSpan.FromSeconds(10)).ConfigureAwait(false);
			}

			async Task UnsubscribeAndDismiss() {
				if (unsubscribed) return;
				unsubscribed = true;
				if (behavior != MessageBehavior.Permanent) {
					//Can I unsubscribe if not even subscribed?
					Bot.Instance.Client.MessageReactionAdded -= ReactionAdded;
					Bot.Instance.Client.MessageCreated -= MessageAdded;
				}
				if (behavior == MessageBehavior.Volatile) {
					//Should pause here
					await Task.Delay((int)timeoutBeforeDelete.Value.TotalMilliseconds).ConfigureAwait(false);
					await Question.DeleteAsync().ConfigureAwait(false);
				}
			}

			Task OnDeleted(MessageDeleteEventArgs e) {
				if (e.Message.Id != Question.Id)
					return Task.CompletedTask;
				unsubscribed = true;
				Bot.Instance.Client.MessageDeleted -= OnDeleted;
				Bot.Instance.Client.MessageCreated -= MessageAdded;
				return Task.CompletedTask;
			}
		}

		/// <summary>
		/// Waits any text answer
		/// </summary>
		/// <param name="channel">A <see cref="DiscordChannel"/> where message should be</param>
		/// <param name="messageBody">A body of this message</param>
		/// <param name="validateAnswer">returns true if an answer is acceptable, or false to throw error message</param>
		/// <param name="onValidatedAnswer">Do any demanding task here to process this answer</param>
		/// <param name="UserId">In case one user should answer this question</param>
		/// <param name="behavior">What to do after timeout</param>
		/// <param name="timeout">This message will be terminated after timeout</param>
		/// <param name="timeoutMessage"></param>
		/// <param name="showTimeoutMessage"></param>
		/// <param name="timeoutBeforeDelete"></param>
		/// <param name="wrongAnswer"></param>
		/// <param name="existingMessage">Existing <see cref="DiscordMessage"/> to modify and to subscribe to</param>
		/// <param name="deleteAnswer"></param>
		/// <param name="deleteAnswerTimeout"></param>
		/// <param name="waitForMultipleAnswers">If <see langword="false"/> it will unsubscribe after getting first answer</param>
		public static async Task CreateQuestion(DiscordChannel channel, string messageBody,
			Func<string, bool> validateAnswer, Func<Answer.AnswerArgs, Task> onValidatedAnswer,
			ulong? UserId = null, MessageBehavior behavior = MessageBehavior.Volatile,
			TimeSpan? timeout = null, string timeoutMessage = "Timeout", bool showTimeoutMessage = true,
			TimeSpan? timeoutBeforeDelete = null, string wrongAnswer = "This isn't what I'm looking for.",
			DiscordMessage existingMessage = null, bool deleteAnswer = false, TimeSpan? deleteAnswerTimeout = null, bool waitForMultipleAnswers = false) {

			SetDefaultValuesForTimeouts(ref timeout, ref timeoutBeforeDelete, ref deleteAnswerTimeout);
			DiscordMessage Question = await GetOrCreateMessage(channel, messageBody, existingMessage).ConfigureAwait(false);

			bool answered = false;
			bool unsubscribed = false;

			PausableTimer timeoutTimer = new PausableTimer(timeout.Value.TotalMilliseconds);
			timeoutTimer.AutoReset = false;

			timeoutTimer.Elapsed += async (s, e) => {
				//Do we need to dispose it? Or just leave it?
				//In case of permanent behavior it will just skip and won't unsubscribe, just what we need
				timeoutTimer.Stop();
				if (behavior != MessageBehavior.Permanent && !answered) {
					_ = UnsubscribeAndDismiss();
					//Timeout message
					if ((!answered || waitForMultipleAnswers) && showTimeoutMessage) {
						await QuickVolatileMessage(channel, timeoutMessage, TimeSpan.FromSeconds(3)).ConfigureAwait(false);
					}
				}
			};

			Bot.Instance.Client.MessageCreated += MessageAdded;
			Bot.Instance.Client.MessageDeleted += OnDeleted;
			//Do I need to even start it in case of Permanent behavior?
			timeoutTimer.Start();

			//This will hold this method from returning until it will receive right answer or get deleted
			//This will give an opportunity to nest dialogs one after another
			while(!unsubscribed) {
				await Task.Delay(100);
			}

			Task MessageAdded(MessageCreateEventArgs e) {

				//Ignore other channels and other users activity
				if (e.Channel.Id != Question.ChannelId || e.Author.IsCurrent || UserId.HasValue ? e.Author.Id != UserId.Value : false)
					return Task.CompletedTask;
				timeoutTimer.Pause();
				//save answered state for further use, but keep local copy for this event, it can be changed outside
				var a = answered = validateAnswer(e.Message.Content);

				if (a && !waitForMultipleAnswers) {
					timeoutTimer.Stop();
					_ = UnsubscribeAndDismiss();
				}
				if (a) {
					_ = onValidatedAnswer(new Answer.AnswerArgs(e.Author, true, e.Message, Question));
				} else {
					//wrong answer
					_ = QuickVolatileMessage(channel, wrongAnswer, timeoutBeforeDelete);
				}

				if (deleteAnswer)
					_ = DeleteMessageAfter(e.Message, deleteAnswerTimeout.Value.Milliseconds).ConfigureAwait(false);
				if (waitForMultipleAnswers || !answered)
					timeoutTimer.Release();
				return Task.CompletedTask;//Nice hack, thank's VS
			}

			async Task UnsubscribeAndDismiss() {
				if (unsubscribed) return;
				unsubscribed = true;
				if (behavior != MessageBehavior.Permanent) {
					Bot.Instance.Client.MessageCreated -= MessageAdded;
				}
				if (behavior == MessageBehavior.Volatile) {
					//Should pause here
					await Task.Delay((int)timeoutBeforeDelete.Value.TotalMilliseconds).ConfigureAwait(false);
					await Question.DeleteAsync().ConfigureAwait(false);
				}
			}
			Task OnDeleted(MessageDeleteEventArgs e) {
				if (e.Message.Id != Question.Id)
					return Task.CompletedTask;
				unsubscribed = true;
				Bot.Instance.Client.MessageDeleted -= OnDeleted;
				Bot.Instance.Client.MessageCreated -= MessageAdded;
				return Task.CompletedTask;
			}
		}

		private static async Task DeleteMessageAfter(DiscordMessage message, int milliseconds) {
			await Task.Delay(milliseconds);
			await message.DeleteAsync().ConfigureAwait(false);
		}

		private static async Task<DiscordMessage> GetOrCreateMessage(DiscordChannel channel, string messageBody, DiscordMessage existingMessage) {
			DiscordMessage message;
			if (existingMessage != null) {
				message = existingMessage;
				if (message.Content != messageBody)
					_ = message.ModifyAsync(messageBody);
			} else {
				message = await channel.SendMessageAsync(messageBody);
			}

			return message;
		}

		private static void SetDefaultValuesForTimeouts(ref TimeSpan? timeout, ref TimeSpan? timeoutBeforeDelete, ref TimeSpan? deleteAnswerTimeout) {
			if (timeout is null) timeout = DefaultTimeout;
			if (timeoutBeforeDelete is null) timeoutBeforeDelete = DefaultTimeoutBeforeDelete;
			if (deleteAnswerTimeout is null) deleteAnswerTimeout = DefaultDeleteAnswerTimeout;
		}
	}
}