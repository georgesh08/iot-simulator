using Base.Base;

namespace Base.Device;

public class DummyDevice(string name) : ABaseIoTDevice<DummyDeviceValue>(name)
{
	public override IoTDeviceType DeviceType => IoTDeviceType.OTHER;
	
	protected override void ProduceValue()
	{
		var value = new DummyDeviceValue
		{
			ByteValue = (byte)Random.Shared.Next(byte.MinValue, byte.MaxValue + 1),
			IntValue = Random.Shared.Next(10000)
		};
		
		LastProducedValue = value;
	}
}
