using System;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus.Entities;

namespace ZBot.DialogFramework {
	public struct Answer {
		/// <summary>
		/// 
		/// </summary>
		/// <param name="emoji"></param>
		/// <param name="stringTokens"></param>
		/// <param name="onAnswer">Must return true if handled well. In case answer wasn't acceptable, should return false</param>
		public Answer(DiscordEmoji emoji, string[] stringTokens, Func<string, bool> validateAnswer, Func<AnswerArgs, Task> onAnswer) {
			Emoji = emoji;
			//Just to make sure it will always be lowercase
			StringTokens = stringTokens.Select(s => s.ToLower()).ToArray();
			ValidateAnswer = validateAnswer;
			if (onAnswer != default)
				OnAnswer = onAnswer;
			else
				OnAnswer = e => Task.FromResult(false);
		}

		public Answer(DiscordEmoji emoji, string[] stringTokens, Func<AnswerArgs, Task> onAnswer) {
			Emoji = emoji;
			//Just to make sure it will always be lowercase
			StringTokens = stringTokens.Select(s => s.ToLower()).ToArray();
			ValidateAnswer = s => true;
			if (onAnswer != default)
				OnAnswer = onAnswer;
			else
				OnAnswer = e => Task.FromResult(false);
		}

		public Answer(DiscordEmoji emoji, Action<AnswerArgs> onAnswer) {
			Emoji = emoji;
			StringTokens = new string[0];
			ValidateAnswer = s => true;
			if (onAnswer != default)
				OnAnswer = e => {
					Task.Run(() => onAnswer(e));
					return Task.FromResult(true); 
				};
			else
				OnAnswer = e => Task.FromResult(false);
		}

		public DiscordEmoji Emoji { get; }
		public Func<string, bool> ValidateAnswer { get; }
		public string[] StringTokens { get; }
		public Func<AnswerArgs, Task> OnAnswer { get; }

		public async Task InvokeFromEmoji(DiscordUser user, DiscordMessageState question) {
			await OnAnswer(new AnswerArgs(user, question));
		}

		public async Task InvokeFromMessage(DiscordMessageState message, DiscordMessageState question) {
			await OnAnswer(new AnswerArgs(message.Author, true, message, question));
		}

		public struct AnswerArgs {
			public AnswerArgs(DiscordUser user, bool isMessage, DiscordMessageState message, DiscordMessageState question) {
				User = user;
				IsMessage = isMessage;
				Message = message;
				Question = question;
			}

			public AnswerArgs(DiscordUser user, DiscordMessageState question) {
				User = user;
				IsMessage = false;
				Message = default;
				Question = question;
			}

			public DiscordUser User { get; }
			public bool IsMessage { get; }
			public DiscordMessageState Message { get; }
			public DiscordMessageState Question { get; }
		}

		public struct DiscordMessageState {
			public DiscordMessageState(DiscordChannel channel, string content, DiscordUser author, ulong id) {
				Channel = channel;
				Content = content;
				Author = author;
				Id = id;
			}

			//
			// Summary:
			//     Gets the channel in which the message was sent.
			public DiscordChannel Channel { get; }
			//
			// Summary:
			//     Gets the message's content.
			public string Content { get; }
			//
			// Summary:
			//     Gets the user or member that sent the message.
			public DiscordUser Author { get; }
			public ulong Id { get; }


			public static implicit operator DiscordMessageState(DiscordMessage m) => new DiscordMessageState(m.Channel, m.Content, m.Author, m.Id);
		}
	}
}