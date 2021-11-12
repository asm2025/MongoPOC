using System;
using essentialMix.Core.Web.Middleware;
using essentialMix.Extensions;
using essentialMix.Helpers;
using essentialMix.Newtonsoft.Helpers;
using essentialMix.Newtonsoft.Serialization;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
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
			IdentityServerSettings identityServerSettings = _configuration.GetSection("IdentityServer").Get<IdentityServerSettings>();

			services
				// config
				.AddSingleton(_configuration)
				.AddSingleton(_environment)
				.AddSingleton<IDbConfig>(dbConfig)
				.AddSingleton<IIdentityServerSettings>(identityServerSettings)
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
				// Database
				.AddIdentity<User, Role>(options =>
				{
					options.Stores.MaxLengthForKeys = 128;
					options.User.RequireUniqueEmail = true;
				})
				.AddMongoDbStores<User, Role, Guid>(dbConfig.ConnectionString, dbConfig.Database)
				.AddUserManager<UserManager<User>>()
				.AddRoleManager<RoleManager<Role>>()
				.AddRoleValidator<RoleValidator<Role>>()
				.AddSignInManager<SignInManager<User>>()
				.AddDefaultTokenProviders();
			services.AddSingleton<BookService>();
			
			// Identity
			IIdentityServerBuilder identityServerBuilder = services.AddIdentityServer(options =>
																	{
																		options.Events.RaiseErrorEvents = true;
																		options.Events.RaiseFailureEvents = true;
																		options.Events.RaiseSuccessEvents = true;
																	})
																	.AddAspNetIdentity<User>()
																	.AddInMemoryPersistedGrants()
																	.AddInMemoryApiScopes(identityServerSettings.ApiScopes)
																	.AddInMemoryApiResources(identityServerSettings.ApiResources)
																	.AddInMemoryClients(identityServerSettings.Clients)
																	.AddInMemoryIdentityResources(identityServerSettings.IdentityResources);
			if (_environment.IsDevelopment()) identityServerBuilder.AddDeveloperSigningCredential();

			// Authentication
			services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme);
			services
				.AddAuthentication(options =>
				{
					options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
					options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
				})
				.AddCookie(options =>
				{
					options.SlidingExpiration = true;
					options.LoginPath = "/users/login";
					options.LogoutPath = "/users/logout";
					options.ExpireTimeSpan = TimeSpan.FromMinutes(_configuration.GetValue("jwt:timeout", 20).NotBelow(5));
				})
				.AddJwtBearer(options =>
				{
					SecurityKey signingKey = SecurityKeyHelper.CreateSymmetricKey(_configuration.GetValue<string>("jwt:signingKey"), 256);
					//SecurityKey decryptionKey = SecurityKeyHelper.CreateSymmetricKey(_configuration.GetValue<string>("jwt:encryptionKey"), 256);
					options.Setup(signingKey, /*decryptionKey, */_configuration, _environment.IsDevelopment());
				})
				.AddOpenIdConnect(OpenIdConnectDefaults.AuthenticationScheme, options =>
				{
					options.Authority = _configuration.GetValue<string>("jwt:authority").ToNullIfEmpty();
					options.ClientId = "asm";
					options.ResponseType = "code";
					options.Scope.Add("openid");
					options.Scope.Add("profile");
					options.Scope.Add("fullaccess");
					options.Scope.Add("roles");
					options.ClaimActions.MapUniqueJsonKey("role", "role");
					options.SaveTokens = true;
				});

			// Authorization
			services.AddAuthorization(options =>
			{
				options.AddPolicy("Deactivate", policy =>
				{
					policy.RequireRole("Admin Manager");
					policy.RequireAuthenticatedUser();
					policy.RequireClaim("email");
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
					options.SerializerSettings.DateFormatHandling = DateFormatHandling.IsoDateFormat;

					JsonSerializerSettingsConverters allConverters = EnumHelper<JsonSerializerSettingsConverters>.GetAllFlags() &
																	~(JsonSerializerSettingsConverters.IsoDateTime |
																	JsonSerializerSettingsConverters.JavaScriptDateTime |
																	JsonSerializerSettingsConverters.UnixDateTime);
					options.SerializerSettings.AddConverters(allConverters);
				});
		}

		// This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
		public void Configure([NotNull] IApplicationBuilder app, [NotNull] IWebHostEnvironment env)
		{
			if (env.IsDevelopment())
				app.UseDefaultExceptionDelegate(_logger);
			else if (env.IsProduction() || env.IsStaging())
				app.UseExceptionHandler("/Error");

			if (!env.IsDevelopment() || _configuration.GetValue<bool>("useSSL")) app.UseHsts();

			app.UseHttpsRedirection()
				.UseForwardedHeaders()
				.UseCultureHandler()
				.UseSerilogRequestLogging()
				.UseSwagger(config => config.RouteTemplate = _configuration.GetValue<string>("swagger:template"))
				.UseSwaggerUI(config =>
				{
					config.SwaggerEndpoint(_configuration.GetValue<string>("swagger:ui"), _configuration.GetValue("title", _environment.ApplicationName));
					config.AsStartPage();
				})
				.UseDefaultFiles()
				.UseStaticFiles(new StaticFileOptions
				{
					FileProvider = new PhysicalFileProvider(AssemblyHelper.GetEntryAssembly().GetDirectoryPath())
				})
				.UseCookiePolicy(new CookiePolicyOptions
				{
					MinimumSameSitePolicy = SameSiteMode.None,
					Secure = CookieSecurePolicy.SameAsRequest
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
