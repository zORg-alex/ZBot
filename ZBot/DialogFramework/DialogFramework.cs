using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Emzi0767.Utilities;
using zLib;

namespace ZBot.DialogFramework {
	public static class DialogFramework {
		public static TimeSpan DefaultTimeout { get; set; } = TimeSpan.FromSeconds(30);
		public static TimeSpan DefaultTimeoutBeforeDelete { get; set; } = TimeSpan.FromSeconds(3);
		public static TimeSpan DefaultDeleteAnswerTimeout { get; set; } = TimeSpan.FromSeconds(3);

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
			var Timeout = timeout.HasValue ? timeout.Value : DefaultQuickMessageTimeout;

			var Message = await channel.SendMessageAsync(message).ConfigureAwait(false);
			await Task.Delay((int)Timeout.TotalMilliseconds);

			await Message.DeleteAsync();
		}

		private static TimeSpan DefaultQuickMessageUpdateStep { get; set; } = TimeSpan.FromSeconds(5);
		public static async Task QuickVolatileMessage(DiscordChannel channel, Func<string> message, TimeSpan? updateStep = null, TimeSpan? timeout = null) {
			var Timeout = timeout.HasValue ? timeout.Value : DefaultQuickMessageTimeout;
			var UpdateStep = updateStep.HasValue ? updateStep.Value : DefaultQuickMessageUpdateStep;

			if (UpdateStep > Timeout) {
				await QuickVolatileMessage(channel, message(), Timeout).ConfigureAwait(false);
				return;
			}

			DateTime timeoutTime = DateTime.Now + Timeout;

			var Message = await channel.SendMessageAsync(message()).ConfigureAwait(false);

			while(timeoutTime > DateTime.Now) {
				await Task.Delay((int)(UpdateStep.TotalMilliseconds % (timeoutTime - DateTime.Now).TotalMilliseconds));
				await Message.ModifyAsync(message()).ConfigureAwait(false);
			}
			await Message.DeleteAsync();
		}

		public static async Task<DiscordMessage> CreateQuestion(DiscordChannel channel, string messageBody, IEnumerable<Answer> answers,
			ulong? UserId = null, MessageBehavior behavior = MessageBehavior.Volatile,
			TimeSpan? timeout = null, string timeoutMessage = "Timeout", bool showTimeoutMessage = true,
			TimeSpan? timeoutBeforeDelete = null, string wrongAnswer = "This isn't what I'm looking for.",
			DiscordMessage existingMessage = null, bool deleteAnswer = false, TimeSpan? deleteAnswerTimeout = null,
			bool waitForMultipleAnswers = false) {

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

			OnCancel += (g, m) => {
				if (g.Id == channel.Guild.Id && m.Id == Question.Id)
					_ = UnsubscribeAndDismiss(true);
			};

			//Do I need to even start it in case of Permanent behavior?
			timeoutTimer.Start();

			//This will hold this method from returning until it will receive right answer or get deleted
			//This will give an opportunity to nest dialogs one after another
			while (!unsubscribed && behavior != MessageBehavior.Permanent) {
				await Task.Delay(100);
			}
			//Permanent fall through and return it's message object
			return Question;


			async Task ReactionAdded(DiscordClient c, MessageReactionAddEventArgs e) {
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

			async Task MessageAdded(DiscordClient c, MessageCreateEventArgs e) {
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

			async Task UnsubscribeAndDismiss(bool force = false) {
				if (unsubscribed) return;
				unsubscribed = true;
				if (behavior != MessageBehavior.Permanent || force) {
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

			Task OnDeleted(DiscordClient c, MessageDeleteEventArgs e) {
				if (e.Message.Id != Question.Id)
					return Task.CompletedTask;
				unsubscribed = true;
				Bot.Instance.Client.MessageDeleted -= OnDeleted;
				Bot.Instance.Client.MessageCreated -= MessageAdded;
				return Task.CompletedTask;
			}
		}

		private static event Action<DiscordGuild, DiscordMessage> OnCancel;
		public static void CancelQuestion(DiscordGuild Guild, DiscordMessage Message) {
			OnCancel.Invoke(Guild, Message);
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
			DiscordMessage existingMessage = null, bool deleteAnswer = false, TimeSpan? deleteAnswerTimeout = null,
			bool waitForMultipleAnswers = false) {

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

			OnCancel += (g, m) => {
				if (g.Id == channel.Guild.Id && m.Id == Question.Id)
					_ = UnsubscribeAndDismiss(true);
			};

			//cancellationToken.Register(UnsubscribeAndDismiss().Wait);
			//Do I need to even start it in case of Permanent behavior?
			timeoutTimer.Start();

			//This will hold this method from returning until it will receive right answer or get deleted
			//This will give an opportunity to nest dialogs one after another
			while(!unsubscribed) {
				await Task.Delay(100);
			}

			Task MessageAdded(DiscordClient c, MessageCreateEventArgs e) {

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

			async Task UnsubscribeAndDismiss(bool force = false) {
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
			Task OnDeleted(DiscordClient c, MessageDeleteEventArgs e) {
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