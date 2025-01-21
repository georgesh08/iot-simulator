using DataAccessLayer.Models;
using MongoDB.Driver;

namespace DataAccessLayer.MongoDb;

public class MongoDbService : IDatabaseService
{
	private readonly IMongoCollection<DbDevice> _devices;
	private readonly IMongoCollection<DeviceDataResult> _deviceData;
	
	public async Task<bool> DeviceExistsAsync(string name)
	{
		var device = await _devices.Find(x => x.Name == name).FirstOrDefaultAsync();
		return device != null;
	}

	public Task<DbDevice> CreateDeviceAsync(DbDevice device)
	{
		throw new NotImplementedException();
	}

	public Task<DeviceDataResult> SaveDeviceDataRecordAsync(DeviceDataResult record)
	{
		throw new NotImplementedException();
	}
}
