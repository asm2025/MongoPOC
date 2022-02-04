using AutoMapper;
using JetBrains.Annotations;
using MongoPOC.Data.Fakers;
using MongoPOC.Model;
using MongoPOC.Model.DTO;
using Swashbuckle.AspNetCore.Filters;

namespace MongoPOC.Data.Swagger.Examples;

public class UserToRegisterExample : IExamplesProvider<UserToRegister>
{
	private readonly UserFaker _faker;
	private readonly IMapper _mapper;

	public UserToRegisterExample([NotNull] IMapper mapper)
	{
		_mapper = mapper;
		_faker = new UserFaker();
	}

	/// <inheritdoc />
	public UserToRegister GetExamples()
	{
		User user = _faker.Generate();
		return _mapper.Map<UserToRegister>(user);
	}
}