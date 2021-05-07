using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using DSharpPlus;
using DSharpPlus.Entities;

namespace ZBot {
	public static class Emoji {

		public static bool TryGetEmojiFromText(DiscordClient client, string text, out DiscordEmoji emoji) {
			if (DiscordEmoji.TryFromUnicode(client, text, out emoji))
				return true;
			var name = Regex.Match(text, @":(.*):");
			if (DiscordEmoji.TryFromName(client, name.Value, out emoji))
				return true;
			//if (DiscordEmoji.TryFromGuildEmote(client, text, out emoji))
			//	return true;
			return false;
		}

		public static DiscordEmoji GetEmojiFromText(DiscordClient client, string text) {
			if (DiscordEmoji.TryFromUnicode(client, text, out var emoji))
				return emoji;
			var name = Regex.Match(text, @":(.*):");
			if (DiscordEmoji.TryFromName(client, name.Value, out emoji))
				return emoji;
			return null;
		}

		//public static string NormalName(this DiscordEmoji t) => t.RequiresColons ? $":{t.Name}:" : t.Name;
	}
}
