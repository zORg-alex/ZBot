using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace DateBot.Base {
	/// <summary>
	/// Serializable State Object
	/// </summary>
	public class BotStateConfig {
		public List<GuildTask> Guilds { get; set; } = new List<GuildTask>();
	}
}
