using IoTServer;

namespace MessageQuery;

public class DeviceMessage
{
	public string DeviceId { get; set; }
	public DeviceProducedValue Value { get; set; }
	public ulong Timestamp { get; set; }
}
