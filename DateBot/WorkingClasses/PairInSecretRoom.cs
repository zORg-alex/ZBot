using System;
using System.Collections.Generic;
using DSharpPlus.Entities;

namespace DateBot.Base {
	public class PairInSecretRoom {
		public DiscordChannel SecretRoom { get; set; }
		public List<DiscordMember> Users { get; set; }
		public DateTime Timeout { get; internal set; }
	}
}
