using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace zLib {
	public static class Extensions {
		/// <summary>
		/// Returns
		/// </summary>
		/// <param name="dt"></param>
		/// <returns>DateTime object with accuracy up to seconds</returns>
		[DebuggerStepThroughAttribute]
		public static DateTime TrimMilliseconds(this DateTime dt) {
			return new DateTime(dt.Year, dt.Month, dt.Day, dt.Hour, dt.Minute, dt.Second, 0);
		}
		/// <summary>
		/// Adds a range of items to an observable collection
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="collection"></param>
		/// <param name="items"></param>
		[DebuggerStepThroughAttribute]
		public static void AddRange<T>(this ObservableCollection<T> collection, IEnumerable<T> items) {
			items.ToList().ForEach(collection.Add);
		}

		[DebuggerStepThroughAttribute]
		public static IEnumerable<TSource> DistinctBy<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector) {
			HashSet<TKey> seenKeys = new HashSet<TKey>();
			foreach (TSource element in source) {
				if (seenKeys.Add(keySelector(element))) {
					yield return element;
				}
			}
		}

		/// <summary>
		/// Check if at least one sequence element return true in action
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="sequence"></param>
		/// <param name="action"></param>
		/// <returns></returns>
		[DebuggerStepThroughAttribute]
		public static bool ForEachIfAtLeastOne<T>(this IEnumerable<T> sequence, Func<T, bool> action) {
			bool b = false;
			if (sequence == null) return false;
			foreach (T item in sequence) b |= action(item);
			return b;
		}
		/// <summary>
		/// Check if all sequence elements return true in action
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="sequence"></param>
		/// <param name="action"></param>
		/// <returns></returns>
		[DebuggerStepThroughAttribute]
		public static bool ForEachIfAll<T>(this IEnumerable<T> sequence, Func<T, bool> action) {
			bool b = true;
			if (sequence == null) return false;
			foreach (T item in sequence) {
				b &= action(item);
				if (!b) break;
			}
			return b;
		}

		/// <summary>
		/// Check if all sequence elements return true in action and ignores null's
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="sequence"></param>
		/// <param name="action"></param>
		/// <returns></returns>
		[DebuggerStepThroughAttribute]
		public static bool? ForEachIfAll<T>(this IEnumerable<T> sequence, Func<T, bool?> action) {
			bool? b = true; int nullcount = 0;
			if (sequence == null) return null;
			foreach (T item in sequence) {
				var r = action(item);
				if (r.HasValue) b &= r.Value;
				else nullcount++;
				if (b.HasValue && !b.Value) break;
			}
			if (nullcount == sequence.Count()) return null;
			return b;
		}

		/// <summary>
		/// Executes Action on each node of a Tree depth first
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="sequence"></param>
		/// <param name="action"></param>
		/// <returns></returns>
		[DebuggerStepThroughAttribute]
		public static void ForEachRecursive<T>(this T obj, Func<T, IEnumerable<T>> getChildSequence, Action<T> action) {
			action(obj);
			var childSequence = getChildSequence(obj);
			if (childSequence != null) {
				foreach (T item in childSequence) {
					item.ForEachRecursive(getChildSequence, action);
				}
			}
			return;
		}
		/// <summary>
		/// Executes Action on each node of a Tree depth first
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="sequence"></param>
		/// <param name="action"></param>
		/// <returns></returns>
		[DebuggerStepThroughAttribute]
		public static void ForEachRecursive<T>(this IEnumerable<T> obj, Func<T, IEnumerable<T>> getChildSequence, Action<T> action) {
			foreach (var item in obj) {
				item.ForEachRecursive(getChildSequence, action);
			}
		}
		/// <summary>
		/// Executes Action on each node of a Tree depth first passing parent into action
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="sequence"></param>
		/// <param name="parentChildAction"></param>
		/// <returns></returns>
		[DebuggerStepThroughAttribute]
		public static void ForEachRecursive<T>(this T obj, T parent, Func<T, IEnumerable<T>> getChildSequence, Action<T, T> parentChildAction) {
			parentChildAction(parent, obj);
			var childSequence = getChildSequence(obj);
			if (childSequence != null) {
				foreach (T item in childSequence) {
					item.ForEachRecursive(obj, getChildSequence, parentChildAction);
				}
			}
			return;
		}
		/// <summary>
		/// Executes Action on each node of a Tree depth first passing parent into action
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="sequence"></param>
		/// <param name="parentChildAction"></param>
		/// <returns></returns>
		[DebuggerStepThroughAttribute]
		public static void ForEachRecursive<T>(this IEnumerable<T> obj, Func<T, IEnumerable<T>> getChildSequence, Action<T, T> parentChildAction) {
			foreach (var item in obj) {
				item.ForEachRecursive(default(T), getChildSequence, parentChildAction);
			}
		}

		/// <summary>
		/// Executes Func on each node of a Tree depth first and returns true if any of nodes return true
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="sequence"></param>
		/// <param name="action"></param>
		/// <returns></returns>
		[DebuggerStepThroughAttribute]
		public static bool ForEachRecursiveIfAny<T>(this T obj, Func<T, IEnumerable<T>> getChildSequence, Func<T, bool> action) {
			bool r = action(obj);
			if (r) return r;
			var childSeq = getChildSequence(obj);
			if (childSeq != null)
				foreach (var item in childSeq) {
					r |= item.ForEachRecursiveIfAny(getChildSequence, action);
					if (r) return r;
				}
			return r;

		}
		/// <summary>
		/// Executes Func on each node of a Tree depth first and returns true if any of nodes return true
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="sequence"></param>
		/// <param name="action"></param>
		/// <returns></returns>
		[DebuggerStepThroughAttribute]
		public static bool ForEachRecursiveIfAny<T>(this IEnumerable<T> obj, Func<T, IEnumerable<T>> getChildSequence, Func<T, bool> action) {
			bool r = false;
			foreach (var item in obj) {
				r |= item.ForEachRecursiveIfAny(getChildSequence, action);
				if (r) return r;
			}
			return r;
		}

		/// <summary>
		/// Executes Func on each node of a Tree depth first and returns true if any of nodes return true
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="sequence"></param>
		/// <param name="parentChildAction"></param>
		/// <returns></returns>
		[DebuggerStepThroughAttribute]
		public static bool ForEachRecursiveIfAny<T>(this T obj, T parent, Func<T, IEnumerable<T>> getChildSequence, Func<T, T, bool> parentChildAction) {
			bool r = parentChildAction(parent, obj);
			var childSeq = getChildSequence(obj);
			if (childSeq != null)
				foreach (var item in childSeq) {
					r |= item.ForEachRecursiveIfAny(obj, getChildSequence, parentChildAction);
					if (r) return r;
				}
			return r;
		}
		/// <summary>
		/// Executes Func on each node of a Tree depth first and returns true if any of nodes return true
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="sequence"></param>
		/// <param name="parentChildAction"></param>
		/// <returns></returns>
		[DebuggerStepThroughAttribute]
		public static bool ForEachRecursiveIfAny<T>(this IEnumerable<T> obj, Func<T, IEnumerable<T>> getChildSequence, Func<T, T, bool> parentChildAction) {
			bool r = false;
			foreach (var item in obj) {
				r |= item.ForEachRecursiveIfAny(default(T), getChildSequence, parentChildAction);
				if (r) return r;
			}
			return r;
		}

		/// <summary>
		/// Converts Collection of IConvertibleFrom objects to <typeparamref name="TReturn"/>
		/// </summary>
		/// <typeparam name="TReturn"></typeparam>
		/// <param name="Original"></param>
		/// <returns></returns>
		[DebuggerStepThroughAttribute]
		public static IEnumerable<TReturn> Convert<TReturn>(this IEnumerable<object> Original) where TReturn : IConvertibleFrom, new() {
			TReturn[] list = new TReturn[Original.Count()];
			int i = 0;
			foreach (var item in Original) {
				list[i] = (TReturn)new TReturn().ConvertFrom(item);
				i++;
			}
			return list;
		}

		public interface IConvertibleFrom {
			IConvertibleFrom ConvertFrom(object Original);
		}

		[DebuggerStepThroughAttribute]
		public static object GetPropValue(this object src, string propName) {
			if (propName.Contains('.')) {
				var paths = propName.Split('.');
				var o = src?.GetType().GetProperty(paths[0]);
				for (int i = 1; i < paths.Length; i++) {
					o = o?.GetType().GetProperty(paths[i]);
				}
				return o.GetValue(src, null);
			} else
				return src?.GetType().GetProperty(propName)?.GetValue(src, null);
		}

		[DebuggerStepThroughAttribute]
		public static void SetPropValue(this object src, string propName, object value) {
			if (propName.Contains('.')) {
				var paths = propName.Split('.');
				for (int i = 0; i < paths.Length - 1; i++) {
					src = src?.GetType().GetProperty(paths[i]).GetValue(src, null);
				}
				src?.GetType().GetProperty(paths[paths.Length - 1])?.SetValue(src, value);
				//o.SetValue(src, value);
			} else
				src?.GetType().GetProperty(propName)?.SetValue(src, value);
		}

		[DebuggerStepThroughAttribute]
		public static string ConvertToASCII(this string o) {
			return LatinToASCIIConverter.Convert(o);
		}

		//[DebuggerStepThroughAttribute]
		public static string ToStringEx(this DateTime input, string format) {

			string dateString = input.ToString
						(format, System.Globalization.CultureInfo.InvariantCulture);

			if (dateString.ToLower().Contains("qq")) {
				dateString = dateString.Replace("qq", ((input.Month - 1) / 3 + 1).ToString("D2"));
				dateString = dateString.Replace("QQ", ((input.Month - 1) / 3 + 1).ToString("D2"));
			} else if (dateString.ToLower().Contains("q")) {
				dateString = dateString.Replace("q", ((input.Month - 1) / 3 + 1).ToString());
				dateString = dateString.Replace("Q", ((input.Month - 1) / 3 + 1).ToString());
			}
			return dateString;
		}

		public static string ToStringItems(this IEnumerable<string> collection) {
			var e = collection.GetEnumerator();
			string r = "";
			if (e.MoveNext()) {
				r = e.Current;
				while (e.MoveNext()) {
					r += $", {e.Current}";
				}
			}
			return r;
		}

		public static int Range(this Random r, int low, int high) {
			if (low < high) {
				return low + (int)((r.NextDouble() / int.MaxValue) * (high - low));
			} else if (low == high)
				return low;
			else
				return high + (int)((r.NextDouble() / int.MaxValue) * (low - high));
		}
		public static double Range(this Random r, double low, double high) {
			if (low < high) {
				return low + ((r.NextDouble() / int.MaxValue) * (high - low));
			} else if (low == high)
				return low;
			else
				return high + ((r.NextDouble() / int.MaxValue) * (low - high));
		}

		/// <summary>
		/// https://www.c-sharpcorner.com/article/linq-extended-joins/
		/// </summary>
		/// <typeparam name="TSource"></typeparam>
		/// <typeparam name="TInner"></typeparam>
		/// <typeparam name="TKey"></typeparam>
		/// <typeparam name="TResult"></typeparam>
		/// <param name="source"></param>
		/// <param name="inner"></param>
		/// <param name="pk"></param>
		/// <param name="fk"></param>
		/// <param name="result"></param>
		/// <returns></returns>
		public static IEnumerable<TResult> LeftJoin<TSource, TInner, TKey, TResult>(this IEnumerable<TSource> source,
														 IEnumerable<TInner> inner,
														 Func<TSource, TKey> pk,
														 Func<TInner, TKey> fk,
														 Func<TSource, TInner, TResult> result)
						where TSource : class where TInner : class {
			IEnumerable<TResult> _result = Enumerable.Empty<TResult>();

			_result = from s in source
					  join i in inner
					  on pk(s) equals fk(i) into joinData
					  from left in joinData.DefaultIfEmpty()
					  select result(s, left);

			return _result;
		}

		/// <summary>
		/// https://www.c-sharpcorner.com/article/linq-extended-joins/
		/// </summary>
		/// <typeparam name="TSource"></typeparam>
		/// <typeparam name="TInner"></typeparam>
		/// <typeparam name="TKey"></typeparam>
		/// <typeparam name="TResult"></typeparam>
		/// <param name="source"></param>
		/// <param name="inner"></param>
		/// <param name="pk"></param>
		/// <param name="fk"></param>
		/// <param name="result"></param>
		/// <returns></returns>
		public static IEnumerable<TResult> RightJoin<TSource, TInner, TKey, TResult>(this IEnumerable<TSource> source,
												  IEnumerable<TInner> inner,
												  Func<TSource, TKey> pk,
												  Func<TInner, TKey> fk,
												  Func<TSource, TInner, TResult> result)
				where TSource : class where TInner : class {
			IEnumerable<TResult> _result = Enumerable.Empty<TResult>();

			_result = from i in inner
					  join s in source
					  on fk(i) equals pk(s) into joinData
					  from right in joinData.DefaultIfEmpty()
					  select result(right, i);

			return _result;
		}

		/// <summary>
		/// https://www.c-sharpcorner.com/article/linq-extended-joins/
		/// </summary>
		/// <typeparam name="TSource"></typeparam>
		/// <typeparam name="TInner"></typeparam>
		/// <typeparam name="TKey"></typeparam>
		/// <typeparam name="TResult"></typeparam>
		/// <param name="source"></param>
		/// <param name="inner"></param>
		/// <param name="pk"></param>
		/// <param name="fk"></param>
		/// <param name="result"></param>
		/// <returns></returns>
		public static IEnumerable<TResult> FullOuterJoinJoin<TSource, TInner, TKey, TResult>(this IEnumerable<TSource> source,
															  IEnumerable<TInner> inner,
															  Func<TSource, TKey> pk,
															  Func<TInner, TKey> fk,
															  Func<TSource, TInner, TResult> result)
					where TSource : class where TInner : class {

			var left = source.LeftJoin(inner, pk, fk, result).ToList();
			var right = source.RightJoin(inner, pk, fk, result).ToList();

			return left.Union(right);
		}

		/// <summary>
		/// https://www.c-sharpcorner.com/article/linq-extended-joins/
		/// </summary>
		/// <typeparam name="TSource"></typeparam>
		/// <typeparam name="TInner"></typeparam>
		/// <typeparam name="TKey"></typeparam>
		/// <typeparam name="TResult"></typeparam>
		/// <param name="source"></param>
		/// <param name="inner"></param>
		/// <param name="pk"></param>
		/// <param name="fk"></param>
		/// <param name="result"></param>
		/// <returns></returns>
		public static IEnumerable<TResult> LeftExcludingJoin<TSource, TInner, TKey, TResult>(this IEnumerable<TSource> source,
														  IEnumerable<TInner> inner,
														  Func<TSource, TKey> pk,
														  Func<TInner, TKey> fk,
														  Func<TSource, TInner, TResult> result)
				where TSource : class where TInner : class {
			IEnumerable<TResult> _result = Enumerable.Empty<TResult>();

			_result = from s in source
					  join i in inner
					  on pk(s) equals fk(i) into joinData
					  from left in joinData.DefaultIfEmpty()
					  where left == null
					  select result(s, left);

			return _result;
		}

		/// <summary>
		/// https://www.c-sharpcorner.com/article/linq-extended-joins/
		/// </summary>
		/// <typeparam name="TSource"></typeparam>
		/// <typeparam name="TInner"></typeparam>
		/// <typeparam name="TKey"></typeparam>
		/// <typeparam name="TResult"></typeparam>
		/// <param name="source"></param>
		/// <param name="inner"></param>
		/// <param name="pk"></param>
		/// <param name="fk"></param>
		/// <param name="result"></param>
		/// <returns></returns>
		public static IEnumerable<TResult> RightExcludingJoin<TSource, TInner, TKey, TResult>(this IEnumerable<TSource> source,
																IEnumerable<TInner> inner,
																Func<TSource, TKey> pk,
																Func<TInner, TKey> fk,
																Func<TSource, TInner, TResult> result)
						where TSource : class where TInner : class {
			IEnumerable<TResult> _result = Enumerable.Empty<TResult>();

			_result = from i in inner
					  join s in source
					  on fk(i) equals pk(s) into joinData
					  from right in joinData.DefaultIfEmpty()
					  where right == null
					  select result(right, i);

			return _result;
		}
		
#pragma warning disable RECS0154 // Parameter is never used
		public static List<T> GetEmptyListOfThisType<T>(this T definition, int Count = 0)
#pragma warning restore RECS0154 // Parameter is never used
	{
			if (Count == 0)
				return new List<T>();
			else
				return new List<T>(Count);
		}

		/// <summary>
		/// Copies tree nodes from root to children if supplied with Get and Set Functions
		/// </summary>
		[DebuggerStepThrough]
		public static TNew CopyTreeNode<TNew, TOld>(TOld Original, Func<TOld, TNew> Copy, Func<TOld, IEnumerable<TOld>> GetOldChildren, Action<TNew, IEnumerable<TNew>> SetNewChildren) {
			TNew NewNode = Copy(Original);
			SetNewChildren(NewNode, GetOldChildren(Original).Select(n => CopyTreeNode(n, Copy, GetOldChildren, SetNewChildren)));
			return NewNode;
		}
		/// <summary>
		/// Copies tree nodes from root to children if supplied with Get and Set Functions
		/// </summary>
		//[DebuggerStepThrough]
		public static TNew CopyTreeNodeWithRejection<TNew, TOld>(TOld Original, Func<TOld, TNew> Copy, Func<TOld, IEnumerable<TOld>> GetOldChildren, Action<TNew, IEnumerable<TNew>> SetNewChildren, bool RemoveEmptyNodes = true) where TOld : class where TNew : class {
			TNew NewNode = Copy(Original);
			if (NewNode != null) {
				var ch = GetOldChildren(Original).Where(o => !(o is null));
				if (ch == null) return null;
				var l = new List<TNew>();
				foreach (var c in ch) {
					var n = CopyTreeNodeWithRejection(c, Copy, GetOldChildren, SetNewChildren, RemoveEmptyNodes);
					l.Add(n);
				}
				if (RemoveEmptyNodes && l.Count() != 0 && l.All(o => o is null)) return null;//Reject Node if ALL children were rejected
				SetNewChildren(NewNode, l.Where(o => !(o is null)));//Clean nulls
			}
			return NewNode;
		}
	
		public static string ConcatString<T>(this IEnumerable<T> collection, Func<T, string> getString) {
			return string.Concat(collection.Select(o=>getString(o)));
		}
	}
}
