using System;
using System.Collections.Generic;
using System.Linq;

namespace DateBot.Base {
	/// <summary>
	/// Serializable User State. Likes, dislikes go here
	/// </summary>
	public class UserState {
		public ulong UserId { get; set; }
		public List<ulong> LikedUserIds { get; set; } = new List<ulong>();
		public List<ulong> DislikedUserIds { get; set; } = new List<ulong>();
		public List<ulong> LastMatches { get; set; } = new List<ulong>();
		public GenderEnum Gender { get; set; }
		public DateTime? LastEnteredLobbyTime { get; internal set; }
		public DateTime? EnteredPrivateRoomTime { get; internal set; }
		public int AgeOptions { get; set; }
		public bool AddTime { get; internal set; }

		public override string ToString() {
			return $"UserState {UserId} gen:{Gender} age:{AgeOptions}";
		}

		internal void AddMatch(ulong id, int max = 5) {
			if (LastMatches.Contains(id))
				LastMatches.Remove(id);
			LastMatches.Add(id);
			if (LastMatches.Count > max) {
				LastMatches.RemoveAt(0);
			}
		}

		internal void AddLike(ulong[] likedIds) {
			foreach (var id in likedIds) {
				AddLike(id);
			}
		}
		internal void AddLike(ulong likedId) {
			LikedUserIds.Remove(likedId);
			DislikedUserIds.Remove(likedId);
			LikedUserIds.Add(likedId);
		}
		internal void AddDislike(ulong likedId) {
			LikedUserIds.Remove(likedId);
			DislikedUserIds.Remove(likedId);
			DislikedUserIds.Add(likedId);
		}
		internal void RemoveAffinity(ulong likedId) {
			LikedUserIds.Remove(likedId);
			DislikedUserIds.Remove(likedId);
			DislikedUserIds.Add(likedId);
		}
	}
}
