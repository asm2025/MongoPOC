using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using essentialMix.Core.Web.Controllers;
using essentialMix.Extensions;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MongoPOC.Model;
using MongoPOC.Model.DTO;

namespace MongoPOC.Identity.Controllers
{
	[Authorize(AuthenticationSchemes = Constants.Authentication.AuthenticationSchemes)]
	[Route("[controller]")]
	public class RolesController : ApiController
	{
		private readonly RoleManager<Role> _roleManager;
		private readonly IMapper _mapper;

		/// <inheritdoc />
		public RolesController([NotNull] RoleManager<Role> roleManager, IMapper mapper, [NotNull] IConfiguration configuration, ILogger<UsersController> logger)
			: base(configuration, logger)
		{
			_roleManager = roleManager;
			_mapper = mapper;
		}

		[HttpPost]
		public async Task<IActionResult> Create([Required] [NotNull] string name)
		{
			if (!User.IsInRole(Role.Administrators)) return Unauthorized();
			if (!ModelState.IsValid) return ValidationProblem();

			Role role = new Role(name);
			IdentityResult result = await _roleManager.CreateAsync(role);

			if (!result.Succeeded)
			{
				foreach (IdentityError error in result.Errors)
					ModelState.AddModelError(string.Empty, error.Description);

				return ValidationProblem();
			}

			RoleForSerialization roleForSerialization = _mapper.Map<RoleForSerialization>(role);
			return CreatedAtAction(nameof(Get), new
			{
				id = role.Id
			}, roleForSerialization);
		}

		[HttpGet()]
		public async Task<IActionResult> Get()
		{
			IQueryable<Role> queryable = _roleManager.Roles;
			IList<RoleForSerialization> roles = await queryable.ProjectTo<RoleForSerialization>(_mapper.ConfigurationProvider)
																.ToListAsync();
			return Ok(roles);
		}

		[HttpGet("{id}")]
		public async Task<IActionResult> Get([FromRoute] string id)
		{
			if (string.IsNullOrEmpty(id)) return BadRequest();
			Role role = await _roleManager.FindByIdAsync(id);
			if (role == null) return NotFound(id);
			RoleForSerialization roleForSerialization = _mapper.Map<RoleForSerialization>(role);
			return Ok(roleForSerialization);
		}

		[HttpDelete("{id}")]
		public async Task<IActionResult> Delete([FromRoute] string id)
		{
			if (!User.IsInRole(Role.Administrators)) return Unauthorized();
			if (string.IsNullOrEmpty(id)) return BadRequest();

			Role role = await _roleManager.FindByIdAsync(id);
			if (role == null) return NotFound(id);
			if (role.Name.IsSame(Role.Administrators) || role.Name.IsSame(Role.Members)) return Unauthorized();
			
			IdentityResult result = await _roleManager.DeleteAsync(role);
			if (result.Succeeded) return Ok();

			foreach (IdentityError error in result.Errors)
				ModelState.AddModelError(string.Empty, error.Description);

			return ValidationProblem();

		}
	}
}
