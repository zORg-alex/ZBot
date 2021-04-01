using System;
using System.Threading.Tasks;
using DSharpPlus.Entities;

namespace ZBot.MessageFramework {
	public struct Answer {
		public Answer(DiscordEmoji emoji, string[] stringTokens, Func<AnswerArgs, Task> onAnswer) {
			Emoji = emoji;
			StringTokens = stringTokens;
			if (onAnswer != default)
				OnAnswer = onAnswer;
			else
				OnAnswer = e => { return null; };
		}

		public DiscordEmoji Emoji { get; }
		public string[] StringTokens { get; }
		public Func<AnswerArgs, Task> OnAnswer { get; }

		public async Task InvokeFromEmoji(DiscordUser user) {
			await OnAnswer(new AnswerArgs(user));
		}

		public async Task InvokeFromMessage(DiscordMessage message) {
			await OnAnswer(new AnswerArgs(message.Author, true, message));
		}

		public struct AnswerArgs {
			public AnswerArgs(DiscordUser user, bool isMessage, DiscordMessage message) {
				User = user;
				IsMessage = isMessage;
				Message = message;
			}

			public AnswerArgs(DiscordUser user) {
				User = user;
				IsMessage = false;
				Message = null;
			}

			public DiscordUser User { get; }
			public bool IsMessage { get; }
			public DiscordMessage Message { get; }
		}
	}
}