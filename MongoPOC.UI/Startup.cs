using essentialMix.Core.Web.Middleware;
using essentialMix.Extensions;
using essentialMix.Helpers;
using essentialMix.Newtonsoft.Helpers;
using essentialMix.Newtonsoft.Serialization;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Serilog;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace MongoPOC.UI
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
		public void ConfigureServices(IServiceCollection services)
		{
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
				// FormOptions
				.Configure<FormOptions>(options =>
				{
					options.ValueLengthLimit = int.MaxValue;
					options.MultipartBodyLengthLimit = int.MaxValue;
					options.MemoryBufferThreshold = int.MaxValue;
				})
				// Helpers
				.AddHttpContextAccessor();

			// MVC
			services
				.AddDefaultCorsPolicy(builder => builder.WithExposedHeaders("Set-Cookie"), "*")
				.AddForwardedHeaders()
				.AddControllersWithViews()
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
				.UseStaticFiles(new StaticFileOptions
				{
					FileProvider = new PhysicalFileProvider(AssemblyHelper.GetEntryAssembly().GetDirectoryPath())
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
