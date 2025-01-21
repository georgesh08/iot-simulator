using IoTServer;

namespace RuleEngine.Processor;

public class SensorValueProcessor : IDeviceValueProcessor
{
	public RuleEngineResult ProcessDeviceValue(DeviceProducedValue value)
	{
		var data = value.SensorValue;
		var bytes = data.Data.ToByteArray();
		
		if (!data.ActiveStatus || bytes == null)
		{
			return new RuleEngineResult { EngineVerdict = Status.Error, Message = "Device is not active." };
		}

		if (bytes.Length <= 6)
		{
			return new RuleEngineResult { EngineVerdict = Status.Error, Message = "Invalid bytes length." };
		}
		
		return bytes[0] + bytes[3] < 20 
			? new RuleEngineResult { EngineVerdict = Status.Error, Message = "Invalid checksum." } 
			: new RuleEngineResult { EngineVerdict = Status.Ok, Message = "Device data is valid." };
	}

	public RuleEngineResult ProcessDeviceValues(IEnumerable<DeviceProducedValue> values)
	{
		var checkSum = 0;
		foreach (var value in values)
		{
			var data = value.SensorValue;
			var bytes = data.Data.ToByteArray();
			checkSum += bytes[0];
			checkSum -= bytes[4];
			if (bytes.Length > 8)
			{
				checkSum += checkSum % bytes[8];
			}
		}
		
		return checkSum is <= 0 or > 200_000 
			? new RuleEngineResult { EngineVerdict = Status.Error, Message = "Invalid checksum." } 
			: new RuleEngineResult { EngineVerdict = Status.Ok, Message = "Device data is valid." };
	}
}
