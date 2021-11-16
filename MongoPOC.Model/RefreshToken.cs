using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using MongoDB.Bson.Serialization.Attributes;

namespace MongoPOC.Model
{
	[Serializable]
	public class RefreshToken : IEntity<string>
	{
		[BsonId]
		[StringLength(90)]
		public string Id { get; set; }
		[BsonRequired]
		public Guid UserId { get; set; }
		public DateTime Created { get; set; }
		public DateTime Expires { get; set; }

		[BsonIgnore]
		[NotMapped]
		public bool IsExpired => DateTime.UtcNow >= Expires;
	}
}