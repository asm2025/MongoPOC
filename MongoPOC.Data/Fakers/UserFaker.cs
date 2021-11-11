using System;
using System.Collections.Generic;
using Bogus;
using Bogus.DataSets;
using essentialMix.Helpers;
using JetBrains.Annotations;
using MongoPOC.Model;

namespace MongoPOC.Data.Fakers
{
	public class UserFaker : Faker<User>
	{
		/// <inheritdoc />
		public UserFaker()
		{
			base.RuleFor(e => e.Id, f => f.Random.Guid());
			base.RuleFor(e => e.Gender, () => (Genders)RNGRandomHelper.Next(1, 2));
			base.RuleFor(e => e.FirstName, (f, e) =>
			{
				return e.Gender switch
				{
					Genders.Male => f.Name.FirstName(Name.Gender.Male),
					Genders.Female => f.Name.FirstName(Name.Gender.Female),
					_ => f.Name.FirstName()
				};
			});
			base.RuleFor(e => e.LastName, f => f.Person.LastName);
			base.RuleFor(e => e.Email, (f, e) => f.Internet.ExampleEmail(e.FirstName, e.LastName));
			base.RuleFor(e => e.KnownAs, (_, e) => e.FirstName);
			base.RuleFor(e => e.UserName, (f, e) => f.Internet.UserName(e.FirstName, e.LastName));
			base.RuleFor(e => e.City, f => f.Address.City());
			base.RuleFor(e => e.Country, f => f.Address.Country());
			base.RuleFor(e => e.Created, f => f.Date.Past(RandomHelper.Next(1, 10)));
			base.RuleFor(e => e.DateOfBirth, f => f.Date.Past(RandomHelper.Next(16, 60), DateTime.Now.AddYears(-18)));
			base.RuleFor(e => e.LastActive, f => f.Date.Past());
		}

		public int MaxSpecifiedGender { get; set; }

		/// <inheritdoc />
		[NotNull]
		public override List<User> Generate(int count, string ruleSets = null)
		{
			List<User> users = base.Generate(count, ruleSets);
			int maxGender = MaxSpecifiedGender;
			if (maxGender <= 0 || users.Count <= maxGender) return users;
	
			int males = 0;
			int females = 0;

			foreach (User user in users)
			{
				switch (user.Gender)
				{
					case Genders.Male:
						if (males == maxGender) user.Gender = Genders.Unspecified;
						else males++;
						break;
					case Genders.Female:
						if (females == maxGender) user.Gender = Genders.Unspecified;
						else females++;
						break;
				}
			}

			return users;
		}
	}
}