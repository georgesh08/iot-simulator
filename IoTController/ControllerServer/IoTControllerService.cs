using System.Collections.Concurrent;
using DataAccessLayer;
using DataAccessLayer.MongoDb;
using Grpc.Core;
using IoTServer;
using Serilog;
using Status = IoTServer.Status;

namespace ControllerServer;

public class IoTControllerService : IoTDeviceService.IoTDeviceServiceBase
{
	private readonly DeviceDataController dataController;
	private readonly IDatabaseService dbService;
	private readonly ConcurrentBag<Guid> deviceIds;

	public IoTControllerService()
	{
		dbService = new MongoDbService();
		dataController = new DeviceDataController(dbService);
		deviceIds = [];
	}
	
    public override async Task<DeviceRegisterResponse> RegisterNewDevice(DeviceRegisterRequest request, ServerCallContext context)
    {
	    var deviceName = request.Device.Name;
	    
	    Log.Information("Received register request for device {0} ({1}) from host: {2}",
		    deviceName, request.Device.Type.ToString(),
		    context.Host);

	    var device = await dbService.DeviceExistsAsync(deviceName);

	    if (device != null)
	    {
		    deviceIds.Add(device.Id);
		    Log.Information("Device {0} already exists ", device.Name);
		    return new DeviceRegisterResponse { DeviceId = device.Id.ToString(), Status = Status.Ok };
	    }
	    
	    var newDeviceId = Guid.NewGuid();

	    await dbService.CreateDeviceAsync(MongoEntityMapper.CreateDevice(request, newDeviceId));
	    
	    deviceIds.Add(newDeviceId);
	    
	    Log.Information("Registered new device. Device with name {0} now has server id {1}", 
		    deviceName, newDeviceId);
	    
        return new DeviceRegisterResponse { DeviceId = newDeviceId.ToString(), Status = Status.Ok};
    }

    public override async Task<DeviceDataResponse> SendDeviceData(DeviceData request, ServerCallContext context)
    {
	    Log.Information("Received device data for {0}", request.DeviceId);

	    var id = Guid.Parse(request.DeviceId);
	    await dataController.ProcessMessage(id, request.DeviceValue);
	    
	    return new DeviceDataResponse { Status = Status.Ok };
    }
}
