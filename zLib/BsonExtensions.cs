using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using zLib.Data;

namespace zLib.Bson {
	public static class BsonExtensions {
		/// <summary>
		/// Generates UpdateDefinition from anonimous type
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <typeparam name="TAnon"></typeparam>
		/// <param name="Object"></param>
		/// <returns></returns>
		public static UpdateDefinition<T> UpdateDefinitionFromAnonimous<T,TAnon> (object Object) where T : IEntity {
			UpdateDefinitionBuilder<T> upddb = Builders<T>.Update;
			var l = new List<UpdateDefinition<T>>();
			var props = typeof(T).GetProperties().Select(p=>new KeyValuePair<string, Type> (p.Name, p.PropertyType));
			var anonProps = typeof(TAnon).GetProperties();
			foreach (var prop in anonProps) {
				var pkvp = props.FirstOrDefault(p => p.Key == prop.Name);
				if (pkvp.Key != null && pkvp.Value == prop.PropertyType) {
					l.Add(upddb.Set(prop.Name, prop.GetValue(Object)));
				}
			}
			
			return upddb.Combine(l);
		}
	}
}
