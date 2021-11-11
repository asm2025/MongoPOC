using JetBrains.Annotations;

namespace MongoPOC.Data.Settings
{
	public class MongoDbConfig
	{
		public string Name { get; init; }
		public string Host { get; init; }
		public int Port { get; init; }
		[NotNull]
		public string ConnectionString => $"mongodb://{Host}:{Port}";
	}
}