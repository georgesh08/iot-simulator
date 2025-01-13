using DataSimulator.Base;

namespace DataSimulator.Device;

public class DummyDevice(string name) : ABaseIoTDevice<DummyDeviceValue>(name)
{
	public override IoTDeviceType DeviceType => IoTDeviceType.OTHER;
	
	protected override void ProduceValue()
	{
		var value = new DummyDeviceValue
		{
			ByteValue = (byte)Random.Shared.Next(byte.MinValue, byte.MaxValue + 1),
			LongValue = Random.Shared.NextInt64(10000)
		};
		
		LastProducedValue = value;
	}
}
