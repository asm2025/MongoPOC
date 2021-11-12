using System;
using AutoMapper;
using essentialMix.Extensions;
using MongoPOC.Model.DTO;

namespace MongoPOC.Model
{
	public class AutoMapperProfiles : Profile
	{
		public AutoMapperProfiles()
		{
			CreateMap<UserToRegister, User>().ReverseMap();
			CreateMap<UserToUpdate, User>().ReverseMap();
			CreateMap<User, UserForLoginDisplay>()
				.ForMember(e => e.Age, opt => opt.MapFrom(e => DateTime.Today.Years(e.BirthDate)));
			CreateMap<User, UserForList>()
				.IncludeBase<User, UserForLoginDisplay>();
			CreateMap<User, UserForDetails>()
				.IncludeBase<User, UserForList>();
			CreateMap<User, UserForSerialization>()
				.IncludeBase<User, UserForList>();

			CreateMap<Role, RoleForList>();

			CreateMap<BookToAdd, Book>().ReverseMap();
			CreateMap<Book, BookForList>();
		}
	}
}