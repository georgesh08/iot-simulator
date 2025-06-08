using IoTServer;

namespace ControllerServer.Caching;

public class CachedDevice
{
	public Guid Id { get; set; }
	public string Name { get; set; }
	public DeviceType Type { get; set; }
	public DateTime CreatedAt { get; set; }
	public DateTime LastSeen { get; set; }
}
