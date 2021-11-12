using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AutoMapper;
using essentialMix.Core.Web.Controllers;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using MongoPOC.Data;
using MongoPOC.Model;
using MongoPOC.Model.DTO;
using MongoPOC.Model.Extensions;

namespace MongoPOC.API.Controllers
{
	[Authorize(AuthenticationSchemes = Constants.Authentication.AuthenticationSchemes)]
	[Route("[controller]")]
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
		public async Task<IActionResult> Create([FromBody][NotNull] BookToAdd bookToAdd)
		{
			if (!User.IsInRole(Role.Administrators)) return Unauthorized();
			if (!ModelState.IsValid) return ValidationProblem();

			Book book = await _service.AddAsync(_mapper.Map<Book>(bookToAdd));
			BookForList bookForList = _mapper.Map<BookForList>(book);
			return CreatedAtAction(nameof(Get), new
			{
				id = book.Id
			}, bookForList);
		}

		[HttpGet]
		public async IAsyncEnumerable<BookForList> Get()
		{
			IAsyncCursor<Book> cursor = await _service.GetAsync();
			if (cursor == null) yield break;

			await foreach (BookForList book in cursor.AsAsyncEnumerable(e => _mapper.Map<BookForList>(e)))
			{
				yield return book;
			}
		}

		[HttpGet("{id:guid}")]
		public async Task<IActionResult> Get([FromRoute] Guid id)
		{
			Book book = await _service.GetAsync(id);
			return book == null
						? NotFound(id)
						: Ok(book);
		}

		[HttpPut("[action]/{id:guid}")]
		public async Task<IActionResult> Update([FromRoute] Guid id, [FromBody][NotNull] BookToAdd bookToAdd)
		{
			if (!ModelState.IsValid) return ValidationProblem();

			Book book = await _service.GetAsync(id);
			if (book == null) return NotFound(id);
			_mapper.Map(bookToAdd, book);
			await _service.UpdateAsync(id, book);
			return Ok(book);
		}

		[HttpDelete("[action]/{id:guid}")]
		public async Task<IActionResult> Delete([FromRoute] Guid id)
		{
			Book book = await _service.GetAsync(id);
			if (book == null) return NotFound(id);
			await _service.DeleteAsync(book.Id);
			return Ok();
		}
	}
}
