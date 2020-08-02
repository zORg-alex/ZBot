using System;
using System.Collections.Generic;
using MongoDB.Bson;
using MongoDB.Driver;
using zLib;
using System.Threading.Tasks;
using System.Linq.Expressions;
using System.Linq;
using MongoDB.Bson.IO;
using System.Data;

namespace zLib.Data {
	public class MongoRepository<T> : IRepository_<T> where T : IEntity {

		public MongoRepository(IMongoCollection<T> Collection) {
			this.Collection = Collection;
		}

		/// <summary>
		/// Needs to be set in ctor
		/// </summary>
		public IMongoCollection<T> Collection { get; protected set; }
		public IQueryable<T> QueriableCollection { get; protected set; }
		public List<T> GetList() => 
			Collection.Find(new BsonDocument()).ToList();

		public async Task<List<T>> GetListAsync() =>
			await Collection.FindAsync(new BsonDocument()).Result.ToListAsync();
		public T GetFirstById(string Id) =>
			Collection.Find(o => o.Id == Id).FirstOrDefault();
		public async Task<T> GetFirstByIdAsync(string Id) =>
			await Collection.Find(o => o.Id == Id).FirstOrDefaultAsync();

		public void Replace(Expression<Func<T, bool>> Filter, T Entity) =>
			Collection.ReplaceOne(Filter, Entity);
		public async void ReplaceAsync(Expression<Func<T, bool>> Filter, T Entity) =>
			await Collection.ReplaceOneAsync(Filter, Entity);

		public void Replace(T Entity) =>
			Collection.ReplaceOne(e=>e.Id == Entity.Id, Entity);
		public async void ReplaceAsync(T Entity) =>
			await Collection.ReplaceOneAsync(e => e.Id == Entity.Id, Entity);

		public void Update<TAnon>(Expression<Func<T, bool>> Filter, TAnon Update) =>
			Collection.UpdateOne(Filter, zLib.Bson.BsonExtensions.UpdateDefinitionFromAnonimous<T, TAnon>(Update));
		public async void UpdateAsync<TAnon>(Expression<Func<T, bool>> Filter, TAnon Update) =>
			await Collection.UpdateOneAsync(Filter, zLib.Bson.BsonExtensions.UpdateDefinitionFromAnonimous<T, TAnon>(Update));

		/// <summary>
		/// Warning this will update all records in Collection
		/// </summary>
		/// <param name="Update"></param>
		public void UpdateMany(Expression<Func<T, bool>> Filter,object Update) =>
				Collection.UpdateMany(Filter, Update.ToBsonDocument());
		/// <summary>
		/// Warning this will update all records in Collection
		/// </summary>
		/// <param name="Update"></param>
		public async void UpdateManyAsync(Expression<Func<T, bool>> Filter, T Update) =>
			await Collection.UpdateManyAsync(Filter, Update.ToBsonDocument());

		public void Upsert(Expression<Func<T, bool>> Filter, T Replacement) =>
			Collection.ReplaceOne(Filter, Replacement, new UpdateOptions { IsUpsert = true });
		public async void UpsertAsync(Expression<Func<T, bool>> Filter, T Replacement) =>
			await Collection.ReplaceOneAsync(Filter, Replacement, new UpdateOptions { IsUpsert = true });

		public void Upsert(T Entity) =>
			Collection.ReplaceOne(e => e.Id == Entity.Id, Entity, new UpdateOptions { IsUpsert = true });
		public async void UpsertAsync(T Entity) =>
			await Collection.ReplaceOneAsync(e => e.Id == Entity.Id, Entity, new UpdateOptions { IsUpsert = true });

		public void UpsertMany(IEnumerable<T> Entities) {
			foreach (var Entity in Entities) {
				Collection.ReplaceOne(e => e.Id == Entity.Id, Entity, new UpdateOptions { IsUpsert = true });
			}
		}
		public async void UpsertManyAsync(IEnumerable<T> Entities) {
			foreach (var Entity in Entities) {
				await Collection.ReplaceOneAsync(e => e.Id == Entity.Id, Entity, new UpdateOptions { IsUpsert = true });
			}
		}

		public void Insert(T Entity) =>
			Collection.InsertOne(Entity);
		public async void InsertAsync(T Entity) =>
			await Collection.InsertOneAsync(Entity);

		public void InsertMany(IEnumerable<T> Entities) =>
			Collection.InsertMany(Entities);
		public async void InsertManyAsync(IEnumerable<T> Entities) =>
			await Collection.InsertManyAsync(Entities);

		public void Remove(T Entity) =>
			Collection.DeleteOne(o => o.Id == Entity.Id);
		public async void RemoveAsync(T Entity) =>
			await Collection.DeleteOneAsync(o => o.Id == Entity.Id);


		public void Remove(string Id) =>
			Collection.DeleteOne(o => o.Id == Id);
		public async void RemoveAsync(string Id) =>
			await Collection.DeleteOneAsync(o => o.Id == Id);

	}
}
