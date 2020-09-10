using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace DateBot.Base {
	[DataContract]
	public class GuildConfig {
		[DataMember]
		public ulong GuildId { get; set; }
		[DataMember]
		public ulong DateCategoryId { get; set; }
		[DataMember]
		public ulong DateLobbyId { get; set; }
		[DataMember]
		public ulong WelcomeMessageId { get; set; }
		[DataMember]
		public ulong LogChannelId { get; set; }
		[DataMember]
		public string MaleEmojiId { get; set; }
		[DataMember]
		public string FemaleEmojiId { get; set; }
		[DataMember]
		public string WelcomeMessageBody { get; set; }
		[DataMember]
		public string PrivateMessageBody { get; set; }
		[DataMember]
		public int SecretRoomTime { get; set; } = TimeSpan.FromMinutes(1).Milliseconds;
		/// <summary>
		/// Serializable user states
		/// </summary>
		[DataMember]
		public Dictionary<ulong, UserState> AllUserStates { get; set; } = new Dictionary<ulong, UserState>();
	}
}
