using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using JetBrains.Annotations;
using MongoDB.Driver;

namespace MongoPOC.Model.Extensions
{
	public static class IAsyncCursorExtension
	{
		public static async IAsyncEnumerable<T> AsAsyncEnumerable<T>([NotNull] this IAsyncCursor<T> thisValue, [EnumeratorCancellation] CancellationToken token = default(CancellationToken))
		{
			while (await thisValue.MoveNextAsync(token))
			{
				foreach (T item in thisValue.Current)
				{
					yield return item;
				}
			}
		}

		public static async IAsyncEnumerable<TProjection> AsAsyncEnumerable<TSource, TProjection>([NotNull] this IAsyncCursor<TSource> thisValue, [NotNull] Func<TSource, TProjection> converter, [EnumeratorCancellation] CancellationToken token = default(CancellationToken))
		{
			while (await thisValue.MoveNextAsync(token))
			{
				foreach (TSource item in thisValue.Current)
				{
					yield return converter(item);
				}
			}
		}
	}
}
