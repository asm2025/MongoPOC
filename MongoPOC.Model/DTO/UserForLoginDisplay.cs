using System;
using System.Diagnostics;

namespace MongoPOC.Model.DTO;

[DebuggerDisplay("{KnownAs}")]
[Serializable]
public class UserForLoginDisplay
{
	public Guid Id { get; set; }
	public string UserName { get; set; }
	public string Email { get; set; }
	public string KnownAs { get; set; }
	public Genders Gender { get; set; }
	public DateTime DateOfBirth { get; set; }
	public string City { get; set; }
	public DateTime Created { get; set; }
	public DateTime LastActive { get; set; }
}