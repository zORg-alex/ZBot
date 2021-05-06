using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace DateBot.Base {
	public interface IDateBotStateProvider{
		List<IDateBotGuildState> GuildStates { get; }

		void AddGuildState(IDateBotGuildState state);
		void RemoveGuildState(IDateBotGuildState state);
		Task LoadAsync();
		Task SaveAsync();
	}
}
