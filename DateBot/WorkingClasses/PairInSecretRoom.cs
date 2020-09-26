using System;
using System.Collections.Generic;
using System.Linq;
using DSharpPlus.Entities;

namespace DateBot.Base {
	public class PairInSecretRoom {
		public DiscordChannel SecretRoom { get; set; }
		public List<DiscordMember> Users { get; set; }
		public DateTime Timeout { get; internal set; }

		public override string ToString() {
			return $"{SecretRoom.Name} ({string.Join(", ", Users.Select(u => u.DisplayName))} timeout:{Timeout.ToLongTimeString()})";
		}
	}
}
