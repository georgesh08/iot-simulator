using IoTServer;

namespace RuleEngine;

public class DeviceMessage
{
	public string DeviceId { get; set; }
	public string Value { get; set; }
	public ulong Timestamp { get; set; }
}
