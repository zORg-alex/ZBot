using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace zLib.Data {
	public interface IRepository_<T> where T : IEntity {
		List<T> GetList();
		Task<List<T>> GetListAsync();
		T GetFirstById(string Id);
		Task<T> GetFirstByIdAsync(string Id);
		void Insert(T Entity);
		void InsertAsync(T Entity);
		void InsertMany(IEnumerable<T> Entities);
		void InsertManyAsync(IEnumerable<T> Entities);
		void Remove(T Entity);
		void Remove(string Id);
		void RemoveAsync(T Entity);
		void RemoveAsync(string Id);
		void Replace(Expression<System.Func<T, bool>> Filter, T Entity);
		void Replace(T Entity);
		void ReplaceAsync(Expression<System.Func<T, bool>> Filter, T Entity);
		void ReplaceAsync(T Entity);
		void Update<TAnon>(Expression<System.Func<T, bool>> Filter, TAnon Update);
		void UpdateAsync<TAnon>(Expression<System.Func<T, bool>> Filter, TAnon Update);

		//void Update(Expression<System.Func<T, bool>> Filter, object Update);
		//void UpdateAsync(Expression<System.Func<T, bool>> Filter, Expression<T> Update);
		void UpdateMany(Expression<System.Func<T, bool>> Filter, object Update);
		void UpdateManyAsync(Expression<System.Func<T, bool>> Filter, T Update);
		void Upsert(Expression<System.Func<T, bool>> Filter, T Replacement);
		void Upsert(T Entity);
		void UpsertAsync(Expression<System.Func<T, bool>> Filter, T Replacement);
		void UpsertAsync(T Entity);
		void UpsertMany(IEnumerable<T> Entities);
		void UpsertManyAsync(IEnumerable<T> Entities);
	}
}
