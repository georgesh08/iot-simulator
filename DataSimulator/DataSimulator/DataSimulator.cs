﻿using Base.Base;
using Base.Device;
using Base.Device.Factory;
using SimulatorServer;

namespace DataSimulator;

public class DataSimulator
{
	private GrpcSimulatorServer server;
	
	public void LaunchSimulator(int numberOfDevices, int dataSendPeriod)
	{
		var devices = GenerateData(numberOfDevices);
		server = new GrpcSimulatorServer(devices, dataSendPeriod);
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
			
			var createdDevice = deviceFactory.CreateDevice(i % 2 == 0 ? IoTDeviceType.SENSOR 
				: (i % 3 == 0 ? IoTDeviceType.OTHER : IoTDeviceType.INDUSTRIAL_SYSTEM));

			if (createdDevice != null)
			{
				devices.Add(createdDevice);
				createdDevice.Start();
			} 
		}

		return devices;
	}
}
