using System;
using System.Collections.Generic;

namespace DateBot.Base {
	/// <summary>
	/// This implementation just stores data, and doesn't react to changes
	/// </summary>
	public class DateBotGuildState : IDateBotGuildState {
		public ulong GuildId { get; set; }
		public ulong DateCategoryId { get; set; }
		public ulong DateSecretCategoryId { get; set; }
		public ulong DateTextChannelId { get; set; }
		public ulong WelcomeMessageId { get; set; }
		public ulong PrivateControlsMessageId { get; set; }
		public string DMLikeMessage { get; set; }
		public string DMLikeMessageTitle { get; set; }
		public ulong LogChannelId { get; set; }
		public string MaleEmojiId { get; set; }
		public string FemaleEmojiId { get; set; }
		public List<string> OptionEmojiIds { get; set; } = new List<string>();
		public string LikeEmojiId { get; set; }
		public string DisLikeEmojiId { get; set; }
		public string CancelLikeEmojiId { get; set; }
		public string TimeEmojiId { get; set; }
		public string WelcomeMessageBody { get; set; }
		public string PrivateMessageBody { get; set; }
		public int SecretRoomTime { get; set; }
		public Dictionary<ulong, UserState> AllUserStates { get; set; } = new Dictionary<ulong, UserState>();
		public ulong MaleRoleId { get; set; }
		public ulong FemaleRoleId { get; set; }
	}
}
