using IoTServer;

namespace RuleEngine;

public class DeviceMessage
{
	public string DeviceId { get; set; }
	public DeviceProducedValue Value { get; set; }
	public ulong Timestamp { get; set; }
}
