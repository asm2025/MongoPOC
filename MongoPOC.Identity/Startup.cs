using System;
using essentialMix.Core.Web.Middleware;
using essentialMix.Extensions;
using essentialMix.Helpers;
using essentialMix.Newtonsoft.Helpers;
using essentialMix.Newtonsoft.Serialization;
using JetBrains.Annotations;
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
using MongoPOC.Data.Settings;
using MongoPOC.Data.Swagger.Examples;
using MongoPOC.Identity.Settings;
using MongoPOC.Model;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Swashbuckle.AspNetCore.Filters;
using Serilog;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace MongoPOC.Identity
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
			MongoDbConfig mongoDbConfig = _configuration.GetSection("data").Get<MongoDbConfig>();
			IdentityServerSettings identityServerSettings = _configuration.GetSection("IdentityServer").Get<IdentityServerSettings>();
			
			services
				// config
				.AddSingleton(_configuration)
				.AddSingleton(_environment)
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
				.AddSwaggerExamplesFromAssemblyOf<UserToRegisterExample>()
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
				.AddAutoMapper((_, builder) => builder.AddProfile(new AutoMapperProfiles()),
								new [] { typeof(AutoMapperProfiles).Assembly },
								ServiceLifetime.Singleton);
			// Database
			services.AddIdentity<User, Role>(options =>
					{
						options.Stores.MaxLengthForKeys = 128;
						options.User.RequireUniqueEmail = true;
					})
					.AddMongoDbStores<User, Role, Guid>(mongoDbConfig.ConnectionString, mongoDbConfig.Name)
					.AddUserManager<UserManager<User>>()
					.AddRoleManager<RoleManager<Role>>()
					.AddRoleValidator<RoleValidator<Role>>()
					.AddSignInManager<SignInManager<User>>()
					.AddDefaultTokenProviders();
			
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

			// MVC
			services
				.AddDefaultCorsPolicy(builder => builder.WithExposedHeaders("Set-Cookie"), allowedClients)
				.AddForwardedHeaders()
				.AddControllers()
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
