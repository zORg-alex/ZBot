using Newtonsoft.Json;

namespace ZBot {
	public class BotConfig {
		[JsonProperty("token")]
		public string Token { get; set; }
		[JsonProperty("prefix")]
		public string Prefix { get; set; }
	}
}