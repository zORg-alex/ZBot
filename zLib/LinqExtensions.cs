using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace zLib {
	public static class LinqExtensions {
		[DebuggerStepThrough]
		public static IEnumerable<(T1 First, T2 Second)> Merge<T1, T2>(this IEnumerable<T1> first, IEnumerable<T2> second, Func<T1, T2, (T1 First, T2 Second)> operation) {
			using (var iter1 = first.GetEnumerator())
			using (var iter2 = second.GetEnumerator()) {
				while (iter1.MoveNext()) {
					if (iter2.MoveNext()) {
						yield return operation(iter1.Current, iter2.Current);
					} else {
						yield return operation(iter1.Current, default);
					}
				}
				while (iter2.MoveNext()) {
					yield return operation(default, iter2.Current);
				}
			}
		}
		[DebuggerStepThrough]
		public static IEnumerable<(T1 First, T2 Second)> Merge<T1, T2>(this IEnumerable<T1> first, IEnumerable<T2> second) {
			using (var iter1 = first.GetEnumerator())
			using (var iter2 = second.GetEnumerator()) {
				while (iter1.MoveNext()) {
					if (iter2.MoveNext()) {
						yield return (First: iter1.Current, Second: iter2.Current);
					} else {
						yield return (First: iter1.Current, Second: default);
					}
				}
				while (iter2.MoveNext()) {
					yield return (First: default, Second: iter2.Current);
				}
			}
		}
	}
}
