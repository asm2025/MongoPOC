using System;
using AutoMapper;
using essentialMix.Data.Patterns.Parameters;
using essentialMix.Extensions;
using essentialMix.Patterns.Pagination;
using MongoPOC.Model.DTO;

namespace MongoPOC.Model
{
	public class AutoMapperProfiles : Profile
	{
		public AutoMapperProfiles()
		{
			CreateMap<SortablePagination, ListSettings>().ReverseMap();
			CreateMap<UserList, ListSettings>().ReverseMap();

			CreateMap<UserToRegister, User>().ReverseMap();
			CreateMap<UserToUpdate, User>().ReverseMap();
			CreateMap<User, UserForLoginDisplay>()
				.ForMember(e => e.Age, opt => opt.MapFrom(e => DateTime.Today.Years(e.DateOfBirth)));
			CreateMap<User, UserForList>()
				.IncludeBase<User, UserForLoginDisplay>();
			CreateMap<User, UserForDetails>()
				.IncludeBase<User, UserForList>();
			CreateMap<User, UserForSerialization>()
				.IncludeBase<User, UserForList>();

			CreateMap<Role, RoleForSerialization>();
		}
	}
}