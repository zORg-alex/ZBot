namespace DateBot.Base {
	public struct UsersPairMatch {
		public UserStateDiscordUserPair A { get; set; }
		public UserStateDiscordUserPair B { get; set; }
		public float Match { get; set; }
		public override string ToString() {
			return $"Pair A:{A} B:{B} match:{Match}";
		}
	}
}