using System;
using System.Collections.Generic;
using DSharpPlus.Entities;

namespace DateBot.Base {
	/// <summary>
	/// Should have [DataContract] attribute
	/// </summary>
	public interface IDateBotGuildState {
		ulong GuildId { get; set; }
		ulong DateCategoryId { get; set; }
		ulong DateSecretCategoryId { get; set; }
		ulong DateTextChannelId { get; set; }
		ulong WelcomeMessageId { get; set; }
		ulong PrivateControlsMessageId { get; set; }
		string DMLikeMessage { get; set; }
		string DMLikeMessageTitle { get; set; }
		ulong LogChannelId { get; set; }
		string MaleEmojiId { get; set; }
		string FemaleEmojiId { get; set; }
		List<string> OptionEmojiIds { get; set; }
		string LikeEmojiId { get; set; }
		string DisLikeEmojiId { get; set; }
		string CancelLikeEmojiId { get; set; }
		string TimeEmojiId { get; set; }
		string WelcomeMessageBody { get; set; }
		string PrivateMessageBody { get; set; }
		int SecretRoomTime { get; set; }
		Dictionary<ulong, UserState> AllUserStates { get; set; }
		ulong MaleRoleId { get; set; }
		ulong FemaleRoleId { get; set; }
		bool Active { get; set; }
	}
}
