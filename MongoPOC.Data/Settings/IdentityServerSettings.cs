using System;
using System.Collections.Generic;
using IdentityServer4.Models;

namespace MongoPOC.Data.Settings
{
	public class IdentityServerSettings : IIdentityServerSettings
	{
		public string Authority { get; init; }
		public int Timeout { get; init; }
		public int RefreshInterval { get; init; }
		public Uri AuthorizationUrl { get; init; }
		public Uri TokenUrl { get; init; }
		public IReadOnlyCollection<ApiResource> ApiResources { get; init; }
		public IReadOnlyCollection<IdentityResource> IdentityResources { get; init; }
		public IReadOnlyCollection<ApiScope> ApiScopes { get; init; }
		public IReadOnlyCollection<Client> Clients { get; init; }

	}
}
