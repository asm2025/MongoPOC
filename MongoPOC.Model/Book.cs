using System;
using System.ComponentModel.DataAnnotations;
using MongoDB.Bson.Serialization.Attributes;

namespace MongoPOC.Model;

[Serializable]
[BsonIgnoreExtraElements]
public class Book : IEntity<Guid>
{
	[BsonId]
	[Key]
	public Guid Id { get; set; }

	[BsonRequired]
	[Required]
	[StringLength(255)]
	public string Name { get; set; }
		
	[BsonRequired]
	[Required]
	[StringLength(255)]
	public string Author { get; set; }
		
	[StringLength(255)]
	public string Publisher { get; set; }
		
	[StringLength(13)]
	public string EAN { get; set; }
		
	[BsonDateTimeOptions(DateOnly = true)]
	public DateTime Published { get; set; }

	[StringLength(128)]
	public string Category { get; set; }
		
	public decimal Price { get; set; }
}