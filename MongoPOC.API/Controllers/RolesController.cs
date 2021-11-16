using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using essentialMix.Core.Web.Controllers;
using essentialMix.Extensions;
using essentialMix.Patterns.Pagination;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MongoPOC.Data;
using MongoPOC.Model;
using MongoPOC.Model.DTO;

namespace MongoPOC.API.Controllers
{
	[Route("[controller]")]
	[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
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
		[Authorize(Roles = Role.Administrators)]
		public async Task<IActionResult> Create([Required][NotNull] string name)
		{
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
		public IActionResult List([FromQuery] Pagination pagination)
		{
			IQueryable<Role> queryable = _roleManager.Roles;
		
			if (pagination != null)
			{
				queryable = queryable.Skip((pagination.Page - 1) * pagination.PageSize)
									.Take(pagination.PageSize);
			}

			IList<RoleForList> roles = queryable.ProjectTo<RoleForList>(_mapper.ConfigurationProvider)
													.ToList();
			return Ok(roles);
		}

		[HttpGet("{id:guid}")]
		public async Task<IActionResult> Get([FromRoute] Guid id)
		{
			if (id.IsEmpty()) return BadRequest();
		
			Role role = await _roleManager.FindByIdAsync(id.ToHexString());
			if (role == null) return NotFound(id);
			
			RoleForList roleForList = _mapper.Map<RoleForList>(role);
			return Ok(roleForList);
		}

		[HttpPut("{id:guid}/[action]")]
		[Authorize(Roles = Role.Administrators)]
		public async Task<IActionResult> Update([FromRoute] Guid id, [Required][NotNull] string name)
		{
			if (!ModelState.IsValid) return ValidationProblem();
			if (id.IsEmpty() || string.IsNullOrWhiteSpace(name)) return BadRequest();

			Role role = await _roleManager.FindByIdAsync(id.ToHexString());
			if (role == null) return NotFound(id);
			if (role.Name.IsSame(Role.Administrators) || role.Name.IsSame(Role.Members)) return Unauthorized();
			role.Name = name;

			IdentityResult result = await _roleManager.UpdateAsync(role);
			if (result.Succeeded) return Ok();

			foreach (IdentityError error in result.Errors)
				ModelState.AddModelError(string.Empty, error.Description);

			return ValidationProblem();
		}

		[HttpDelete("{id:guid}/[action]")]
		[Authorize(Roles = Role.Administrators)]
		public async Task<IActionResult> Delete([FromRoute] Guid id)
		{
			if (id.IsEmpty()) return BadRequest();

			Role role = await _roleManager.FindByIdAsync(id.ToHexString());
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
