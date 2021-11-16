using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
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
		SignInManager<User> SignInManager { get; }
		[NotNull]
		IQueryable<User> Users { get; }
		[NotNull]
		IQueryable<Role> Roles { get; }
		[NotNull]
		IMongoCollection<RefreshToken> RefreshTokens { get; }
		[NotNull]
		IMongoCollection<Book> Books { get; }
		[NotNull]
		IMongoCollection<UserBook> UserBooks { get; }

		[NotNull]
		Task SeedAsync([NotNull] ILogger logger);
		[ItemNotNull]
		Task<TokenSignInResult> SignInAsync([NotNull] string userName, string password, bool lockoutOnFailure);
		[ItemNotNull]
		Task<TokenSignInResult> SignInAsync([NotNull] User user, string password, bool lockoutOnFailure);
		[ItemNotNull]
		Task<TokenSignInResult> RefreshTokenAsync([NotNull] string refreshToken);
		[ItemNotNull]
		Task<TokenSignInResult> RefreshTokenAsync([NotNull] RefreshToken refreshToken);
		[ItemNotNull]
		Task<TokenSignInResult> RefreshTokenAsync([NotNull] User user, [NotNull] RefreshToken refreshToken);
		Task LogoutAsync([NotNull] string userId);
		Task LogoutAsync([NotNull] User user);
		Task LogoutByTokenAsync([NotNull] string refreshToken);
		Task LogoutByTokenAsync([NotNull] RefreshToken refreshToken);
		bool IsSignedIn([NotNull] ClaimsPrincipal principal);
		int GetTokenExpirationTime();
		int GetRefreshTokenExpirationTime();
	}
}