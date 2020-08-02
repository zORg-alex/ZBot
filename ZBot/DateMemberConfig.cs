using DSharpPlus.Entities;
using Newtonsoft.Json;

namespace ZBot {
	public class DateMemberConfig {
		[JsonIgnore]
		public DiscordMember Member { get; set; }
		public GenderEnum? Gender { get; set; }
		public ulong MemberId { get; set; }
		public ulong[] LikedIds { get; set; } 
	}
}
