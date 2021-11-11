using System;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using AspNetCore.Identity.MongoDbCore.Models;
using essentialMix.Extensions;
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
		private string _knownAs;
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

		[Required]
		[StringLength(255)]
		public string FirstName
		{
			get => _firstName;
			set => _firstName = value.ToNullIfEmpty();
		}

		[Required]
		[StringLength(255)]
		public string LastName
		{
			get => _lastName; 
			set => _lastName = value.ToNullIfEmpty();
		}

		[StringLength(255)]
		public string KnownAs
		{
			get => _knownAs ?? FirstName; 
			set => _knownAs = value.ToNullIfEmpty();
		}

		public Genders Gender { get; set; }
        public DateTime DateOfBirth { get; set; }
        
        [StringLength(255)]
		public string City { get; set; }
        
        [StringLength(255)]
		public string Country { get; set; }
        public DateTime Created { get; set; }
        public DateTime Modified { get; set; }
        public DateTime LastActive { get; set; }
	}
}