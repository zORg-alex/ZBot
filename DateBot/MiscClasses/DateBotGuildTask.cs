using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;

namespace DateBot.Base {
	//Mess goes here, don't want to see it
	//It's go'na be long
	public partial class DateBotGuildTask {

		public DiscordGuild _guild;

		public DiscordGuild Guild {
			get { return _guild; }
			set { _guild = value; State.GuildId = value.Id; }
		}

		private DiscordChannel _dateCategory;

		public DiscordChannel DateLobbyCategory {
			get { return _dateCategory; }
			set { _dateCategory = value; State.DateCategoryId = value.Id; }
		}

		private DiscordChannel _dateSecretCategory;

		public DiscordChannel DateSecretCategory {
			get { return _dateSecretCategory; }
			set { _dateSecretCategory = value; State.DateSecretCategoryId = value.Id; }
		}

		private DiscordChannel _dateTextChannel;

		public DiscordChannel DateTextChannel {
			get { return _dateTextChannel; }
			set { _dateTextChannel = value; State.DateTextChannelId = value.Id; }
		}

		private DiscordMessage _welcomeMessage;

		public DiscordMessage WelcomeMessage {
			get { return _welcomeMessage; }
			set { _welcomeMessage = value; State.WelcomeMessageId = value.Id; }
		}

		private DiscordMessage _privateControlsMessage;

		public DiscordMessage PrivateControlsMessage {
			get { return _privateControlsMessage; }
			set { _privateControlsMessage = value; State.PrivateControlsMessageId = value.Id; }
		}

		private List<DiscordChannel> _dateVoiceLobbies = new List<DiscordChannel>();

		public List<DiscordChannel> DateVoiceLobbies {
			get { return _dateVoiceLobbies; }
			set { _dateVoiceLobbies = value; }
		}

		private List<DiscordUser> _usersInLobbies = new List<DiscordUser>();

		public List<DiscordUser> UsersInLobbies {
			get { return _usersInLobbies; }
			set { _usersInLobbies = value; }
		}

		private List<DiscordChannel> _secretRooms = new List<DiscordChannel>();

		public List<DiscordChannel> SecretRooms {
			get { return _secretRooms; }
			set { _secretRooms = value; }
		}

		private DiscordEmoji _maleEmoji;
		public DiscordEmoji MaleEmoji {
			get { return _maleEmoji; }
			set { _maleEmoji = value; }
		}

		private DiscordEmoji _femaleEmoji;
		public DiscordEmoji FemaleEmoji {
			get { return _femaleEmoji; }
			set { _femaleEmoji = value; }
		}

		private DiscordEmoji _optionsEmoji;
		public DiscordEmoji OptionsEmoji {
			get { return _optionsEmoji; }
			set { _optionsEmoji = value; }
		}

		private DiscordEmoji _likeEmoji;
		public DiscordEmoji LikeEmoji {
			get { return _likeEmoji; }
			set { _likeEmoji = value; }
		}

		private DiscordEmoji _dislikeEmoji;
		public DiscordEmoji DislikeEmoji {
			get { return _dislikeEmoji; }
			set { _dislikeEmoji = value; }
		}

		private DiscordEmoji _cancelLikeEmoji;
		public DiscordEmoji CancelLikeEmoji {
			get { return _cancelLikeEmoji; }
			set { _cancelLikeEmoji = value; }
		}

		private DiscordEmoji _timeEmoji;
		public DiscordEmoji TimeEmoji {
			get { return _timeEmoji; }
			set { _timeEmoji = value; }
		}

		private List<DiscordEmoji> _optionEmojis = new List<DiscordEmoji>();
		public List<DiscordEmoji> OptionEmojis {
			get { return _optionEmojis; }
			set { _optionEmojis = value; }
		}

	}
}
