using Base.Base;

namespace Base.Device;

public class SensorDevice : ABaseIoTDevice<SensorDeviceValue>
{
	public SensorDevice(string name) : base(name)
	{
		valueProducerScheduler.Interval = TimeSpan.FromSeconds(2);
	}

	public override IoTDeviceType DeviceType => IoTDeviceType.SENSOR;
	
	protected override void ProduceValue()
	{
		var bytesToGenerate = Random.Shared.Next(2, 11);
		var newBytes = new byte[bytesToGenerate];
		Random.Shared.NextBytes(newBytes);

		LastProducedValue = new SensorDeviceValue
		{
			Data = newBytes,
			Timestamp = DateTime.UtcNow
		};
	}
}
