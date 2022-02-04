using System;
using System.Collections.Generic;
using System.Diagnostics;
using AspNetCore.Identity.MongoDbCore.Models;

namespace MongoPOC.Model.DTO;

[Serializable]
[DebuggerDisplay("{Name}")]
public class RoleForList
{
	public string Name { get; set; }
}