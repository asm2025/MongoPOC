using AutoMapper;
using JetBrains.Annotations;
using MongoPOC.Data.Fakers;
using MongoPOC.Model;
using MongoPOC.Model.DTO;
using Swashbuckle.AspNetCore.Filters;

namespace MongoPOC.Data.Swagger.Examples
{
	public class BookToAddExample : IExamplesProvider<BookToAdd>
	{
		private readonly BookFaker _faker;
		private readonly IMapper _mapper;

		public BookToAddExample([NotNull] IMapper mapper)
		{
			_mapper = mapper;
			_faker = new BookFaker();
		}

		/// <inheritdoc />
		public BookToAdd GetExamples()
		{
			Book book = _faker.Generate();
			return _mapper.Map<BookToAdd>(book);
		}
	}
}