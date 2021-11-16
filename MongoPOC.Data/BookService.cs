using System;
using System.Linq;
using JetBrains.Annotations;
using MongoDB.Driver;
using MongoPOC.Model;

namespace MongoPOC.Data
{
	public class BookService : MongoDbService<Book, Guid>
	{
		private IMongoCollection<UserBook> _userBooks;

		/// <inheritdoc />
		public BookService([NotNull] IMongoPOCContext context)
			: base(context, e => e.Books)
		{
		}

		[NotNull]
		public IQueryable<Book> ListForUser(Guid userId)
		{
			IQueryable<Book> books = List();
			return UserBooks.AsQueryable()
							.Where(e => e.UserId == userId)
							.SelectMany(e => books.Where(book => book.Value == e.BookId));
		}

		[NotNull]
		protected IMongoCollection<UserBook> UserBooks => _userBooks ??= Context.UserBooks;
	}
}