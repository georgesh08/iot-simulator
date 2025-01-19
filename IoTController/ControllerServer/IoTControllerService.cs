using Grpc.Core;
using IoTServer;

namespace ControllerServer;

public class IoTControllerService : IoTDeviceService.IoTDeviceServiceBase
{
    public override Task<DeviceRegisterResponse> RegisterNewDevice(DeviceRegisterRequest request, ServerCallContext context)
    {
        return base.RegisterNewDevice(request, context);
    }

    public override Task<DeviceDataResponse> SendDeviceData(DeviceData request, ServerCallContext context)
    {
        return base.SendDeviceData(request, context);
    }
}