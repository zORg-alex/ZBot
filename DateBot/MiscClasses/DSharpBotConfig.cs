using System.Text.Json.Serialization;
using DSharpPlus;
using Newtonsoft.Json;

namespace DateBot.Base {
	public class DSharpBotConfig {
		[JsonProperty("token")]
		public string Token { get; set; }

		[JsonProperty("prefix")]
		public string Prefix { get; set; }
	}
}