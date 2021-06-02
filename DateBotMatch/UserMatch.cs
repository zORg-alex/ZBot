namespace DateBot.Base.Match {
	public struct UserMatch {
		public MatchUser A;
		public MatchUser B;
		public float MatchValue;

		public UserMatch(MatchUser a, MatchUser b, float match) : this() {
			A = a;
			B = b;
			MatchValue = match;
		}
		public bool Contains(MatchUser a, MatchUser b) => A.Id == a.Id || A.Id == b.Id || B.Id == a.Id || B.Id == b.Id;
	}
}
