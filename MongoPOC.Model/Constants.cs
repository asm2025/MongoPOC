using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;

namespace MongoPOC.Model
{
	public static class Constants
	{
		public static class Authentication
		{
			public const string AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme + "," + OpenIdConnectDefaults.AuthenticationScheme;
		}
	}
}
