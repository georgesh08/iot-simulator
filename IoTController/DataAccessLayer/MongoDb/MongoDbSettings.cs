namespace DataAccessLayer.MongoDb;

public class MongoDbSettings
{
	public string Host { get; set; }
	public int Port { get; set; }
	public string Database { get; set; }
	public string User { get; set; }
	public string Password { get; set; }

	public string ConnectionString => string.IsNullOrEmpty(User) 
		? $"mongodb://{Host}:{Port}"
		: $"mongodb://{User}:{Password}@{Host}:{Port}";
}
