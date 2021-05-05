using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace DateBot.Base {
	[DataContract]
	public class DateBotGuildConfig {
		[DataMember]
		public ulong GuildId { get; set; }
		[DataMember]
		public ulong DateCategoryId { get; set; }
		[DataMember]
		public ulong DateSecretCategoryId { get; set; }
		[DataMember]
		public ulong DateTextChannelId { get; set; }
		[DataMember]
		public ulong WelcomeMessageId { get; set; }
		[DataMember]
		public ulong PrivateControlsMessageId { get; set; }
		[DataMember]
		public string DMLikeMessage { get; set; }
		[DataMember]
		public string DMLikeMessageTitle { get; set; }
		[DataMember]
		public ulong LogChannelId { get; set; }
		[DataMember]
		public string MaleEmojiId { get; set; }
		[DataMember]
		public string FemaleEmojiId { get; set; }
		[DataMember]
		public List<string> OptionEmojiIds { get; set; } = new List<string>();
		[DataMember]
		public string LikeEmojiId { get; set; }
		[DataMember]
		public string DisLikeEmojiId { get; set; }
		[DataMember]
		public string CancelLikeEmojiId { get; set; }
		[DataMember]
		public string TimeEmojiId { get; set; }
		[DataMember]
		public string WelcomeMessageBody { get; set; }
		[DataMember]
		public string PrivateMessageBody { get; set; }
		[DataMember]
		public int SecretRoomTime { get; set; }
		/// <summary>
		/// Serializable user states
		/// </summary>
		[DataMember]
		public Dictionary<ulong, UserState> AllUserStates { get; set; } = new Dictionary<ulong, UserState>();
	}
}
