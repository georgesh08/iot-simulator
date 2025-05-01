using System.Collections.Concurrent;
using Base.Base;
using Base.Device;
using Grpc.Core;
using Grpc.Net.Client;
using IoTServer;
using Prometheus;
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

	private readonly string? controllerHost;
	private readonly int controllerPort;
	private readonly Counter generatorRequestsSent;

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
		
		generatorRequestsSent = Metrics.CreateCounter("generator_grpc_requests_total", "Total gRPC requests sent from generator.");
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
				Log.Information("Sending device register request {0}", device.Name);
				
				var request = CreateDeviceRegisterRequest(device);
				var response = client?.RegisterNewDevice(request);
				
				Log.Information("Request sent");

				if (response is { Status: Status.Error })
				{
					Log.Error("Couldn't register device with name {0}", device.Name);
					continue;
				}

				devices.TryAdd(Guid.Parse(response.DeviceId), device);
				devicesToRemove.Add(device);
				
				generatorRequestsSent.Inc();
			}
			catch (Exception e)
			{
				Log.Error("Error during device register request. {0}", e.Message);
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
			Log.Information("Sending device data {0}", device.Key);
			try
			{
				var response = client?.SendDeviceData(new DeviceData
				{
					DeviceId = device.Key.ToString(),
					DeviceValue = device.Value.GetDeviceProducedValue()
				});

				if (response.Status == Status.Error)
				{
					Log.Error("Error sending device data for {0}", device.Key);
				}
				
				generatorRequestsSent.Inc();
			}
			catch (Exception e)
			{
				Log.Error("Unexpected error while sending data for device {0}. {1}", device.Key, e.Message);
			}
		}
	}
}
