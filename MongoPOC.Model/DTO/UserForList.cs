using System;
using System.Diagnostics;

namespace MongoPOC.Model.DTO
{
	[Serializable]
	[DebuggerDisplay("[{KnownAs}] {FirstName} {LastName}")]
	public class UserForList : UserForLoginDisplay
	{
		public string FirstName { get; set; }
		public string LastName { get; set; }
	}
}