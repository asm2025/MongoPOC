using System;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;

namespace MongoPOC.Model.DTO;

[Serializable]
[DebuggerDisplay("{UserName}, {Email}, {FirstName} {LastName}")]
public class UserToRegister
{
	[Required]
	[StringLength(128)]
	public string UserName { get; set; }

	[Required]
	[StringLength(32, MinimumLength = 6)]
	public string Password { get; set; }

	[Required]
	[EmailAddress]
	[StringLength(255)]
	public string Email { get; set; }

	[Required]
	[StringLength(255)]
	public string FirstName { get; set; }

	[StringLength(255)]
	public string LastName { get; set; }

	[StringLength(255)]
	public string KnownAs { get; set; }

	public Genders Gender { get; set; }

	public DateTime DateOfBirth { get; set; }

	[Required]
	[StringLength(255)]
	public string City { get; set; }

	[Required]
	[StringLength(255)]
	public string Country { get; set; }
}