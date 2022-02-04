using System;
using System.ComponentModel.DataAnnotations;
using essentialMix.Data.Model;
using MongoDB.Bson.Serialization.Attributes;

namespace MongoPOC.Model;

[Serializable]
[BsonIgnoreExtraElements]
public class UserBook : IEntity
{
	[BsonRequired]
	[Required]
	public Guid UserId { get; set; }

	[BsonRequired]
	[Required]
	public Guid BookId { get; set; }
}