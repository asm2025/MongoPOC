using System;
using System.Diagnostics;
using AspNetCore.Identity.MongoDbCore.Models;
using MongoDbGenericRepository.Attributes;

namespace MongoPOC.Model;

[Serializable]
[CollectionName("Roles")]
[DebuggerDisplay("User: {Name}")]
public class Role : MongoIdentityRole<Guid>
{
	public const string Administrators = "Administrators";
	public const string Members = "Members";
			
	public static readonly string[] Roles = 
	{
		Administrators,
		Members
	};
	
	/// <inheritdoc />
	public Role() 
	{
	}

	/// <inheritdoc />
	public Role(string name)
		: base(name)
	{
	}

	/// <inheritdoc />
	public Role(string name, Guid key)
		: base(name, key)
	{
	}
}