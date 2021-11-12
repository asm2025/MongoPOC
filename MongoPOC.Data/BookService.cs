using System;
using JetBrains.Annotations;
using MongoPOC.Data.Settings;
using MongoPOC.Model;

namespace MongoPOC.Data
{
	public class BookService : MongoDbService<Book, Guid>
	{
		/// <inheritdoc />
		public BookService([NotNull] IDbConfig configuration)
			: base(configuration, "Books")
		{
		}
	}
}