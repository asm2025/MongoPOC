using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using essentialMix.Core.Web.Controllers;
using essentialMix.Extensions;
using essentialMix.Patterns.Pagination;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MongoPOC.Data;
using MongoPOC.Model;
using MongoPOC.Model.DTO;
using Swashbuckle.AspNetCore.Annotations;

namespace MongoPOC.API.Controllers;

[Route("[controller]")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class BooksController : ApiController
{
	private readonly BookService _service;
	private readonly IMapper _mapper;

	/// <inheritdoc />
	public BooksController([NotNull] BookService service, [NotNull] IMapper mapper, [NotNull] IConfiguration configuration, [NotNull] ILogger<BooksController> logger)
		: base(configuration, logger)
	{
		_service = service;
		_mapper = mapper;
	}

	[HttpPost("[action]")]
	[SwaggerResponse((int)HttpStatusCode.Created)]
	[Authorize(Roles = Role.Administrators)]
	[ItemNotNull]
	public async Task<IActionResult> Create([FromBody][NotNull] BookToAdd bookToAdd)
	{
		if (!ModelState.IsValid) return ValidationProblem();

		Book book = await _service.AddAsync(_mapper.Map<Book>(bookToAdd));
		BookForList bookForList = _mapper.Map<BookForList>(book);
		return CreatedAtAction(nameof(Get), new
		{
			id = book.Id
		}, bookForList);
	}

	[HttpGet]
	[NotNull]
	public IActionResult List([FromQuery] Pagination pagination)
	{
		IQueryable<Book> queryable = _service.List();

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

	[HttpGet("{id:guid}")]
	[SwaggerResponse((int)HttpStatusCode.NotFound)]
	[ItemNotNull]
	public async Task<IActionResult> Get([FromRoute] Guid id)
	{
		Book book = await _service.GetAsync(id);
		return book == null
					? NotFound(id)
					: Ok(book);
	}

	[HttpGet("{id:guid}/[action]")]
	[SwaggerResponse((int)HttpStatusCode.BadRequest)]
	[SwaggerResponse((int)HttpStatusCode.Unauthorized)]
	[SwaggerResponse((int)HttpStatusCode.NotFound)]
	[Authorize(Roles = Role.Administrators)]
	[ItemNotNull]
	public async Task<IActionResult> Edit([FromRoute] Guid id)
	{
		if (id.IsEmpty()) return BadRequest();

		Book book = await _service.GetAsync(id);
		if (book == null) return NotFound(id);

		BookToAdd bookToAdd = _mapper.Map<BookToAdd>(book);
		return Ok(bookToAdd);
	}

	[HttpPut("{id:guid}/[action]")]
	[SwaggerResponse((int)HttpStatusCode.BadRequest)]
	[SwaggerResponse((int)HttpStatusCode.NotFound)]
	[Authorize(Roles = Role.Administrators)]
	[ItemNotNull]
	public async Task<IActionResult> Update([FromRoute] Guid id, [FromBody][NotNull] BookToAdd bookToAdd)
	{
		if (!ModelState.IsValid) return ValidationProblem();

		Book book = await _service.GetAsync(id);
		if (book == null) return NotFound(id);
		_mapper.Map(bookToAdd, book);
		await _service.UpdateAsync(id, book);
		return Ok(book);
	}

	[HttpDelete("{id:guid}/[action]")]
	[SwaggerResponse((int)HttpStatusCode.BadRequest)]
	[SwaggerResponse((int)HttpStatusCode.NotFound)]
	[Authorize(Roles = Role.Administrators)]
	[ItemNotNull]
	public async Task<IActionResult> Delete([FromRoute] Guid id)
	{
		Book book = await _service.GetAsync(id);
		if (book == null) return NotFound(id);
		await _service.DeleteAsync(book.Id);
		return Ok();
	}
}