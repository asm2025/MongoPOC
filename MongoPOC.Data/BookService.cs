using System;
using JetBrains.Annotations;
using MongoPOC.Model;

namespace MongoPOC.Data
{
	public class BookService : MongoDbService<Book, string>
	{
		/// <inheritdoc />
		public BookService([NotNull] IMongoPOCContext context)
			: base(context.Books)
		{
		}
	}
}