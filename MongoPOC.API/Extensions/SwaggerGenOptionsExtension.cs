using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace MongoPOC.API.Extensions
{
	public static class SwaggerGenOptionsExtension
	{
		[NotNull]
		public static SwaggerGenOptions AddOpenIdConnectSecurity([NotNull] this SwaggerGenOptions thisValue, [NotNull] Uri authorizationUrl, [NotNull] Uri tokenUrl, IDictionary<string, string> scopes, string description = null)
		{
			if (string.IsNullOrEmpty(description)) description = $"OAuth2 using the {OpenIdConnectDefaults.AuthenticationScheme} scheme.";
			scopes ??= new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
			{
				{"api", "API - full access"}
			};
			thisValue.AddSecurityDefinition(OpenIdConnectDefaults.AuthenticationScheme, new OpenApiSecurityScheme
			{
				Type = SecuritySchemeType.OAuth2,
				Description = description,
				Flows = new OpenApiOAuthFlows
				{
					AuthorizationCode = new OpenApiOAuthFlow
					{
						AuthorizationUrl = authorizationUrl,
						TokenUrl = tokenUrl,
						Scopes = scopes
					}
				}
			});

			thisValue.AddSecurityRequirement(new OpenApiSecurityRequirement
			{
				{
					new OpenApiSecurityScheme
					{
						Scheme = SecuritySchemeType.OAuth2.ToString(),
						Name = OpenIdConnectDefaults.AuthenticationScheme,
						Reference = new OpenApiReference
						{
							Id = OpenIdConnectDefaults.AuthenticationScheme,
							Type = ReferenceType.SecurityScheme
						}
					},
					scopes.Keys.ToList()
				}
			});

			return thisValue;
		}
	}
}