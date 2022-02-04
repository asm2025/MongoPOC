using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using essentialMix.Core.Web.Helpers;
using essentialMix.Extensions;
using essentialMix.Helpers;
using JetBrains.Annotations;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoPOC.Data;
using Serilog;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace MongoPOC.API;

public class Program
{
	public static async Task<int> Main([NotNull] string[] args)
	{
		Console.OutputEncoding = Encoding.UTF8;
		Directory.SetCurrentDirectory(AssemblyHelper.GetEntryAssembly().GetDirectoryPath());

		// Configuration
		IConfiguration configuration = IConfigurationBuilderHelper.CreateConfiguration()
																.AddConfigurationFiles(EnvironmentHelper.GetEnvironmentName())
																.AddEnvironmentVariables()
																.AddUserSecrets()
																.AddArguments(args)
																.Build();
		// Bson
		BsonSerializer.RegisterSerializer(new GuidSerializer(BsonType.String));

		// Logging
		LoggerConfiguration loggerConfiguration = new LoggerConfiguration();
		if (configuration.GetValue<bool>("LoggingEnabled")) loggerConfiguration.ReadFrom.Configuration(configuration);
		Log.Logger = loggerConfiguration.CreateLogger();

		// Host
		IWebHost host = CreateHostBuilder(args).Build();
		ILogger logger = host.Services.GetRequiredService<ILogger<Program>>();
		IServiceScope scope = null;

		try
		{
			scope = host.Services.CreateScope();
			logger.LogInformation($"{configuration.GetValue<string>("title")} is starting...");

			// Seed data
			IMongoPOCContext dbContext = scope.ServiceProvider.GetRequiredService<IMongoPOCContext>();
			ILogger seedDataLogger = scope.ServiceProvider.GetRequiredService<ILogger<MongoPOCContext>>();
			// We MUST wait for this thing to finish.
			await dbContext.SeedAsync(seedDataLogger);
			await host.RunAsync();
			return 0;
		}
		catch (Exception e)
		{
			logger.LogError(e, e.Message);
			return 1;
		}
		finally
		{
			ObjectHelper.Dispose(ref scope);
			Log.CloseAndFlush();
		}
	}

	[NotNull]
	public static IWebHostBuilder CreateHostBuilder([NotNull] string[] args)
	{
		return WebHost.CreateDefaultBuilder(args)
					.UseSerilog()
					.UseContentRoot(AssemblyHelper.GetEntryAssembly().GetDirectoryPath())
					.ConfigureAppConfiguration((context, configurationBuilder) =>
					{
						configurationBuilder.Setup(context.HostingEnvironment)
											.AddConfigurationFiles(context.HostingEnvironment)
											.AddEnvironmentVariables()
											.AddUserSecrets()
											.AddArguments(args);
					})
					.UseStartup<Startup>();
	}
}