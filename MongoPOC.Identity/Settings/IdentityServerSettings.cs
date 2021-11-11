using System.Collections.Generic;
using IdentityServer4.Models;
using JetBrains.Annotations;

namespace MongoPOC.Identity.Settings
{
	public class IdentityServerSettings
	{
		public IReadOnlyCollection<ApiScope> ApiScopes { get; init; }
		public IReadOnlyCollection<ApiResource> ApiResources { get; init; }

		public IReadOnlyCollection<Client> Clients { get; init; }

		[NotNull]
		public IReadOnlyCollection<IdentityResource> IdentityResources { get; } = new[]
		{
			new IdentityResources.OpenId(),
			new IdentityResources.Profile(),
			new IdentityResource("roles", "User role(s)", new List<string>
			{
				"role"
			})
		};
	}
}
