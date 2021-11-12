using JetBrains.Annotations;

namespace MongoPOC.Data.Settings
{
	public class MongoDbConfig : IDbConfig
	{
		public string Host { get; init; }
		public int Port { get; init; }
		public string Database { get; init; }
		[NotNull]
		public string ConnectionString => $"mongodb://{Host}:{Port}";
	}
}