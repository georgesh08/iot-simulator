using DataAccessLayer;
using Google.Protobuf;
using IoTServer;
using MessageQuery;
using MessageQuery.RabbitMQ;
using Serilog;
using Utils;

namespace ControllerServer;

public class DeviceDataController
{
	private RabbitMqPublisher publisher;
	private readonly PeriodicalScheduler reconnectScheduler;
	
	public DeviceDataController(IDatabaseService dbService)
	{
		publisher = new RabbitMqPublisher(dbService);
		reconnectScheduler = new PeriodicalScheduler(SubscribeToQueues, TimeSpan.FromSeconds(3));
		
		reconnectScheduler.Start();
	}

	private void SubscribeToQueues()
	{
		Log.Information("Trying connect to queues");
		publisher.SubscribeToAnalysisResults();

		if (publisher.CanSubscribeToQueues)
		{
			Log.Information("Connected to queues");
			reconnectScheduler.Stop();
		}
	}

	public async Task ProcessMessage(Guid deviceId, DeviceProducedValue message)
	{
		var value = GetDeviceMessage(deviceId, message);

		await publisher.PublishDeviceDataAsync(value);
	}

	private DeviceMessage GetDeviceMessage(Guid deviceId, DeviceProducedValue value)
	{
		var bytes = value.ToByteArray();
		var res = Convert.ToBase64String(bytes);
		return new DeviceMessage
		{
			DeviceId = deviceId.ToString(),
			Value = res,
			Timestamp = TimestampConverter.ConvertToTimestamp(DateTime.UtcNow),
		};
	}
}
