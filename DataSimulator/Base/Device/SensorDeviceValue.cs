using Base.Base;

namespace Base.Device;

public struct SensorDeviceValue : IIoTDeviceValue
{
	public byte[] Data { get; set; }
	public ulong Timestamp { get; set; }
}
