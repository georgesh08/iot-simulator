using DataAccessLayer;
using Google.Protobuf;
using IoTServer;
using MessageQuery;
using MessageQuery.RabbitMQ;
using Utils;

namespace ControllerServer;

public class DeviceDataController
{
	private RabbitMqPublisher publisher;
	
	public DeviceDataController(IDatabaseService dbService)
	{
		publisher = new RabbitMqPublisher(dbService);
		publisher.SubscribeToAnalysisResults();
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
