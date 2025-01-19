using Grpc.Core;
using IoTServer;
using Serilog;
using Status = IoTServer.Status;

namespace ControllerServer;

public class IoTControllerService : IoTDeviceService.IoTDeviceServiceBase
{
    public override Task<DeviceRegisterResponse> RegisterNewDevice(DeviceRegisterRequest request, ServerCallContext context)
    {
	    var deviceName = request.Device.Name;
	    
	    Log.Information("Received register request for device {0} ({1}) from host: {2}",
		    deviceName, request.Device.Type.ToString(), context.Host);
	    var newDeviceId = Guid.NewGuid().ToString();
	    
	    Log.Information("Registered new device. Device with name {0} now has server id {1}", 
		    deviceName, newDeviceId);
	    
        return Task.FromResult(new DeviceRegisterResponse { DeviceId = newDeviceId, Status = Status.Ok});
    }

    public override Task<DeviceDataResponse> SendDeviceData(DeviceData request, ServerCallContext context)
    {
	    Log.Information("Received device data for {0}", request.DeviceId);
	    return Task.FromResult(new DeviceDataResponse { Status = Status.Ok });
    }
}
