using System;
using System.ComponentModel.DataAnnotations;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace MongoPOC.Model
{
	[Serializable]
	public class Book : IEntity<string>
	{
		[BsonId]
		[BsonRepresentation(BsonType.ObjectId)]
		[Key]
		[StringLength(128)]
		public string Id { get; set; }

		[BsonRequired]
		[StringLength(255)]
		public string Name { get; set; }
		
		[BsonRequired]
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
}