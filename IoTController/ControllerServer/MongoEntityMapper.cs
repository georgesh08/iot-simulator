using DataAccessLayer.Models;
using IoTServer;
using DeviceType = IoTServer.DeviceType;
using DbDeviceType = DataAccessLayer.Models.DeviceType;

namespace ControllerServer;

public class MongoEntityMapper
{
	public static DbDevice CreateDevice(DeviceRegisterRequest request, Guid id)
	{
		var device = new DbDevice
		{
			Id = id,
			Name = request.Device.Name,
			CreatedAt = DateTime.UtcNow,
			Type = MapDeviceType(request.Device.Type)
		};
		
		return device;
	}

	public static DbDeviceType MapDeviceType(DeviceType type)
	{
		var deviceType = type switch
		{
			DeviceType.Other => DbDeviceType.OTHER,
			DeviceType.Sensor => DbDeviceType.SENSOR,
			_ => DbDeviceType.OTHER
		};

		return deviceType;
	}
}
