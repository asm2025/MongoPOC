using System;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using AspNetCore.Identity.MongoDbCore.Models;
using essentialMix.Extensions;
using MongoDB.Bson.Serialization.Attributes;
using MongoDbGenericRepository.Attributes;

namespace MongoPOC.Model
{
	[Serializable]
	[CollectionName("Users")]
	[DebuggerDisplay("User: {UserName}, E-mail:{Email}")]
	public class User : MongoIdentityUser<Guid>
    {
	    public const int AGE_MIN = 0;
	    public const int AGE_MAX = 99;

		private string _firstName;
		private string _name;
		private string _lastName;

		/// <inheritdoc />
		public User() 
		{
		}

		/// <inheritdoc />
		public User(string userName)
			: base(userName)
		{
		}

		/// <inheritdoc />
		public User(string userName, string email)
			: base(userName, email)
		{
		}

		[BsonRequired]
		[Required]
		[StringLength(255)]
		public string FirstName
		{
			get => _firstName;
			set => _firstName = value.ToNullIfEmpty();
		}

		[BsonRequired]
		[Required]
		[StringLength(255)]
		public string LastName
		{
			get => _lastName; 
			set => _lastName = value.ToNullIfEmpty();
		}

		[StringLength(255)]
		public string Name
		{
			get => _name ?? FirstName; 
			set => _name = value.ToNullIfEmpty();
		}

		public Genders Gender { get; set; }
        
		[Required]
		public DateTime BirthDate { get; set; }
        
		[Required]
        [StringLength(255)]
		public string City { get; set; }
        
        [StringLength(255)]
		public string Country { get; set; }
        
		public DateTime UpdatedOn { get; set; }

        public DateTime LastActive { get; set; }
	}
}