using System;
using System.Security.Claims;
using essentialMix.Core.Web.Middleware;
using essentialMix.Extensions;
using essentialMix.Helpers;
using essentialMix.Newtonsoft.Helpers;
using essentialMix.Newtonsoft.Serialization;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using MongoPOC.Data;
using MongoPOC.Data.Settings;
using MongoPOC.Model;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Scrutor;
using Swashbuckle.AspNetCore.Filters;
using Serilog;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace MongoPOC.API
{
	public class Startup
	{
		[NotNull]
		private readonly IHostEnvironment _environment;

		[NotNull]
		private readonly IConfiguration _configuration;

		[NotNull]
		private readonly ILogger _logger;

		public Startup([NotNull] IHostEnvironment environment, [NotNull] IConfiguration configuration, [NotNull] ILogger<Startup> logger)
		{
			_environment = environment;
			_configuration = configuration;
			_logger = logger;
		}

		// This method gets called by the runtime. Use this method to add services to the container.
		public void ConfigureServices([NotNull] IServiceCollection services)
		{
			string[] allowedClients = _configuration.GetSection("allowedClients").Get<string[]>();
			MongoDbConfig dbConfig = _configuration.GetSection("data").Get<MongoDbConfig>();

			services
				// config
				.AddSingleton(_configuration)
				.AddSingleton(_environment)
				.AddSingleton<IDbConfig>(dbConfig)
				// logging
				.AddLogging(config =>
				{
					config
						.AddDebug()
						.AddConsole()
						.AddSerilog();
				})
				.AddSingleton(typeof(ILogger<>), typeof(Logger<>))
				// Swagger
				.AddSwaggerGen(options =>
				{
					options.Setup(_configuration, _environment)
							.AddJwtBearerSecurity();
					//options.OperationFilter<FormFileFilter>();
					options.ExampleFilters();
				})
				.AddSwaggerExamplesFromAssemblyOf<IDbConfig>()
				// Cookies
				.Configure<CookiePolicyOptions>(options =>
				{
					// This lambda determines whether user consent for non-essential cookies is needed for a given request.
					options.CheckConsentNeeded = _ => true;
					options.MinimumSameSitePolicy = SameSiteMode.None;
				})
				// FormOptions
				.Configure<FormOptions>(options =>
				{
					options.ValueLengthLimit = int.MaxValue;
					options.MultipartBodyLengthLimit = int.MaxValue;
					options.MemoryBufferThreshold = int.MaxValue;
				})
				// Helpers
				.AddHttpContextAccessor()
				// Mapper
				.AddAutoMapper((_, builder) => builder.AddProfile(new AutoMapperProfiles()), new[] { typeof(AutoMapperProfiles).Assembly }, ServiceLifetime.Singleton)
				// Identity & Database
				.AddIdentityCore<User>(options =>
				{
					options.Password.RequireDigit = true;
					options.Password.RequireUppercase = true;
					options.Password.RequireLowercase = true;
					options.Password.RequireNonAlphanumeric = true;
					options.Password.RequiredLength = 6;

					options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
					options.Lockout.MaxFailedAccessAttempts = 5;
					options.Lockout.AllowedForNewUsers = true;

					options.SignIn.RequireConfirmedAccount = false;
					options.SignIn.RequireConfirmedEmail = false;
					options.SignIn.RequireConfirmedPhoneNumber = false;

					options.Stores.MaxLengthForKeys = 128;

					options.User.RequireUniqueEmail = true;
				})
				.AddRoles<Role>()
				.AddMongoDbStores<User, Role, Guid>(dbConfig.ConnectionString, dbConfig.Database)
				.AddUserManager<UserManager<User>>()
				.AddRoleManager<RoleManager<Role>>()
				.AddRoleValidator<RoleValidator<Role>>()
				.AddSignInManager<SignInManager<User>>()
				.AddDefaultTokenProviders();
			services
				.AddScoped<IMongoPOCContext, MongoPOCContext>()
				// using Scrutor
				.Scan(scan =>
				{
					// Add all repositories
					scan.FromAssemblyOf<IDbConfig>()
						.AddClasses(cls => cls.AssignableTo(typeof(MongoDbService<,>)))
						.UsingRegistrationStrategy(RegistrationStrategy.Skip)
						.AsSelf()
						.WithScopedLifetime();
				});

			// Authentication
			services
				// Jwt Bearer
				.AddJwtBearerAuthentication()
				.AddCookie(options =>
				{
					options.SlidingExpiration = true;
					options.LoginPath = "/users/login";
					options.LogoutPath = "/users/logout";
					options.ExpireTimeSpan = TimeSpan.FromMinutes(_configuration.GetValue("jwt:timeout", 20).NotBelow(5));
				})
				.AddJwtBearerOptions(options =>
				{
					SecurityKey signingKey = SecurityKeyHelper.CreateSymmetricKey(_configuration.GetValue<string>("jwt:signingKey"), 256);
					//SecurityKey decryptionKey = SecurityKeyHelper.CreateSymmetricKey(_configuration.GetValue<string>("jwt:encryptionKey"), 256);
					options.Setup(signingKey, /*decryptionKey, */_configuration, _environment.IsDevelopment());
				});
			
			// Authorization
			services
				.AddAuthorization(options =>
				{
					options.DefaultPolicy = new AuthorizationPolicyBuilder(JwtBearerDefaults.AuthenticationScheme)
											.RequireAuthenticatedUser()
											.RequireClaim(ClaimTypes.Role, Role.Roles)
											.Build();

					options.AddPolicy(Role.Members, policy =>
					{
						policy.AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme)
							.RequireAuthenticatedUser()
							.RequireClaim(ClaimTypes.Role, Role.Members)
							.RequireRole(Role.Members);
					});

					options.AddPolicy(Role.Administrators, policy =>
					{
						policy.AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme)
							.RequireAuthenticatedUser()
							.RequireClaim(ClaimTypes.Role, Role.Administrators)
							.RequireRole(Role.Administrators);
					});
				});

			// MVC
			services
				.AddDefaultCorsPolicy(builder => builder.WithExposedHeaders("Set-Cookie"), allowedClients)
				.AddForwardedHeaders()
				.AddControllers()
				// last setting
				.AddNewtonsoftJson(options =>
				{
					JsonHelper.SetDefaults(options.SerializerSettings, contractResolver: new CamelCasePropertyNamesContractResolver());
					options.SerializerSettings.NullValueHandling = NullValueHandling.Include;
					options.SerializerSettings.DateFormatHandling = DateFormatHandling.IsoDateFormat;

					JsonSerializerSettingsConverters allConverters = EnumHelper<JsonSerializerSettingsConverters>.GetAllFlags() &
																	~(JsonSerializerSettingsConverters.IsoDateTime |
																	JsonSerializerSettingsConverters.JavaScriptDateTime |
																	JsonSerializerSettingsConverters.UnixDateTime);
					options.SerializerSettings.AddConverters(allConverters);
				});
		}

		// This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
		public void Configure([NotNull] IApplicationBuilder app)
		{
			if (_environment.IsDevelopment()) app.UseDefaultExceptionDelegate(_logger);
			else app.UseRedirectWithStatusCode("/error/{0}");

			if (!_environment.IsDevelopment() || _configuration.GetValue<bool>("useSSL")) app.UseHsts();
			app
				.UseHttpsRedirection()
				.UseForwardedHeaders()
				.UseCultureHandler()
				.UseSerilogRequestLogging();

			if (_environment.IsDevelopment())
			{
				app.UseSwagger(options => options.RouteTemplate = _configuration.GetValue<string>("swagger:template"))
					.UseSwaggerUI(options =>
					{
						options.SwaggerEndpoint(_configuration.GetValue<string>("swagger:ui"), _configuration.GetValue("title", _environment.ApplicationName));
						options.AsStartPage();
					});
			}

			app.UseCookiePolicy(new CookiePolicyOptions
				{
					MinimumSameSitePolicy = SameSiteMode.None,
					Secure = CookieSecurePolicy.SameAsRequest
				})
				.UseDefaultFiles()
				.UseStaticFiles(new StaticFileOptions
				{
					FileProvider = new PhysicalFileProvider(_environment.ContentRootPath)
				})
				.UseRouting()
				.UseCors()
				.UseAuthentication()
				.UseAuthorization()
				.UseEndpoints(endpoints =>
				{
					endpoints.MapControllers();

					// Last route
					endpoints.MapDefaultRoutes();
				});
		}
	}
}
