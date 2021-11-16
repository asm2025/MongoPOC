using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Threading.Tasks;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using essentialMix.Core.Web.Controllers;
using essentialMix.Extensions;
using essentialMix.Patterns.Pagination;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MongoPOC.Data;
using MongoPOC.Model;
using MongoPOC.Model.DTO;
using Swashbuckle.AspNetCore.Annotations;

namespace MongoPOC.API.Controllers
{
	[Route("[controller]")]
	[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
	public class UsersController : ApiController
	{
		private const string REFRESH_TOKEN_NAME = "refreshToken";

		private readonly IMongoPOCContext _context;
		private readonly BookService _bookService;
		private readonly IMapper _mapper;

		/// <inheritdoc />
		public UsersController([NotNull] IMongoPOCContext context, [NotNull] BookService bookService, [NotNull] IMapper mapper, [NotNull] IConfiguration configuration, [NotNull] ILogger<UsersController> logger)
			: base(configuration, logger)
		{
			_context = context;
			_bookService = bookService;
			_mapper = mapper;
		}

		[AllowAnonymous]
		[HttpPost("[action]")]
		[SwaggerResponse((int)HttpStatusCode.Created)]
		public async Task<IActionResult> Register([FromBody][NotNull] UserToRegister userParams)
		{
			if (!ModelState.IsValid) return ValidationProblem();

			User user = _mapper.Map<User>(userParams);
			user.UpdatedOn = DateTime.UtcNow;

			IdentityResult result = string.IsNullOrEmpty(userParams.Password)
										? await _context.UserManager.CreateAsync(user)
										: await _context.UserManager.CreateAsync(user, userParams.Password);

			if (!result.Succeeded)
			{
				foreach (IdentityError error in result.Errors)
					ModelState.AddModelError(string.Empty, error.Description);

				return ValidationProblem();
			}

			await _context.UserManager.AddToRoleAsync(user, Role.Members);
			
			UserForSerialization userForSerialization = _mapper.Map<UserForSerialization>(user);
			return CreatedAtAction(nameof(Get), new
			{
				id = user.Id
			}, userForSerialization);
		}

		[HttpGet]
		[Authorize(Roles = Role.Administrators)]
		public IActionResult List([FromQuery] Pagination pagination)
		{
			IQueryable<User> queryable = _context.UserManager.Users;
		
			if (pagination != null)
			{
				queryable = queryable.Skip((pagination.Page - 1) * pagination.PageSize)
									.Take(pagination.PageSize);
			}

			IList<UserForList> users = queryable
											.ProjectTo<UserForList>(_mapper.ConfigurationProvider)
											.ToList();
			return Ok(users);
		}

		[HttpGet("{id:guid}")]
		[SwaggerResponse((int)HttpStatusCode.NotFound)]
		[SwaggerResponse((int)HttpStatusCode.Unauthorized)]
		public async Task<IActionResult> Get([FromRoute] Guid id)
		{
			if (id.IsEmpty()) return BadRequest();
			
			string userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
			if (string.IsNullOrEmpty(userId)) return Unauthorized();

			User user = await _context.UserManager.FindByIdAsync(id.ToHexString());
			if (user == null) return NotFound(id);
			
			UserForDetails userForDetails = _mapper.Map<UserForDetails>(user);
			return Ok(userForDetails);
		}

		[HttpGet("{id:guid}/[action]")]
		[SwaggerResponse((int)HttpStatusCode.BadRequest)]
		[SwaggerResponse((int)HttpStatusCode.Unauthorized)]
		[SwaggerResponse((int)HttpStatusCode.NotFound)]
		public async Task<IActionResult> Edit([FromRoute] Guid id)
		{
			if (id.IsEmpty()) return BadRequest();

			string userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
			if (!Guid.TryParse(userId, out Guid uid) || uid != id && !User.IsInRole(Role.Administrators)) return Unauthorized(id);
			
			User user = await _context.UserManager.FindByIdAsync(id.ToHexString());
			if (user == null) return NotFound(id);
			
			UserToUpdate userToUpdate = _mapper.Map<UserToUpdate>(user);
			return Ok(userToUpdate);
		}

		[HttpPut("{id:guid}/[action]")]
		[SwaggerResponse((int)HttpStatusCode.BadRequest)]
		[SwaggerResponse((int)HttpStatusCode.Unauthorized)]
		[SwaggerResponse((int)HttpStatusCode.NotFound)]
		public async Task<IActionResult> Update([FromRoute] Guid id, [FromBody][NotNull] UserToUpdate userToUpdate)
		{
			if (id.IsEmpty()) return BadRequest();
			if (!ModelState.IsValid) return ValidationProblem();

			string userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
			if (!Guid.TryParse(userId, out Guid uid) || uid != id && !User.IsInRole(Role.Administrators)) return Unauthorized(id);

			User user = await _context.UserManager.FindByIdAsync(id.ToHexString());
			if (user == null) return NotFound(id);
			_mapper.Map(userToUpdate, user);

			IdentityResult result = await _context.UserManager.UpdateAsync(user);

			if (!result.Succeeded)
			{
				foreach (IdentityError error in result.Errors)
					ModelState.AddModelError(string.Empty, error.Description);

				return ValidationProblem();
			}

			UserForDetails userForDetails = _mapper.Map<UserForDetails>(user);
			return Ok(userForDetails);
		}

		[HttpDelete("{id:guid}/[action]")]
		[SwaggerResponse((int)HttpStatusCode.BadRequest)]
		[SwaggerResponse((int)HttpStatusCode.Unauthorized)]
		[SwaggerResponse((int)HttpStatusCode.NotFound)]
		public async Task<IActionResult> Delete([FromRoute] Guid id)
		{
			if (id.IsEmpty()) return BadRequest();

			string userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
			if (!Guid.TryParse(userId, out Guid uid) || uid != id && !User.IsInRole(Role.Administrators)) return Unauthorized(id);

			User user = await _context.UserManager.FindByIdAsync(userId);
			if (user == null) return NotFound(id);

			IdentityResult result = await _context.UserManager.DeleteAsync(user);
			if (result.Succeeded) return Ok();

			foreach (IdentityError error in result.Errors)
				ModelState.AddModelError(string.Empty, error.Description);

			return ValidationProblem();
		}

		[AllowAnonymous]
		[HttpPost("[action]")]
		[SwaggerResponse((int)HttpStatusCode.Unauthorized)]
		public async Task<IActionResult> Login([FromBody][NotNull] UserForLogin userForLogin)
		{
			TokenSignInResult result = await _context.SignInAsync(userForLogin.UserName, userForLogin.Password, true);

			if (!result.Succeeded)
			{
				if (result.IsLockedOut) return Unauthorized("Locked out");
				if (result.RequiresTwoFactor) return Unauthorized("Requires two factors");
				return Unauthorized();
			}

			UserForLoginDisplay userForLoginDisplay = _mapper.Map<UserForLoginDisplay>(result.User);
			SetTokenCookie(result.RefreshToken);
			return Ok(new
			{
				token = result.Token,
				user = userForLoginDisplay
			});
		}

		[AllowAnonymous]
		[HttpPost("[action]")]
		public async Task<IActionResult> Logout([FromBody] string revokeToken)
		{
			if (User.Identity is not {IsAuthenticated: true}) return NoContent();
			
			string refreshToken = revokeToken.ToNullIfEmpty() ?? Request.Cookies[REFRESH_TOKEN_NAME];

			if (!string.IsNullOrEmpty(refreshToken))
			{
				await _context.LogoutByTokenAsync(refreshToken);
				SetTokenCookie(null);
				return NoContent();
			}

			if (User.Identity is not {IsAuthenticated: true}) return NoContent();
			
			string userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
			if (string.IsNullOrEmpty(userId)) return NoContent();
			await _context.LogoutAsync(userId);
			SetTokenCookie(null);
			return NoContent();
		}

		[HttpGet("{id:guid}/[action]")]
		[SwaggerResponse((int)HttpStatusCode.BadRequest)]
		[SwaggerResponse((int)HttpStatusCode.NotFound)]
		public async Task<IActionResult> Roles([FromRoute] Guid id)
		{
			if (id.IsEmpty()) return BadRequest();

			User user = await _context.UserManager.FindByIdAsync(id.ToHexString());
			if (user == null) return NotFound(id);
			
			IList<string> roles = await _context.UserManager.GetRolesAsync(user);
			return Ok(roles);
		}

		[HttpGet("{id:guid}/[action]")]
		[SwaggerResponse((int)HttpStatusCode.BadRequest)]
		[SwaggerResponse((int)HttpStatusCode.NotFound)]
		public async Task<IActionResult> Books([FromRoute] Guid id, [FromQuery] Pagination pagination)
		{
			if (id.IsEmpty()) return BadRequest();

			User user = await _context.UserManager.FindByIdAsync(id.ToHexString());
			if (user == null) return NotFound(id);
			
			IQueryable<Book> queryable = _bookService.List();

			if (pagination != null)
			{
				queryable = queryable.Skip((pagination.Page - 1) * pagination.PageSize)
									.Take(pagination.PageSize);
			}

			IList<BookForList> books = queryable
											.ProjectTo<BookForList>(_mapper.ConfigurationProvider)
											.ToList();
			return Ok(books);
		}

		private void SetTokenCookie(string token)
		{
			if (string.IsNullOrEmpty(token))
			{
				Response.Cookies.Delete(REFRESH_TOKEN_NAME);
				return;
			}

			CookieOptions options = new CookieOptions
			{
				HttpOnly = true,
				Secure = true,
				Expires = DateTime.UtcNow.AddMinutes(_context.GetRefreshTokenExpirationTime())
			};
			Response.Cookies.Append(REFRESH_TOKEN_NAME, token, options);
		}
	}
}
