// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

using System;
using System.IO;
using essentialMix.Core.Web.Middleware;
using essentialMix.Extensions;
using essentialMix.Helpers;
using essentialMix.Newtonsoft.Helpers;
using essentialMix.Newtonsoft.Serialization;
using JetBrains.Annotations;
using IdentityServer4;
using IdentityServerHost.Quickstart.UI;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MongoPOC.Data;
using MongoPOC.Data.Settings;
using MongoPOC.Model;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Scrutor;
using Serilog;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace MongoPOC.IS
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

		public void ConfigureServices(IServiceCollection services)
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
				.AddAutoMapper((_, builder) => builder.AddProfile(new AutoMapperProfiles()), new[]
				{
					typeof(AutoMapperProfiles).Assembly
				}, ServiceLifetime.Singleton)
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

				// Identity server
				IIdentityServerBuilder isBuilder = services.AddIdentityServer(options =>
															{
																options.Events.RaiseErrorEvents = true;
																options.Events.RaiseInformationEvents = true;
																options.Events.RaiseFailureEvents = true;
																options.Events.RaiseSuccessEvents = true;

																// see https://identityserver4.readthedocs.io/en/latest/topics/resources.html
																options.EmitStaticAudienceClaim = true;
															})
															.AddTestUsers(TestUsers.Users)
															.AddInMemoryIdentityResources(identityServerSettings.IdentityResources)
															.AddInMemoryApiScopes(identityServerSettings.ApiScopes)
															.AddInMemoryClients(identityServerSettings.Clients);
			// not recommended for production - you need to store your key material somewhere secure
			if (_environment.IsDevelopment()) isBuilder.AddDeveloperSigningCredential();

			// Authentication
			services.AddAuthentication()
					.AddCookie(options =>
					{
						options.SlidingExpiration = true;
						options.LoginPath = "/account/login";
						options.LogoutPath = "/account/logout";
						options.ExpireTimeSpan = TimeSpan.FromMinutes(identityServerSettings.Timeout);
					})
					.AddGoogle(options =>
					{
						options.SignInScheme = IdentityServerConstants.ExternalCookieAuthenticationScheme;

						// register your IdentityServer with Google at https://console.developers.google.com
						// enable the Google+ API
						// set the redirect URI to https://localhost:5001/signin-google
						options.ClientId = "copy client ID from Google here";
						options.ClientSecret = "copy client secret from Google here";
					});

			services
				// MVC
				.AddDefaultCorsPolicy(builder => builder.WithExposedHeaders("Set-Cookie"), allowedClients)
				.AddForwardedHeaders()
				.AddControllersWithViews()
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

		public void Configure([NotNull] IApplicationBuilder app)
		{
			if (_environment.IsDevelopment()) app.UseDefaultExceptionDelegate(_logger);
			else app.UseRedirectWithStatusCode("/error/{0}");

			if (!_environment.IsDevelopment() || _configuration.GetValue<bool>("useSSL")) app.UseHsts();
			app
				.UseHttpsRedirection()
				.UseForwardedHeaders()
				.UseCultureHandler()
				.UseSerilogRequestLogging()
				.UseCookiePolicy(new CookiePolicyOptions
				{
					MinimumSameSitePolicy = SameSiteMode.None,
					Secure = CookieSecurePolicy.SameAsRequest
				})
				.UseDefaultFiles()
				.UseStaticFiles(new StaticFileOptions
				{
					FileProvider = new PhysicalFileProvider(Path.Combine(_environment.ContentRootPath, "wwwroot"))
				})
				.UseRouting()
				.UseCors()
				.UseAuthentication()
				.UseRouting()
				.UseIdentityServer()
				.UseAuthorization()
				.UseEndpoints(endpoints =>
				{
					endpoints.MapDefaultControllerRoute();
			
					// Last route
					endpoints.MapDefaultRoutes();
				});
		}
	}
}