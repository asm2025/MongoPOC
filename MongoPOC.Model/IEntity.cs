using System;
using essentialMix.Data.Model;

namespace MongoPOC.Model;

public interface IEntity<T> : IEntity
	where T : IComparable<T>, IEquatable<T>
{
	T Id { get; set; }
}