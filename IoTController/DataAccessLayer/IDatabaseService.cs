using DataAccessLayer.Models;

namespace DataAccessLayer;

public interface IDatabaseService
{
	Task<DbDevice?> DeviceExistsAsync(string name);
	Task<DbDevice> CreateDeviceAsync(DbDevice device);
	Task<DeviceDataResult> SaveDeviceDataRecordAsync(DeviceDataResult record);
}
