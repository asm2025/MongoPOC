using System;
using System.Threading.Tasks;
using AutoMapper;
using essentialMix.Core.Web.Controllers;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MongoPOC.Model;
using MongoPOC.Model.DTO;

namespace MongoPOC.Identity.Controllers
{
	[Authorize(AuthenticationSchemes = Constants.Authentication.AuthenticationSchemes)]
	[Route("[controller]")]
	public class UsersController : ApiController
	{
		private readonly UserManager<User> _userManager;
		private readonly IMapper _mapper;

		/// <inheritdoc />
		public UsersController([NotNull] UserManager<User> userManager, IMapper mapper, [NotNull] IConfiguration configuration, ILogger<UsersController> logger)
			: base(configuration, logger)
		{
			_userManager = userManager;
			_mapper = mapper;
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

		[HttpPost]
		public async Task<IActionResult> Register([FromBody][NotNull] UserToRegister userParams)
		{
			if (!ModelState.IsValid) return ValidationProblem();

			User user = _mapper.Map<User>(userParams);
			user.Created = DateTime.UtcNow;
			user.Modified = user.Created;

			IdentityResult result = string.IsNullOrEmpty(userParams.Password)
										? await _userManager.CreateAsync(user)
										: await _userManager.CreateAsync(user, userParams.Password);

			if (!result.Succeeded)
			{
				foreach (IdentityError error in result.Errors)
					ModelState.AddModelError(string.Empty, error.Description);

				return ValidationProblem();
			}

			await _userManager.AddToRoleAsync(user, Role.Members);
			UserForSerialization userForSerialization = _mapper.Map<UserForSerialization>(user);
			return CreatedAtAction(nameof(Get), new
			{
				id = user.Id
			}, userForSerialization);
		}

		[HttpGet("{id}")]
		public async Task<IActionResult> Get([FromRoute] string id)
		{
			if (string.IsNullOrEmpty(id)) return BadRequest();
			User user = await _userManager.FindByIdAsync(id);
			if (user == null) return NotFound(id);
			UserForDetails userForDetails = _mapper.Map<UserForDetails>(user);
			return Ok(userForDetails);
		}
	}
}
