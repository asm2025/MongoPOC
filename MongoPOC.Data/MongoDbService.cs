using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using JetBrains.Annotations;
using MongoDB.Driver;
using MongoPOC.Model;

namespace MongoPOC.Data
{
	public abstract class MongoDbService<T, TKey>
		where T : IEntity<TKey>
		where TKey : IComparable<TKey>, IEquatable<TKey>
	{
		protected MongoDbService([NotNull] IMongoPOCContext context, [NotNull] Func<IMongoPOCContext, IMongoCollection<T>> getCollection)
		{
			Context = context;
			Collection = getCollection(context);
		}

		[NotNull]
		public IQueryable<T> List() { return Collection.AsQueryable(); }

		[NotNull]
		public T Get([NotNull] TKey id) { return Collection.Find(e => id.Equals(e.Value)).FirstOrDefault(); }
		
		[NotNull]
		public async Task<T> GetAsync([NotNull] TKey id)
		{
			IAsyncCursor<T> cursor = await Collection.FindAsync(e => id.Equals(e.Value));
			return cursor == null
						? default(T)
						: await cursor.FirstOrDefaultAsync();
		}

		[NotNull]
		public T Add([NotNull] T item)
		{
			Collection.InsertOne(item);
			return item;
		}

		public void Add([NotNull] IEnumerable<T> items)
		{
			Collection.InsertMany(items);
		}

		[NotNull]
		[ItemNotNull]
		public async Task<T> AddAsync([NotNull] T item)
		{
			await Collection.InsertOneAsync(item);
			return item;
		}

		[NotNull]
		public Task AddAsync([NotNull] IEnumerable<T> items)
		{
			return Collection.InsertManyAsync(items);
		}

		public void Update([NotNull] TKey id, [NotNull] T item)
		{
			Collection.ReplaceOne(e => id.Equals(e.Value), item);
		}

		[NotNull]
		public Task UpdateAsync([NotNull] TKey id, [NotNull] T item)
		{
			return Collection.ReplaceOneAsync(e => id.Equals(e.Value), item);
		}

		public void Delete([NotNull] TKey id)
		{
			Collection.DeleteOne(e => id.Equals(e.Value));
		}

		public void Delete([NotNull] Expression<Func<T, bool>> filter)
		{
			Collection.DeleteMany(filter);
		}

		[NotNull]
		public Task DeleteAsync([NotNull] TKey id)
		{
			return Collection.DeleteOneAsync(e => id.Equals(e.Value));
		}

		[NotNull]
		public Task DeleteAsync([NotNull] Expression<Func<T, bool>> filter)
		{
			return Collection.DeleteManyAsync(filter);
		}

		[NotNull]
		public IMongoPOCContext Context { get; }

		[NotNull]
		protected IMongoCollection<T> Collection { get; }
	}
}
