using Base.Base;

namespace Base.Device;

public struct SensorDeviceValue : IIoTDeviceValue
{
	public byte[] Data { get; set; }
	public DateTime Timestamp { get; set; }
}
