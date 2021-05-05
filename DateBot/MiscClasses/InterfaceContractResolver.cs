using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace DateBot.Base {
	public class InterfaceContractResolver<TInterface> : DefaultContractResolver where TInterface : class {
		protected override IList<JsonProperty> CreateProperties(Type type, MemberSerialization memberSerialization) {
			IList<JsonProperty> properties = base.CreateProperties(typeof(TInterface), memberSerialization);
			return properties;
		}
	}
}
