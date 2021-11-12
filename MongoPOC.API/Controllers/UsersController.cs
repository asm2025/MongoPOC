using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using essentialMix.Core.Web.Controllers;
using essentialMix.Extensions;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MongoPOC.Data;
using MongoPOC.Model;
using MongoPOC.Model.DTO;

namespace MongoPOC.API.Controllers
{
	[Authorize(AuthenticationSchemes =OpenIdConnectDefaults.AuthenticationScheme)]
	[Route("[controller]")]
	public class UsersController : ApiController
	{
		private readonly UserManager<User> _userManager;
		private readonly IMapper _mapper;

		/// <inheritdoc />
		public UsersController([NotNull] IMongoPOCContext context, [NotNull] IMapper mapper, [NotNull] IConfiguration configuration, [NotNull] ILogger<UsersController> logger)
			: base(configuration, logger)
		{
			_userManager = context.UserManager;
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

		[AllowAnonymous]
		[HttpPost("[action]")]
		public async Task<IActionResult> Register([FromBody][NotNull] UserToRegister userParams)
		{
			if (!ModelState.IsValid) return ValidationProblem();

			User user = _mapper.Map<User>(userParams);
			user.UpdatedOn = DateTime.UtcNow;

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

		[HttpGet]
		public async Task<IActionResult> Get()
		{
			if (!User.IsInRole(Role.Administrators)) return Unauthorized();

			IQueryable<User> queryable = _userManager.Users;
			if (queryable == null) return NoContent();

			IList<UserForList> users = await queryable.ProjectTo<UserForList>(_mapper.ConfigurationProvider)
													.ToListAsync();
			return Ok(users);
		}

		[HttpGet("{id:length(128)}")]
		public async Task<IActionResult> Get([FromRoute] string id)
		{
			if (string.IsNullOrEmpty(id)) return BadRequest();
			
			User user = await _userManager.FindByIdAsync(id);
			if (user == null) return NotFound(id);
			
			UserForDetails userForDetails = _mapper.Map<UserForDetails>(user);
			return Ok(userForDetails);
		}

		[HttpPut("{id:length(128)}")]
		public async Task<IActionResult> Update([FromRoute] string id, [FromBody][NotNull] UserToUpdate userToUpdate)
		{
			if (string.IsNullOrEmpty(id)) return BadRequest();
			if (!ModelState.IsValid) return ValidationProblem();

			string currentUserId = User.FindFirst(ClaimTypes.Name)?.Value;
			if (string.IsNullOrEmpty(currentUserId) || !currentUserId.IsSame(id) && !User.IsInRole(Role.Administrators)) return Unauthorized();

			User user = await _userManager.FindByIdAsync(id);
			if (user == null) return NotFound(id);
			_mapper.Map(userToUpdate, user);

			IdentityResult result = await _userManager.UpdateAsync(user);

			if (!result.Succeeded)
			{
				foreach (IdentityError error in result.Errors)
					ModelState.AddModelError(string.Empty, error.Description);

				return ValidationProblem();
			}

			UserForDetails userForDetails = _mapper.Map<UserForDetails>(user);
			return Ok(userForDetails);
		}

		[HttpDelete("{id:length(128)}")]
		public async Task<IActionResult> Delete([FromRoute] string id)
		{
			if (string.IsNullOrEmpty(id)) return BadRequest();

			string currentUserId = User.FindFirst(ClaimTypes.Name)?.Value;
			if (string.IsNullOrEmpty(currentUserId) || !currentUserId.IsSame(id) && !User.IsInRole(Role.Administrators)) return Unauthorized();

			User user = await _userManager.FindByIdAsync(id);
			if (user == null) return NotFound(id);

			IdentityResult result = await _userManager.DeleteAsync(user);
			if (result.Succeeded) return Ok();

			foreach (IdentityError error in result.Errors)
				ModelState.AddModelError(string.Empty, error.Description);

			return ValidationProblem();
		}
	}
}
