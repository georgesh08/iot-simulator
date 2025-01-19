using System.Collections.Concurrent;
using Base.Base;
using Base.Device;
using Grpc.Core;
using Grpc.Net.Client;
using IoTServer;
using Serilog;
using Utils;
using Status = IoTServer.Status;

namespace SimulatorServer;

public class IoTDeviceService
{
	private readonly ConcurrentDictionary<Guid, ABaseIoTDevice> devices = new();
	
	private IoTServer.IoTDeviceService.IoTDeviceServiceClient? client;
	private GrpcChannel? channel;
	
	private PeriodicalScheduler dataSenderScheduler;
	private PeriodicalScheduler connectionScheduler;
	
	private string ioTControllerHost;
	private const int HostPort = 18686;

	public IoTDeviceService(int period, string controllerHost)
	{
		ioTControllerHost = $"http://{controllerHost}:{HostPort}";
		
		connectionScheduler = new PeriodicalScheduler(TryConnect, TimeSpan.FromSeconds(5));
		dataSenderScheduler = new PeriodicalScheduler(SendUpdate, TimeSpan.FromSeconds(period));
	}
	
	public List<ABaseIoTDevice>? DevicesToRegister { get; set; }
	
	public void Start()
	{
		connectionScheduler.Start();
		dataSenderScheduler.Start();
	}

	public void Stop()
	{
		dataSenderScheduler.Stop();
		connectionScheduler.Stop();
	}

	private void TryConnect()
	{
		channel ??= GrpcChannel.ForAddress(ioTControllerHost);

		client ??= new IoTServer.IoTDeviceService.IoTDeviceServiceClient(channel);

		if (channel.State != ConnectivityState.Shutdown && DevicesToRegister?.Count != 0)
		{
			RegisterDevices();
		}
	}

	private void RegisterDevices()
	{
		if (DevicesToRegister == null)
		{
			return;
		}
		
		foreach (var device in DevicesToRegister)
		{
			try
			{
				var request = CreateDeviceRegisterRequest(device);
				var response = client?.RegisterNewDevice(request);

				if (response.Status == Status.Error)
				{
					Log.Error("Couldn't register device with name {0}", device.Name);
					continue;
				}

				devices.TryAdd(Guid.Parse(response.DeviceId), device);
				DevicesToRegister.Remove(device);
			}
			catch (Exception e)
			{
				Log.Error("Error during device register request. {0}", e.Message);
			}
		}
	}

	private DeviceRegisterRequest CreateDeviceRegisterRequest(ABaseIoTDevice device)
	{
		var request = new DeviceRegisterRequest
		{
			Device = new IoTDevice
			{
				Type = MapType(device.DeviceType),
				Name = device.Name
			}
		};
		
		return request;
	}

	private DeviceType MapType(IoTDeviceType type)
	{
		return type switch
		{
			IoTDeviceType.OTHER => DeviceType.Other,
			IoTDeviceType.SENSOR => DeviceType.Sensor,
			_ => DeviceType.Other
		};
	}

	private void SendUpdate()
	{
		foreach (var device in devices)
		{
			try
			{
				var response = client?.SendDeviceData(new DeviceData
				{
					DeviceId = device.Key.ToString(),
					DeviceValue = device.Value.GetDeviceProducedValue()
				});

				if (response.Status == Status.Error)
				{
					Log.Error("Error sending device data for device {0}", device.Key);
				}
			}
			catch (Exception e)
			{
				Log.Error("Unexpected error while sending data for device {0}. {1}", device.Key, e.Message);
			}
		}
	}
}
