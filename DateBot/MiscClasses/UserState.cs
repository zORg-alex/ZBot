using System;
using System.Collections.Generic;

namespace DateBot.Base {
	/// <summary>
	/// Serializable User State. Likes, dislikes go here
	/// </summary>
	public class UserState {
		public ulong UserId { get; set; }
		public List<ulong> LikedUserIds { get; set; } = new List<ulong>();
		public List<ulong> LastMatches { get; set; } = new List<ulong>();
		public GenderEnum Gender { get; set; }
		public DateTime? LastEnteredLobbyTime { get; internal set; }
	}
}
