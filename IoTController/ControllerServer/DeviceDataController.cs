using IoTServer;
using MessageQuery;
using MessageQuery.RabbitMQ;
using Utils;

namespace ControllerServer;

public class DeviceDataController
{
	private RabbitMQPublisher publisher;
	
	public DeviceDataController()
	{
		publisher = new RabbitMQPublisher();
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
