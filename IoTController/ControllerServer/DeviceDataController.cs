using IoTServer;
using MessageQuery;
using MessageQuery.RabbitMQ;
using Utils;

namespace ControllerServer;

public class DeviceDataController
{
	private RabbitMqPublisher publisher;
	
	public DeviceDataController()
	{
		publisher = new RabbitMqPublisher();
		publisher.SubscribeToAnalysisResults();
	}

	public async Task ProcessMessage(Guid deviceId, DeviceProducedValue message)
	{
		var value = GetDeviceMessage(deviceId, message);

		await publisher.PublishDeviceDataAsync(value);
	}

	private DeviceMessage GetDeviceMessage(Guid deviceId, DeviceProducedValue value)
	{
		return new DeviceMessage
		{
			DeviceId = deviceId.ToString(),
			Value = value,
			Timestamp = TimestampConverter.ConvertToTimestamp(DateTime.UtcNow),
		};
	}
}
