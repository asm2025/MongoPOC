using System;

namespace MongoPOC.Model.DTO;

[Serializable]
public class UserForSerialization : UserForList
{
	public DateTime Modified { get; set; }
}