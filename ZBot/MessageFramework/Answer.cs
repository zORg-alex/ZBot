using System;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus.Entities;

namespace ZBot.MessageFramework {
	public struct Answer {
		/// <summary>
		/// 
		/// </summary>
		/// <param name="emoji"></param>
		/// <param name="stringTokens"></param>
		/// <param name="onAnswer">Must return true if handled well. In case answer wasn't acceptable, should return false</param>
		public Answer(DiscordEmoji emoji, string[] stringTokens, Func<AnswerArgs, Task<bool>> onAnswer) {
			Emoji = emoji;
			//Just to make sure it will always be lowercase
			StringTokens = stringTokens.Select(s=>s.ToLower()).ToArray();
			if (onAnswer != default)
				OnAnswer = onAnswer;
			else
				OnAnswer = e => { return null; };
		}

		public DiscordEmoji Emoji { get; }
		public string[] StringTokens { get; }
		public Func<AnswerArgs, Task<bool>> OnAnswer { get; }

		public async Task<bool> InvokeFromEmoji(DiscordUser user) {
			return await OnAnswer(new AnswerArgs(user));
		}

		public async Task<bool> InvokeFromMessage(DiscordMessageState message) {
			return await OnAnswer(new AnswerArgs(message.Author, true, message));
		}

		public struct AnswerArgs {
			public AnswerArgs(DiscordUser user, bool isMessage, DiscordMessageState message) {
				User = user;
				IsMessage = isMessage;
				Message = message;
			}

			public AnswerArgs(DiscordUser user) {
				User = user;
				IsMessage = false;
				Message = default;
			}

			public DiscordUser User { get; }
			public bool IsMessage { get; }
			public DiscordMessageState Message { get; }
		}

		public struct DiscordMessageState {
			public DiscordMessageState(DiscordChannel channel, string content, DiscordUser author) {
				Channel = channel;
				Content = content;
				Author = author;
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


			public static implicit operator DiscordMessageState(DiscordMessage m) => new DiscordMessageState(m.Channel, m.Content, m.Author);
		}
	}
}