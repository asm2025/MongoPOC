using System;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace MongoPOC.Model
{
	[Serializable]
	public class Book : IEntity<Guid>
	{
		[BsonId]
		[BsonRepresentation(BsonType.ObjectId)]
		public Guid Id { get; set; }

		[BsonRequired]
		public string Name { get; set; }
		
		[BsonRequired]
		public string Author { get; set; }
		public string Publisher { get; set; }
		public string EAN { get; set; }
		
		[BsonDateTimeOptions(DateOnly = true)]
		public DateTime Published { get; set; }

		public string Category { get; set; }
		public decimal Price { get; set; }
	}
}