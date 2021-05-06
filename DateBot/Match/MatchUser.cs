using System;
using System.Linq;

namespace DateBot.Base.Match {
	public struct MatchUser {
		/// <summary>
		/// User Id
		/// </summary>
		public ulong Id;
		/// <summary>
		/// User Ids to up match chances
		/// </summary>
		public ulong[] Likes;
		/// <summary>
		/// User Ids to lower match chances
		/// </summary>
		public ulong[] Dislikes;
		/// <summary>
		/// Match preference flags
		/// </summary>
		public int AgeFlag;
		/// <summary>
		/// Last match in the end
		/// </summary>
		public ulong[] LastMatches;
		public MatchUser(ulong id, ulong[] likes, ulong[] dislikes, int ageFlag, ulong[] lastMatches) {
			Id = id;
			Likes = likes;
			Dislikes = dislikes;
			AgeFlag = ageFlag;
			LastMatches = lastMatches;
		}

		internal float MatchedRecently(MatchUser b) => Array.IndexOf(LastMatches, b) * -1f;

		internal bool Liked(MatchUser a) => Likes.Contains(a.Id);
		internal bool Disliked(MatchUser a) => Dislikes.Contains(a.Id);
	}
}
