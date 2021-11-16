using AutoMapper;
using MongoPOC.Model.DTO;

namespace MongoPOC.Model
{
	public class AutoMapperProfiles : Profile
	{
		public AutoMapperProfiles()
		{
			AllowNullCollections = true;
			
			CreateMap<Role, RoleForList>();
			
			CreateMap<BookToAdd, Book>().ReverseMap();
			CreateMap<Book, BookForList>();

			CreateMap<UserToRegister, User>().ReverseMap();
			CreateMap<UserToUpdate, User>().ReverseMap();
			CreateMap<User, UserForLoginDisplay>();
			CreateMap<User, UserForList>()
				.IncludeBase<User, UserForLoginDisplay>();
			CreateMap<User, UserForDetails>()
				.IncludeBase<User, UserForList>();
			CreateMap<User, UserForSerialization>()
				.IncludeBase<User, UserForList>();
		}
	}
}