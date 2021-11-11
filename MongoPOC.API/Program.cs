using System;
using System.IO;
using System.Text;
using essentialMix.Core.Web.Helpers;
using essentialMix.Extensions;
using essentialMix.Helpers;
using JetBrains.Annotations;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace MongoPOC.API
{
	public class Program
	{
		public static int Main(string[] args)
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

			// Logging
			LoggerConfiguration loggerConfiguration = new LoggerConfiguration();
			if (configuration.GetValue<bool>("Logging:Enabled")) loggerConfiguration.ReadFrom.Configuration(configuration);
			Log.Logger = loggerConfiguration.CreateLogger();
			
			IWebHost host = CreateHostBuilder(args).Build();
			ILogger logger = host.Services.GetRequiredService<ILogger<Program>>();
			IServiceScope scope = null;

			try
			{
				scope = host.Services.CreateScope();
				logger.LogInformation($"{configuration.GetValue<string>("title")} is starting...");
				host.Run();
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
}
