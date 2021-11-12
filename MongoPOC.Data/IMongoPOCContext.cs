using System.Linq;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Identity;
using MongoDB.Driver;
using MongoDbGenericRepository;
using MongoPOC.Model;

namespace MongoPOC.Data
{
	public interface IMongoPOCContext : IMongoDbContext
	{
		[NotNull]
		UserManager<User> UserManager { get; }
		[NotNull]
		RoleManager<Role> RoleManager { get; }
		[NotNull]
		IQueryable<User> Users { get; }
		[NotNull]
		IQueryable<Role> Roles { get; }
		[NotNull]
		IMongoCollection<Book> Books { get; }
	}
}