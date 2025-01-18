using Base.Base;
using Base.Device;
using Base.Device.Factory;
using SimulatorServer;

namespace DataSimulator;

public class DataSimulator
{
	private GrpcSimulatorServer server;
	
	public DataSimulator()
	{
		server = new GrpcSimulatorServer();
	}
	
	public void LaunchSimulator(int numberOfDevices, int dataSendPeriod)
	{
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
			ABaseIoTDevice? createdDevice;
			createdDevice = deviceFactory.CreateDevice(i % 2 == 0 ? IoTDeviceType.SENSOR : IoTDeviceType.OTHER);

			if (createdDevice != null)
			{
				devices.Add(createdDevice);
			} 
		}

		return devices;
	}
}
