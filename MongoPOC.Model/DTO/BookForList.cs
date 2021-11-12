using System;
using System.Diagnostics;

namespace MongoPOC.Model.DTO
{
	[Serializable]
	[DebuggerDisplay("{Name}")]
	public class BookForList
	{
		public string Id { get; set; }
		public string Name { get; set; }
		public string Author { get; set; }
		public string EAN { get; set; }
	}
}