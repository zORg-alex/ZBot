using System.Collections.Generic;
using DSharpPlus.Entities;

namespace ZBot.DialogFramework {
	public static class EmojiProvider {
		private static Dictionary<string, DiscordEmoji> guildEmoji { get; } = new Dictionary<string, DiscordEmoji>();

		public static DiscordEmoji GetEmoji(string emoji) {

			if (guildEmoji.ContainsKey(emoji)) {
				return guildEmoji[emoji];
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
		public static DiscordEmoji ArrowsCounterclockwise { get { return GetEmoji(":arrows_counterclockwise:"); } }
		public static DiscordEmoji ArrowsClockwise { get { return GetEmoji(":arrows_clockwise:"); } }
		public static DiscordEmoji GreenSquare { get { return GetEmoji(":green_square:"); } }
		public static DiscordEmoji FemaleSign { get { return GetEmoji(":female_sign:"); } }
		public static DiscordEmoji MaleSign { get { return GetEmoji(":male_sign:"); } }
		public static DiscordEmoji WhiteHeartInRed { get { return GetEmoji(":heart_decoration:"); } }
		public static DiscordEmoji Heart { get { return GetEmoji(":heart:"); } }
		public static DiscordEmoji HeartBlack { get { return GetEmoji(":black_heart:"); } }
		public static DiscordEmoji HeartBroken { get { return GetEmoji(":broken_heart:"); } }
		public static DiscordEmoji QuestionMark { get { return GetEmoji(":question:"); } }

		public static DiscordEmoji Detective { get { return GetEmoji(":detective:"); } }
		public static DiscordEmoji Handsahke { get { return GetEmoji(":handshake:"); } }
		public static DiscordEmoji ControlKnobs { get { return GetEmoji(":control_knobs:"); } }
		public static DiscordEmoji Timer { get { return GetEmoji(":timer:"); } }
		public static DiscordEmoji Sunflower { get { return GetEmoji(":sunflower:"); } }
		public static DiscordEmoji Rose { get { return GetEmoji(":rose:"); } }

		public static DiscordEmoji One { get { return GetEmoji(":one:"); } }
		public static DiscordEmoji Two { get { return GetEmoji(":two:"); } }
		public static DiscordEmoji Three { get { return GetEmoji(":three:"); } }
		public static DiscordEmoji Four { get { return GetEmoji(":four:"); } }
		public static DiscordEmoji Five { get { return GetEmoji(":five:"); } }
		public static DiscordEmoji Six { get { return GetEmoji(":six:"); } }
		public static DiscordEmoji Seven { get { return GetEmoji(":seven:"); } }
		public static DiscordEmoji Eight { get { return GetEmoji(":eight:"); } }
		public static DiscordEmoji Nine { get { return GetEmoji(":nine:"); } }
		public static DiscordEmoji Zero { get { return GetEmoji(":zero:"); } }
	}
}