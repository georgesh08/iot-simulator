using Base.Base;
using Base.Device;
using Base.Device.Factory;
using SimulatorServer;

namespace DataSimulator;

public class DataSimulator
{
	private GrpcSimulatorServer server;
	
	public void LaunchSimulator(int numberOfDevices, int dataSendPeriod, string controllerHost)
	{
		var devices = GenerateData(numberOfDevices);
		server = new GrpcSimulatorServer(devices, dataSendPeriod, controllerHost);
		server.Start();
	}
	
	public async Task Stop()
	{
		await server.StopAsync();
	}

	private List<ABaseIoTDevice> GenerateData(int numberOfDevices)
	{
		var deviceFactory = new DeviceFactory();
		var devices = new List<ABaseIoTDevice>();

		for (var i = 1; i <= numberOfDevices; i++)
		{
			var createdDevice = deviceFactory.CreateDevice(i % 2 == 0 ? IoTDeviceType.SENSOR : IoTDeviceType.OTHER);

			if (createdDevice != null)
			{
				devices.Add(createdDevice);
			} 
		}

		return devices;
	}
}
