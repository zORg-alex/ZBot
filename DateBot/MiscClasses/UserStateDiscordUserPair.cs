using DSharpPlus.Entities;

namespace DateBot.Base {
	public struct UserStateDiscordUserPair {
		public DiscordUser User { get; set; }
		public UserState State { get; set; }
	}
}