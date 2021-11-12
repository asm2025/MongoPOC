namespace MongoPOC.Data.Settings
{
	public interface IDbConfig
	{
		string Host { get; init; }
		int Port { get; init; }
		string Database { get; init; }
		string ConnectionString { get; }
	}
}