using AutoMapper;
using essentialMix.Core.Web.Controllers;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MongoPOC.Data;
using MongoPOC.Model;

namespace MongoPOC.API.Controllers
{
	[AllowAnonymous]
	[Route("[controller]")]
	public class ConnectController : ApiController
	{
		private readonly UserManager<User> _userManager;
		private readonly IMapper _mapper;

		/// <inheritdoc />
		public ConnectController([NotNull] IMongoPOCContext context, [NotNull] IMapper mapper, [NotNull] IConfiguration configuration, [NotNull] ILogger<UsersController> logger)
			: base(configuration, logger)
		{
			_userManager = context.UserManager;
			_mapper = mapper;
		}

		[HttpGet("[action]")]
		public IActionResult Authorize()
		{
			return Ok();
		}

		[HttpGet("[action]")]
		public IActionResult Token()
		{
			return Ok();
		}

		[HttpPost("[action]")]
		public IActionResult Logout()
		{
			if (User?.Identity == null || !User.Identity.IsAuthenticated) return NoContent();
			return Ok();
		}
	}
}
