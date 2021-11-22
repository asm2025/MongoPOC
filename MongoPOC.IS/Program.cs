// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

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
using Serilog;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace MongoPOC.IS
{
	public class Program
	{
		public static async Task<int> Main(string[] args)
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

			try
			{
				logger.LogInformation($"{configuration.GetValue<string>("title")} is starting...");
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
				Log.CloseAndFlush();
			}
		}

		[NotNull]
		public static IWebHostBuilder CreateHostBuilder(string[] args)
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