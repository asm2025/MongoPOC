using System;
using System.Threading;
using Bogus;
using essentialMix.Helpers;
using MongoPOC.Model;

namespace MongoPOC.Data.Fakers
{
	public class BookFaker : Faker<Book>
	{
		private static readonly Lazy<string[]> __categories = new Lazy<string[]>(() =>
		{
			Faker faker = new Faker();
			return faker.Commerce.Categories(RNGRandomHelper.Next(4, 16));
		}, LazyThreadSafetyMode.PublicationOnly);

		/// <inheritdoc />
		public BookFaker()
		{
			base.RuleFor(e => e.Id, f => f.Random.Guid());
			base.RuleFor(e => e.Name, f => f.Lorem.Sentence(1, 6));
			base.RuleFor(e => e.Author, f => f.Person.FullName);
			base.RuleFor(e => e.Publisher, f => f.Company.CompanyName());
			base.RuleFor(e => e.EAN, f => f.Commerce.Ean13());
			base.RuleFor(e => e.Published, f => f.Date.Past(RNGRandomHelper.Next(0, 60), DateTime.Today).Date);
			base.RuleFor(e => e.Category, f => f.PickRandom(__categories.Value));
			base.RuleFor(e => e.Price, f => f.Random.Decimal(1.0m, 1000.0m));
		}
	}
}