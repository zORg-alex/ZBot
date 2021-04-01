namespace ZBot.MessageFramework {
	public enum MessageBehavior {
		/// <summary>
		/// Keep message forever
		/// </summary>
		Permanent,
		/// <summary>
		/// Destroy message after it waas answered
		/// </summary>
		Volatile,
		/// <summary>
		/// Keep message after answered, but unsubscribe
		/// </summary>
		KeepMessageAfterTimeout
	}
}