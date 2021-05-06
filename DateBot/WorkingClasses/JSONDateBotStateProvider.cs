using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace DateBot.Base {
	/// <summary>
	/// This State Provider uses JSON to store/load state
	/// </summary>
	public class JSONDateBotStateProvider : IDateBotStateProvider {
		public JSONDateBotStateProvider(string path) {
			SaveLocation(path);
		}
		public static async Task<JSONDateBotStateProvider> CreateAndLoad(string path) {
			var n = new JSONDateBotStateProvider(path);
			await n.LoadAsync();
			return n;
		}
		public JSONDateBotStateProvider() : this("\\state.json") { }
		private string _savePath { get; set; }

		/// <summary>
		/// Set location for json to save to
		/// </summary>
		/// <param name="path"></param>
		public void SaveLocation(string path) {
			try {
				var savedir = Directory.GetParent(path);
				if (!savedir.Exists)
					Directory.CreateDirectory(savedir.FullName);
			} catch (Exception e) {
				Console.WriteLine(e.Message);
				return;
			}
			_savePath = path;
		}
		public async Task SaveAsync() {
			try {
				using (var sr = new StreamWriter(_savePath)) {
					await sr.WriteAsync(JsonConvert.SerializeObject(GuildStates, Formatting.Indented));
				}
			} catch (Exception e) { Console.WriteLine(e); }
		}

		public bool Loaded = false;

		public async Task LoadAsync() {
			if (File.Exists(_savePath)) {
				//Deserealize bot last state
				using (var sr = new StreamReader(_savePath)) {
					GuildStates = JsonConvert.DeserializeObject<List<DateBotGuildState>>(
						await sr.ReadToEndAsync()
					).Select(s=>(IDateBotGuildState)s).ToList();
				}
			}
			//If still null or wasn't read for some reason, instantiate new one
			if (GuildStates == null) {
				Console.WriteLine("Bot State wasn't deserialized properly, instantiatin new. You may might have lost state file.");
				GuildStates = new List<IDateBotGuildState>();
			}
			Loaded = true;
		}
		public List<IDateBotGuildState> GuildStates { get; private set; }

		public void AddGuildState(IDateBotGuildState state) {
			GuildStates.Add(state);
		}
		public void RemoveGuildState(IDateBotGuildState state) {
			GuildStates.Remove(state);
		}
	}
}
