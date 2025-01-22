using DataAccessLayer.Models;
using MongoDB.Driver;

namespace DataAccessLayer.MongoDb;

public class MongoDbService : IDatabaseService
{
	private readonly IMongoCollection<DbDevice> _devices;
	private readonly IMongoCollection<DeviceDataResult> _deviceData;

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
		_devices = database.GetCollection<DbDevice>("Devices");
		_deviceData = database.GetCollection<DeviceDataResult>("DeviceData");
	}
	
	public async Task<DbDevice?> DeviceExistsAsync(string name)
	{
		return await _devices.Find(x => x.Name == name).FirstOrDefaultAsync();
	}

	public async Task<DbDevice> CreateDeviceAsync(DbDevice device)
	{
		await _devices.InsertOneAsync(device);
		return device;
	}

	public async Task<DeviceDataResult> SaveDeviceDataRecordAsync(DeviceDataResult result)
	{
		await _deviceData.InsertOneAsync(result);
		return result;
	}
}
