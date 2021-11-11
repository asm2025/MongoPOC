using System;
using System.ComponentModel.DataAnnotations;
using essentialMix.ComponentModel.DataAnnotations;

namespace MongoPOC.Model.DTO
{
	[Serializable]
	public class UserForLogin
	{
		[Required]
		[UserName]
		[StringLength(128)]
		public string UserName { get; set; }

		[Required]
		[StringLength(32, MinimumLength = 6)]
		public string Password { get; set; }
	}
}