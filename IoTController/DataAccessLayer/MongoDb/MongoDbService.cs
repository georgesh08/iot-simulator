using DataAccessLayer.Models;
using MongoDB.Driver;
using Prometheus;

namespace DataAccessLayer.MongoDb;

public class MongoDbService : IDatabaseService
{
	private readonly IMongoCollection<DbDevice> devices;
	private readonly IMongoCollection<DeviceDataResult> deviceData;

	private readonly Counter dbQueriesCounter;

	public MongoDbService()
	{
		var settings = new MongoDbSettings
		{
			Host = Environment.GetEnvironmentVariable("MONGODB_HOST") ?? "localhost",
			Port = int.Parse(Environment.GetEnvironmentVariable("MONGODB_PORT") ?? "27017"),
			Database = Environment.GetEnvironmentVariable("MONGODB_DATABASE") ?? "DevicesDb",
			User = Environment.GetEnvironmentVariable("MONGODB_USER"),
			Password = Environment.GetEnvironmentVariable("MONGODB_PASSWORD")
		};
		
		var client = new MongoClient(settings.ConnectionString);
		var database = client.GetDatabase(settings.Database);
		devices = database.GetCollection<DbDevice>("Devices");
		deviceData = database.GetCollection<DeviceDataResult>("DeviceData");
		
		dbQueriesCounter = Metrics
			.CreateCounter("mongo_queries_total", "Total queries sent sent to MongoDB.");
	}
	
	public async Task<DbDevice?> DeviceExistsAsync(string name)
	{
		var res = await devices.Find(x => x.Name == name).FirstOrDefaultAsync();
		dbQueriesCounter.Inc();
		return res;
	}

	public async Task<DbDevice> CreateDeviceAsync(DbDevice device)
	{
		await devices.InsertOneAsync(device);
		dbQueriesCounter.Inc();
		return device;
	}

	public async Task<DeviceDataResult> SaveDeviceDataRecordAsync(DeviceDataResult result)
	{
		await deviceData.InsertOneAsync(result);
		dbQueriesCounter.Inc();
		return result;
	}
}
