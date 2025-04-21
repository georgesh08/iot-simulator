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
	
	private string connectionString;

	private string controllerHost;
	private int controllerPort;

	public IoTDeviceService(int period)
	{
		controllerPort = Environment.GetEnvironmentVariable("CONTROLLER_PORT") != null
			? Convert.ToInt32(Environment.GetEnvironmentVariable("CONTROLLER_PORT"))
			: 18686;
		
		controllerHost = Environment.GetEnvironmentVariable("CONTROLLER_HOST") != null
			? Environment.GetEnvironmentVariable("CONTROLLER_HOST")
			: "localhost";
		
		connectionString = $"http://{controllerHost}:{controllerPort}";
		
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
		channel ??= GrpcChannel.ForAddress(connectionString);

		client ??= new IoTServer.IoTDeviceService.IoTDeviceServiceClient(channel);

		if (channel.State != ConnectivityState.Shutdown && DevicesToRegister?.Count != 0)
		{
			RegisterDevices();
		}
	}

	private void RegisterDevices()
	{
		var devicesToRemove = new List<ABaseIoTDevice>();

		foreach (var device in DevicesToRegister)
		{
			try
			{
				ELKLogger.Information($"Sending device register request {device.Name}");
				
				var request = CreateDeviceRegisterRequest(device);
				var response = client?.RegisterNewDevice(request);
				
				ELKLogger.Information("Request sent");

				if (response is { Status: Status.Error })
				{
					ELKLogger.Error($"Couldn't register device with name {device.Name}");
					continue;
				}

				devices.TryAdd(Guid.Parse(response.DeviceId), device);
				devicesToRemove.Add(device);
			}
			catch (Exception e)
			{
				ELKLogger.Error($"Error during device register request. {e.Message}");
			}
		}

		foreach (var device in devicesToRemove)
		{
			DevicesToRegister.Remove(device);
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
			ELKLogger.Information($"Sending device data {device.Key}");
			try
			{
				var response = client?.SendDeviceData(new DeviceData
				{
					DeviceId = device.Key.ToString(),
					DeviceValue = device.Value.GetDeviceProducedValue()
				});

				if (response is { Status: Status.Error })
				{
					ELKLogger.Error($"Error sending device data for {device.Key}");
				}
			}
			catch (Exception e)
			{
				ELKLogger.Error($"Unexpected error while sending data for device {device.Key}. {e.Message}");
			}
		}
	}
}
