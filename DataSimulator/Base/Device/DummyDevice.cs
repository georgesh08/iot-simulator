using Base.Base;
using IoTServer;

namespace Base.Device;

public class DummyDevice(string name) : ABaseIoTDevice(name)
{
	private byte byteValue;
	private int intValue;
	
	public override IoTDeviceType DeviceType => IoTDeviceType.OTHER;
	
	public override DeviceProducedValue GetDeviceProducedValue()
	{
		return new DeviceProducedValue
		{
			DummyValue = new DummyDeviceData
			{
				Value1 = byteValue,
				Value2 = intValue,
				ActiveStatus = IsActive
			}
		};
	}

	protected override void ProduceValue()
	{
		byteValue = (byte)Random.Shared.Next(byte.MinValue, byte.MaxValue + 1);
		intValue = Random.Shared.Next(10000);
		
		if (intValue % 6 == 0)
		{
			Deactivate();
		}

		if (byteValue % 3 == 0)
		{
			Activate();
		}
	}
}
