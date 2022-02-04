using System;
using System.ComponentModel.DataAnnotations;

namespace MongoPOC.Model.DTO;

[Serializable]
public class UserForLogin
{
	[Required]
	[StringLength(128)]
	public string UserName { get; set; }

	[Required]
	[StringLength(32, MinimumLength = 6)]
	public string Password { get; set; }
}