using System.Collections.Generic;
using DSharpPlus.Entities;

namespace ZBot.MessageFramework {
	public static class GildEmoji {
		private static Dictionary<string, DiscordEmoji> guildEmoji { get; } = new Dictionary<string, DiscordEmoji>();

		public static DiscordEmoji GetEmoji(string emoji) {

			if (guildEmoji[emoji] is DiscordEmoji foundEmoji) {
				return foundEmoji;
			} else {
				var newEmoji = DiscordEmoji.FromName(Bot.Instance.Client, emoji);
				//What if not found? Return null or throw exception?
				guildEmoji[emoji] = newEmoji;
				return newEmoji;
			}
		}

		public static DiscordEmoji CheckMarkOnBlue { get { return GetEmoji(":ballot_box_with_check:"); } }
		public static DiscordEmoji CheckMarkOnGreen { get { return GetEmoji(":white_check_mark:"); } }
		public static DiscordEmoji CrossOnGreen { get { return GetEmoji(":negative_squared_cross_mark:"); } }
		public static DiscordEmoji BlueSquare { get { return GetEmoji(":blue_square:"); } }
		public static DiscordEmoji GreenSquare { get { return GetEmoji(":green_square:"); } }
		public static DiscordEmoji FemaleSign { get { return GetEmoji(":female_sign:"); } }
		public static DiscordEmoji MaleSign { get { return GetEmoji(":male_sign:"); } }
		public static DiscordEmoji WhiteHeartInRed { get { return GetEmoji(":heart_decoration:"); } }


	}
}