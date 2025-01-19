using Base.Base;
using Google.Protobuf;
using IoTServer;
using Utils;

namespace Base.Device;

public class SensorDevice : ABaseIoTDevice
{
	private Memory<byte> data;
	private ulong timestamp;
	
	public SensorDevice(string name) : base(name)
	{
		valueProducerScheduler.Interval = TimeSpan.FromSeconds(2);
	}

	public override IoTDeviceType DeviceType => IoTDeviceType.SENSOR;

	public override DeviceProducedValue GetDeviceProducedValue()
	{
		return new DeviceProducedValue
		{
			SensorValue = new SensorDeviceData
			{
				Timestamp = timestamp,
				Data = ByteString.CopyFrom(data.Span),
				ActiveStatus = IsActive
			}
		};
	}

	protected override void ProduceValue()
	{
		var bytesToGenerate = Random.Shared.Next(5, 11);
		var newBytes = new byte[bytesToGenerate];
		Random.Shared.NextBytes(newBytes);

		data = new Memory<byte>(newBytes);
		timestamp = TimestampConverter.ConvertToTimestamp(DateTime.UtcNow);

		if (data.Span[3] + data.Span[5] >= 100)
		{
			Deactivate();
		}

		if (data.Span[4] <= 50)
		{
			Activate();
		}
	}
}
