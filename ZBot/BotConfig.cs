using Newtonsoft.Json;

namespace ZBot {
	public class BotConfig {
		[JsonProperty("token")]
		internal string Token { get; set; }
		[JsonProperty("prefix")]
		internal string Prefix { get; set; }
	}
}