using essentialMix.Core.Web.Controllers;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MongoPOC.Model;
using MongoPOC.Model.DTO;

namespace MongoPOC.API.Controllers
{
	[Authorize(AuthenticationSchemes = Constants.Authentication.AuthenticationSchemes)]
	[Route("[controller]")]
	public class UsersController : ApiController
	{
		/// <inheritdoc />
		public UsersController([NotNull] IConfiguration configuration, ILogger<UsersController> logger)
			: base(configuration, logger)
		{
		}

		[AllowAnonymous]
		[HttpPost("[action]")]
		public IActionResult Login([FromBody][NotNull] UserForLogin user)
		{
			return Ok();
		}

		[AllowAnonymous]
		[HttpPost("[action]")]
		public IActionResult Logout()
		{
			if (User?.Identity == null || !User.Identity.IsAuthenticated) return NoContent();
			return Ok();
		}
	}
}
