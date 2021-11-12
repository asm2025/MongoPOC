using System.Collections.Generic;
using IdentityServer4.Models;

namespace MongoPOC.Data.Settings
{
	public interface IIdentityServerSettings
	{
		string Authority { get; }
		int Timeout { get; }
		int RefreshInterval { get; }
		IReadOnlyCollection<ApiScope> ApiScopes { get; }
		IReadOnlyCollection<ApiResource> ApiResources { get; }
		IReadOnlyCollection<Client> Clients { get; }
		IReadOnlyCollection<IdentityResource> IdentityResources { get; }
	}
}