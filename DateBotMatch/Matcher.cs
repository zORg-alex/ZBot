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
		/// //rename boys to major group and girls to lesser group
		/// //TODO add a leaver penalty -.5 for every left date
		public static IEnumerable<MatchUser[]> MatchUsers(IEnumerable<MatchUser> boys, IEnumerable<MatchUser> girls) {
			var rand = new Random();
			var boysRand = boys.OrderBy(u => rand.Next());

			var matches = new List<UserMatch>(boys.Count() * girls.Count());
			foreach (var ua in boysRand) {
				foreach (var ub in girls) {
					matches.Add(new UserMatch(ua, ub, Match(ua, ub)));
				}
			}

			var sortedMatches = matches.OrderByDescending(m => m.MatchValue).ToList();

			while (sortedMatches.Count()>0) {
				var first = sortedMatches.First();
				yield return new MatchUser[] { first.A, first.B};
				sortedMatches.RemoveAll(m=>m.Contains(first.A,first.B));
			}
		}

		static float Match(MatchUser a, MatchUser b) =>
			((a.AgeFlag | b.AgeFlag) > 0 ? 1f : 0f) +
			a.MatchedRecently(b) +
			(a.Liked(b) && b.Liked(a) ? 1f : 0f) +
			(a.Disliked(b) || b.Disliked(a) ? -2f : 0f);
	}
}
