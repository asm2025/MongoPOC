using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
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
	[Authorize(AuthenticationSchemes = OpenIdConnectDefaults.AuthenticationScheme)]
	[Route("[controller]")]
	public class RolesController : ApiController
	{
		private readonly RoleManager<Role> _roleManager;
		private readonly IMapper _mapper;

		/// <inheritdoc />
		public RolesController([NotNull] IMongoPOCContext context, IMapper mapper, [NotNull] IConfiguration configuration, [NotNull] ILogger<UsersController> logger)
			: base(configuration, logger)
		{
			_roleManager = context.RoleManager;
			_mapper = mapper;
		}

		[HttpPost("[action]")]
		public async Task<IActionResult> Create([Required][NotNull] string name)
		{
			if (!User.IsInRole(Role.Administrators)) return Unauthorized();
			if (!ModelState.IsValid) return ValidationProblem();
			if (name.IsSame(Role.Administrators) || name.IsSame(Role.Members)) return Ok();

			Role role = new Role(name);
			IdentityResult result = await _roleManager.CreateAsync(role);

			if (!result.Succeeded)
			{
				foreach (IdentityError error in result.Errors)
					ModelState.AddModelError(string.Empty, error.Description);

				return ValidationProblem();
			}

			RoleForList roleForList = _mapper.Map<RoleForList>(role);
			return CreatedAtAction(nameof(Get), new
			{
				id = role.Id
			}, roleForList);
		}

		[HttpGet]
		public async Task<IActionResult> Get()
		{
			if (!User.IsInRole(Role.Administrators)) return Unauthorized();

			IQueryable<Role> queryable = _roleManager.Roles;
			if (queryable == null) return NoContent();
			
			IList<RoleForList> roles = await queryable.ProjectTo<RoleForList>(_mapper.ConfigurationProvider)
													.ToListAsync();
			return Ok(roles);
		}

		[HttpGet("{id:length(128)}")]
		public async Task<IActionResult> Get([FromRoute] string id)
		{
			if (string.IsNullOrEmpty(id)) return BadRequest();
		
			Role role = await _roleManager.FindByIdAsync(id);
			if (role == null) return NotFound(id);
			
			RoleForList roleForList = _mapper.Map<RoleForList>(role);
			return Ok(roleForList);
		}

		[HttpPut("[action]/{id:length(128)}")]
		public async Task<IActionResult> Update([FromRoute] string id, [Required][NotNull] string name)
		{
			if (!User.IsInRole(Role.Administrators)) return Unauthorized();
			if (!ModelState.IsValid) return ValidationProblem();
			if (string.IsNullOrEmpty(id) || string.IsNullOrWhiteSpace(name)) return BadRequest();

			Role role = await _roleManager.FindByIdAsync(id);
			if (role == null) return NotFound(id);
			if (role.Name.IsSame(Role.Administrators) || role.Name.IsSame(Role.Members)) return Unauthorized();
			role.Name = name;

			IdentityResult result = await _roleManager.UpdateAsync(role);
			if (result.Succeeded) return Ok();

			foreach (IdentityError error in result.Errors)
				ModelState.AddModelError(string.Empty, error.Description);

			return ValidationProblem();
		}

		[HttpDelete("[action]/{id:length(128)}")]
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
