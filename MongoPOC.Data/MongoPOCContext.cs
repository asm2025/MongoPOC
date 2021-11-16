using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using essentialMix.Extensions;
using essentialMix.Helpers;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Driver;
using MongoDbGenericRepository;
using MongoPOC.Data.Fakers;
using MongoPOC.Data.Settings;
using MongoPOC.Model;

namespace MongoPOC.Data
{
	public class MongoPOCContext : MongoDbContext, IMongoPOCContext
	{
		private const int TOKEN_MIN = 1;
		private const int TOKEN_MAX = 1440;

		private readonly IConfiguration _configuration;

		private IMongoCollection<RefreshToken> _refreshTokens;
		private IMongoCollection<Book> _books;
		private IMongoCollection<UserBook> _userBooks;

		/// <inheritdoc />
		public MongoPOCContext([NotNull] IDbConfig config, [NotNull] IConfiguration configuration, [NotNull] UserManager<User> userManager, [NotNull] RoleManager<Role> roleManager, [NotNull] SignInManager<User> signInManager)
			: this(config.ConnectionString, config.Database, configuration, userManager, roleManager, signInManager)
		{
		}

		/// <inheritdoc />
		public MongoPOCContext([NotNull] string connectionString, [NotNull] string databaseName, [NotNull] IConfiguration configuration, [NotNull] UserManager<User> userManager, [NotNull] RoleManager<Role> roleManager, [NotNull] SignInManager<User> signInManager)
			: base(connectionString, databaseName)
		{
			_configuration = configuration;
			UserManager = userManager;
			RoleManager = roleManager;
			SignInManager = signInManager;
		}

		/// <inheritdoc />
		public UserManager<User> UserManager { get; }

		/// <inheritdoc />
		public RoleManager<Role> RoleManager { get; }

		/// <inheritdoc />
		public SignInManager<User> SignInManager { get; }

		/// <inheritdoc />
		public IQueryable<User> Users => UserManager.Users;

		/// <inheritdoc />
		public IQueryable<Role> Roles => RoleManager.Roles;

		/// <inheritdoc />
		public IMongoCollection<RefreshToken> RefreshTokens => _refreshTokens ??= Database.GetCollection<RefreshToken>("RefreshTokens");

		/// <inheritdoc />
		public IMongoCollection<Book> Books => _books ??= Database.GetCollection<Book>("Books");

		/// <inheritdoc />
		public IMongoCollection<UserBook> UserBooks => _userBooks ??= Database.GetCollection<UserBook>("UserBooks");

		/// <inheritdoc />
		public async Task SeedAsync(ILogger logger)
		{
			const string PWD_SUFFIX = "123!";

			logger.LogInformation("Checking database...");

			HashSet<string> roles = RoleManager.Roles.Select(e => e.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
			if (!roles.Contains(Role.Administrators)) await RoleManager.CreateAsync(new Role(Role.Administrators));
			if (!roles.Contains(Role.Members)) await RoleManager.CreateAsync(new Role(Role.Members));

			if (!UserManager.Users.Any())
			{
				logger.LogInformation("Seeding users...");

				UserFaker userFaker = new UserFaker();
				List<User> newUsers = userFaker.Generate(3);
				
				User user = newUsers[0];
				user.UserName = "Administrator";
				user.Email = "administrator@example.com";

				user = newUsers[1];
				user.UserName = "Bob";
				user.Email = "bob@example.com";

				user = newUsers[2];
				user.UserName = "Alice";
				user.Email = "alice@example.com";

				for (int i = 0; i < newUsers.Count; i++)
				{
					user = newUsers[i];
					user.UpdatedOn = DateTime.UtcNow;
					user.EmailConfirmed = true;

					string password = $"{user.UserName[0].ToUpperInvariant()}{user.UserName.Substring(1, user.UserName.Length - 1).ToLowerInvariant()}{user.UserName[^1] .ToUpperInvariant()}{PWD_SUFFIX}";
					IdentityResult result = await UserManager.CreateAsync(user, password);
					if (!result.Succeeded) continue;
					await UserManager.AddToRoleAsync(user, Role.Members);
					if (i == 0) await UserManager.AddToRoleAsync(user, Role.Administrators);
				}
			}

			if (!Books.AsQueryable().Any())
			{
				logger.LogInformation("Seeding books...");

				BookFaker bookFaker = new BookFaker();
				List<Book> newBooks = bookFaker.Generate(RNGRandomHelper.Next(1, 5));
				await Books.InsertManyAsync(newBooks);
			}
		}

		/// <inheritdoc />
		public async Task<TokenSignInResult> SignInAsync(string userName, string password, bool lockoutOnFailure)
		{
			User user = await UserManager.FindByNameAsync(userName);
			return user == null
						? TokenSignInResult.NotAllowed
						: await SignInAsync(user, password, lockoutOnFailure);
		}

		/// <inheritdoc />
		public async Task<TokenSignInResult> SignInAsync(User user, string password, bool lockoutOnFailure)
		{
			if (string.IsNullOrEmpty(password))
			{
				await SignInManager.SignInAsync(user, true);
			}
			else
			{
				SignInResult result = await SignInManager.CheckPasswordSignInAsync(user, password, lockoutOnFailure);
				if (result.RequiresTwoFactor) return TokenSignInResult.TwoFactorRequired;
				if (result.IsLockedOut) return TokenSignInResult.LockedOut;
				if (!result.Succeeded) return TokenSignInResult.NotAllowed;
			}

			RefreshToken refreshToken = await GenerateRefreshToken(user);
			user.LastActive = DateTime.UtcNow;
			await UserManager.UpdateAsync(user);
			string accessToken = await GenerateToken(user);
			return TokenSignInResult.SuccessFrom(user, accessToken, refreshToken.Id);
		}

		/// <inheritdoc />
		public async Task<TokenSignInResult> RefreshTokenAsync(string refreshToken)
		{
			if (string.IsNullOrEmpty(refreshToken)) return TokenSignInResult.NotAllowed;

			IAsyncCursor<RefreshToken> cursor = await RefreshTokens.FindAsync(e => e.Id == refreshToken);
			if (cursor == null) return TokenSignInResult.NotAllowed;

			RefreshToken token = await cursor.FirstOrDefaultAsync();
			if (token == null) return TokenSignInResult.NotAllowed;

			User user = await UserManager.FindByIdAsync(token.UserId.ToString("D"));
			if (user == null) return TokenSignInResult.NotAllowed;
			return await RefreshTokenAsync(user, token);
		}

		/// <inheritdoc />
		public async Task<TokenSignInResult> RefreshTokenAsync(RefreshToken refreshToken)
		{
			if (refreshToken.UserId.IsEmpty() || !refreshToken.IsExpired) return TokenSignInResult.NotAllowed;

			User user = await UserManager.FindByIdAsync(refreshToken.UserId.ToString("D"));
			if (user == null) return TokenSignInResult.NotAllowed;
			return await RefreshTokenAsync(user, refreshToken);
		}

		/// <inheritdoc />
		public async Task<TokenSignInResult> RefreshTokenAsync(User user, RefreshToken refreshToken)
		{
			RefreshToken newRefreshToken = await GenerateRefreshToken(user, true);
			user.LastActive = DateTime.UtcNow;
			await UserManager.UpdateAsync(user);

			string accessToken = await GenerateToken(user);
			return TokenSignInResult.SuccessFrom(user, accessToken, newRefreshToken.Id);
		}

		/// <inheritdoc />
		public async Task LogoutAsync(string userId)
		{
			if (string.IsNullOrEmpty(userId)) throw new ArgumentNullException(nameof(userId));
			
			User user = await UserManager.FindByIdAsync(userId);
			if (user == null) return;
			await LogoutAsync(user);
		}

		/// <inheritdoc />
		public Task LogoutAsync(User user)
		{
			return RefreshTokens.DeleteManyAsync(e => e.UserId == user.Id);
		}

		/// <inheritdoc />
		public async Task LogoutByTokenAsync(string refreshToken)
		{
			if (string.IsNullOrEmpty(refreshToken)) return;
	
			IAsyncCursor<RefreshToken> cursor = await RefreshTokens.FindAsync(e => e.Id == refreshToken);
			if (cursor == null) return;

			RefreshToken token = await cursor.FirstOrDefaultAsync();
			if (token == null) return;
	
			User user = await UserManager.FindByIdAsync(token.UserId.ToString("D"));
			if (user == null) return;
			await LogoutAsync(user);
		}

		/// <inheritdoc />
		public async Task LogoutByTokenAsync(RefreshToken refreshToken)
		{
			User user = await UserManager.FindByIdAsync(refreshToken.UserId.ToString("D"));
			if (user == null) return;
			await LogoutAsync(user);
		}

		/// <inheritdoc />
		public bool IsSignedIn(ClaimsPrincipal principal)
		{
			return SignInManager.IsSignedIn(principal);
		}

		/// <inheritdoc />
		public int GetTokenExpirationTime()
		{
			const int TOKEN_DEF = 20;

			return _configuration.GetValue<int>("jwt:timeout")
								.Within(0, TOKEN_MAX)
								.IfLessThan(TOKEN_MIN, TOKEN_DEF);
		}

		/// <inheritdoc />
		public int GetRefreshTokenExpirationTime()
		{
			const int TOKEN_DEF = 720;

			return _configuration.GetValue<int>("jwt:refreshInterval")
								.Within(0, TOKEN_MAX)
								.IfLessThan(TOKEN_MIN, TOKEN_DEF);
		}

		private async Task<string> GenerateToken([NotNull] User user)
		{
			const string AUDIENCE_DEF = "api://default";

			ClaimsPrincipal principal = await SignInManager.CreateUserPrincipalAsync(user);
			IList<string> roles = await UserManager.GetRolesAsync(user);
			ClaimsIdentity subject = new ClaimsIdentity(principal.Identity, null, JwtBearerDefaults.AuthenticationScheme, ClaimTypes.Name, ClaimTypes.Role);
			subject.AddClaim(new Claim(ClaimTypes.GivenName, user.Name ?? $"{user.FirstName} {user.LastName}".Trim()));
			subject.AddClaim(new Claim(ClaimTypes.Surname, user.LastName ?? string.Empty));
			subject.AddClaim(new Claim(ClaimTypes.Email, user.Email));
			subject.AddClaim(new Claim(ClaimTypes.Gender, user.Gender.ToString()));
			subject.AddClaim(new Claim(ClaimTypes.DateOfBirth, user.BirthDate.ToString(Constants.DATE_FORMAT)));
			subject.AddClaim(new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N")));

			foreach (string role in roles)
				subject.AddClaim(new Claim(ClaimTypes.Role, role));

			HttpRequest request = SignInManager.Context?.Request;
			string host = request?.Host.Host;
			string audience = _configuration.GetValue("jwt:audience", AUDIENCE_DEF);
			string signingKey = _configuration.GetValue<string>("jwt:signingKey");
			string encryptionKey = _configuration.GetValue<string>("jwt:encryptionKey");

			SecurityTokenDescriptor tokenDescriptor = new SecurityTokenDescriptor
			{
				Issuer = host,
				Audience = audience,
				Subject = subject,
				IssuedAt = DateTime.UtcNow,
				Expires = DateTime.UtcNow.AddMinutes(GetTokenExpirationTime()),
			};

			if (!string.IsNullOrEmpty(signingKey))
				tokenDescriptor.SigningCredentials = new SigningCredentials(SecurityKeyHelper.CreateSymmetricKey(signingKey, 256), SecurityAlgorithms.HmacSha256Signature);

			// It is not recommended to use this if JS libraries will handle tokens unless they can and will do decryption.
			if (!string.IsNullOrEmpty(encryptionKey))
				tokenDescriptor.EncryptingCredentials = new EncryptingCredentials(SecurityKeyHelper.CreateSymmetricKey(encryptionKey, 256), SecurityAlgorithms.Aes256KW, SecurityAlgorithms.Aes128CbcHmacSha256);

			JwtSecurityToken securityToken = SecurityTokenHelper.CreateToken(tokenDescriptor);
			return SecurityTokenHelper.Value(securityToken);
		}

		[NotNull]
		[ItemNotNull]
		private async Task<RefreshToken> GenerateRefreshToken([NotNull] User user, bool forceNew = false)
		{
			List<RefreshToken> openTokens = RefreshTokens.AsQueryable()
										.Where(e => e.UserId == user.Id)
										.OrderByDescending(e => e.Expires)
										.ToList();
			RefreshToken refreshToken = null;

			if (!forceNew && openTokens.Count > 0)
			{
				refreshToken = openTokens[0];
				if ((refreshToken.Expires - DateTime.UtcNow).TotalSeconds < 5.0d) refreshToken = null;
			}

			if (refreshToken != null)
			{
				openTokens.Remove(refreshToken);
				if (openTokens.Count > 0) await RefreshTokens.DeleteManyAsync(e => openTokens.Contains(e));
			}
			else
			{
				byte[] buffer = new byte[64];
				RNGRandomHelper.Default.GetNonZeroBytes(buffer);
				refreshToken = new RefreshToken
				{
					Id = Convert.ToBase64String(buffer),
					UserId = user.Id,
					Expires = DateTime.UtcNow.AddMinutes(GetRefreshTokenExpirationTime()),
					Created = DateTime.UtcNow
				};

				if (openTokens.Count > 0) await RefreshTokens.DeleteManyAsync(e => openTokens.Contains(e));
				await RefreshTokens.InsertOneAsync(refreshToken);
			}

			return refreshToken;
		}
	}
}