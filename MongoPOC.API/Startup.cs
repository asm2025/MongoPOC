using System;
using System.Linq;
using essentialMix.Core.Web.Middleware;
using essentialMix.Extensions;
using essentialMix.Helpers;
using essentialMix.Newtonsoft.Helpers;
using essentialMix.Newtonsoft.Serialization;
using JetBrains.Annotations;
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
using MongoPOC.API.Extensions;
using MongoPOC.Data;
using MongoPOC.Data.Settings;
using MongoPOC.Model;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Swashbuckle.AspNetCore.Filters;
using Serilog;

namespace MongoPOC.API
{
	public class Startup
	{
		[NotNull]
		private readonly IHostEnvironment _environment;

		[NotNull]
		private readonly IConfiguration _configuration;

		public Startup([NotNull] IHostEnvironment environment, [NotNull] IConfiguration configuration)
		{
			_environment = environment;
			_configuration = configuration;
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
							.AddOpenIdConnectSecurity(identityServerSettings.AuthorizationUrl, 
													identityServerSettings.TokenUrl, 
													identityServerSettings.ApiScopes.ToDictionary(e => e.Name, e => e.Description));
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
			services
				.AddScoped<IMongoPOCContext, MongoPOCContext>()
				.AddScoped<BookService>()
				// Identity
				.AddIdentityServer(options =>
				{
					options.Events.RaiseErrorEvents = true;
					options.Events.RaiseFailureEvents = true;
					options.Events.RaiseInformationEvents = true;
					options.Events.RaiseSuccessEvents = true;
				})
				.AddAspNetIdentity<User>()
				.AddInMemoryPersistedGrants()
				.AddInMemoryApiScopes(identityServerSettings.ApiScopes)
				.AddInMemoryApiResources(identityServerSettings.ApiResources)
				.AddInMemoryIdentityResources(identityServerSettings.IdentityResources)
				.AddInMemoryClients(identityServerSettings.Clients)
				.AddDeveloperSigningCredential();

			// Authentication
			services
				.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
				.AddCookie(options =>
				{
					options.SlidingExpiration = true;
					options.LoginPath = "/connect/authorize";
					options.LogoutPath = "/connect/logout";
					options.ExpireTimeSpan = TimeSpan.FromMinutes(identityServerSettings.Timeout);
				})
				.AddIdentityServerAuthentication(OpenIdConnectDefaults.AuthenticationScheme, option =>
				{
					option.ApiName = "api";
					option.Authority = identityServerSettings.Authority;
					option.ApiSecret = "endc@m+Y8hZCW&MAEEb5RY2?AeE75d3?";
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
			app.UseExceptionHandler("/error");
			if (!env.IsDevelopment() || _configuration.GetValue<bool>("useSSL")) app.UseHsts();
			app.UseHttpsRedirection()
				.UseForwardedHeaders()
				.UseCultureHandler()
				.UseSerilogRequestLogging();

			if (env.IsDevelopment())
			{
				app.UseSwagger(options => options.RouteTemplate = _configuration.GetValue<string>("swagger:template"))
					.UseSwaggerUI(options =>
					{
						options.SwaggerEndpoint(_configuration.GetValue<string>("swagger:ui"), _configuration.GetValue("title", _environment.ApplicationName));
						options.OAuthUsePkce();
						options.AsStartPage();
					});
			}

			app.UseCookiePolicy(new CookiePolicyOptions
				{
					MinimumSameSitePolicy = SameSiteMode.None,
					Secure = CookieSecurePolicy.SameAsRequest
				})
				.UseStaticFiles(new StaticFileOptions
				{
					FileProvider = new PhysicalFileProvider(AssemblyHelper.GetEntryAssembly().GetDirectoryPath())
				})
				.UseRouting()
				.UseCors()
				.UseIdentityServer()
				.UseAuthentication()
				.UseAuthorization()
				.UseDefaultFiles()
				.UseEndpoints(endpoints =>
				{
					endpoints.MapControllers();

					// Last route
					endpoints.MapDefaultRoutes();
				});
		}
	}
}
