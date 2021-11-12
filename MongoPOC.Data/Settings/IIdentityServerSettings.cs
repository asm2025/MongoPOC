using System.Collections.Generic;
using IdentityServer4.Models;

namespace MongoPOC.Data.Settings
{
	public interface IIdentityServerSettings
	{
		IReadOnlyCollection<ApiScope> ApiScopes { get; init; }
		IReadOnlyCollection<ApiResource> ApiResources { get; init; }
		IReadOnlyCollection<Client> Clients { get; init; }
		IReadOnlyCollection<IdentityResource> IdentityResources { get; }
	}
}