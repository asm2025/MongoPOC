using AutoMapper;
using JetBrains.Annotations;
using MongoPOC.Data.Fakers;
using MongoPOC.Model;
using MongoPOC.Model.DTO;
using Swashbuckle.AspNetCore.Filters;

namespace MongoPOC.Data.Swagger.Examples;

public class UserToUpdateExample : IExamplesProvider<UserToUpdate>
{
	private readonly UserFaker _faker;
	private readonly IMapper _mapper;

	public UserToUpdateExample([NotNull] IMapper mapper)
	{
		_mapper = mapper;
		_faker = new UserFaker();
	}

	/// <inheritdoc />
	public UserToUpdate GetExamples()
	{
		User user = _faker.Generate();
		return _mapper.Map<UserToUpdate>(user);
	}
}