using System;
using System.Collections.Generic;
using System.Linq;

namespace DateBot.Base.Match {
	public static class Matcher {
		/// <summary>
		/// Matches all boys to all girls and returns randomized best pairs
		/// </summary>
		/// <param name="boys"></param>
		/// <param name="girls"></param>
		/// <returns></returns>
		public static IEnumerable<MatchUser[]> MatchUsers(IEnumerable<MatchUser> boys, IEnumerable<MatchUser> girls) {
			var rand = new Random();
			var aRand = boys.OrderBy(u => rand.Next());

			var matches = new List<UserMatch>(boys.Count() * girls.Count());
			foreach (var ua in aRand) {
				foreach (var ub in girls) {
					matches.Add(new UserMatch(ua, ub, Match(ua, ub)));
				}
			}

			while (matches.Count>0) {
				var first = matches.First();
				yield return new MatchUser[] { first.A, first.B};
				matches.RemoveAll(m=>m.Contains(first.A,first.B));
			}
		}

		static float Match(MatchUser a, MatchUser b) =>
			((a.AgeFlag | b.AgeFlag) > 0 ? 1f : 0f) +
			a.MatchedRecently(b) +
			(a.Liked(b) ? 1f : 0f) +
			(a.Disliked(b) ? -1f : 0f);
	}
}
