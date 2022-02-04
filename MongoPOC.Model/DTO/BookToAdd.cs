using System;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;

namespace MongoPOC.Model.DTO;

[Serializable]
[DebuggerDisplay("{Name}")]
public class BookToAdd
{
	[Required]
	[StringLength(255)]
	public string Name { get; set; }
	[Required]
	[StringLength(255)]
	public string Author { get; set; }
	[StringLength(255)]
	public string Publisher { get; set; }
	[StringLength(13)]
	public string EAN { get; set; }
	public DateTime Published { get; set; }
	[StringLength(128)]
	public string Category { get; set; }
	public decimal Price { get; set; }
}