using Base.Base;

namespace Base.Device;

public struct DummyDeviceValue : IIoTDeviceValue
{
	public byte ByteValue { get; set; }

	public int IntValue { get; set; }
}
