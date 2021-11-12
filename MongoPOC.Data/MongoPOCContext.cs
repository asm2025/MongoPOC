using System.Linq;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Identity;
using MongoDB.Driver;
using MongoDbGenericRepository;
using MongoPOC.Data.Settings;
using MongoPOC.Model;

namespace MongoPOC.Data
{
	public class MongoPOCContext : MongoDbContext, IMongoPOCContext
	{
		private IMongoCollection<Book> _books;

		/// <inheritdoc />
		public MongoPOCContext([NotNull] IDbConfig config, [NotNull] UserManager<User> userManager, [NotNull] RoleManager<Role> roleManager)
			: this(config.ConnectionString, config.Database, userManager, roleManager)
		{
		}

		/// <inheritdoc />
		public MongoPOCContext([NotNull] string connectionString, [NotNull] string databaseName, [NotNull] UserManager<User> userManager, [NotNull] RoleManager<Role> roleManager)
			: base(connectionString, databaseName)
		{
			UserManager = userManager;
			RoleManager = roleManager;
		}

		/// <inheritdoc />
		public UserManager<User> UserManager { get; }

		/// <inheritdoc />
		public RoleManager<Role> RoleManager { get; }

		/// <inheritdoc />
		public IQueryable<User> Users => UserManager.Users;

		/// <inheritdoc />
		public IQueryable<Role> Roles => RoleManager.Roles;

		/// <inheritdoc />
		public IMongoCollection<Book> Books => _books ??= Database.GetCollection<Book>("Books");
	}
}