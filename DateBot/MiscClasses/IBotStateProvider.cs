using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace DateBot.Base {
	public interface IBotStateProvider{
		List<IDateBotGuildTaskProvider> GuildTasks { get; }
	}

	/// <summary>
	/// Should have [DataContract] attribute
	/// </summary>
	public interface IDateBotGuildTaskProvider {
		ulong GuildId { get; }
	}

	public class JSONDateBotStateProvider : IBotStateProvider {
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
					var jss = new JsonSerializerSettings() {
						ContractResolver = new InterfaceContractResolver<IDateBotGuildTaskProvider>(),
						Formatting = Formatting.Indented
					};
					await sr.WriteAsync(JsonConvert.SerializeObject(GuildTasks, jss));
				}
			} catch (Exception e) { Console.WriteLine(e); }
		}

		public bool Loaded = false;

		public async Task LoadAsync() {
			if (File.Exists(_savePath)) {
				//Deserealize bot last state
				using (var sr = new StreamReader(_savePath)) {
					GuildTasks = JsonConvert.DeserializeObject<List<JSONDateBotGuildTaskProvider>>(
						await sr.ReadToEndAsync()
					).Select(j=>(IDateBotGuildTaskProvider)j).ToList();
				}
			}
			//If still null or wasn't read for some reason, instantiate new one
			if (GuildTasks == null) {
				Console.WriteLine("Bot State wasn't deserialized properly, instantiatin new. You may might have lost state file.");
				GuildTasks = new List<IDateBotGuildTaskProvider>();
			}
			Loaded = true;
		}
		public List<IDateBotGuildTaskProvider> GuildTasks { get; private set; }
	}

	public class JSONDateBotGuildTaskProvider : IDateBotGuildTaskProvider {
		public ulong GuildId { get; set; }
		public string GuildName = "Name that shouldn't be serialized";

	}
}
