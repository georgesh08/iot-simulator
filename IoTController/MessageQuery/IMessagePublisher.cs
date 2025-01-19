namespace MessageQuery;

public interface IMessagePublisher
{
	Task PublishDeviceDataAsync(DeviceMessage message);
}
