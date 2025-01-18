using System.Collections.Concurrent;
using Base.Base;
using Grpc.Net.Client;
using IoTServer;
using Serilog;
using Utils;

namespace SimulatorServer;

public class IoTDeviceService
{
	private readonly ConcurrentDictionary<Guid, ABaseIoTDevice> devices = new();
	
	private IoTServer.IoTDeviceService.IoTDeviceServiceClient client;
	private GrpcChannel channel;
	
	private PeriodicalScheduler dataSenderScheduler;
	private PeriodicalScheduler connectionScheduler;
	
	private const string IoTControllerHost = "0.0.0.0:18686";

	public IoTDeviceService(int period)
	{
		dataSenderScheduler = new PeriodicalScheduler(SendUpdate, TimeSpan.FromSeconds(period));
	}
	
	public void Start(CancellationTokenSource tokenSource)
	{
		dataSenderScheduler.Start();
	}

	public void Stop()
	{
		dataSenderScheduler.Stop();
	}

	private void SendUpdate()
	{
		foreach (var device in devices)
		{
			try
			{
				var response = client.SendDeviceData(new DeviceData
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
				Log.Error(e, "Unexpected error while sending data for device {DeviceId}", device.Key);
			}
		}
	}
}
