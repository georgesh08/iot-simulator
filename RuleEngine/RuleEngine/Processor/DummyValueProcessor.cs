using IoTServer;

namespace RuleEngine.Processor;

public class DummyValueProcessor : IDeviceValueProcessor
{
	public RuleEngineResult ProcessDeviceValue(DeviceProducedValue value)
	{
		var data = value.DummyValue;
		if (!data.ActiveStatus)
		{
			return new RuleEngineResult { EngineVerdict = Status.Error, Message = "Device is not active." };
		}

		if (data is { Value1: > 100, Value2: < 1024 })
		{
			return new RuleEngineResult { EngineVerdict = Status.Ok, Message = "Device data is valid." };
		}

		if (data.Value2 > 16544)
		{
			return new RuleEngineResult { EngineVerdict = Status.Error, Message = "Device data out of bounds." };
		}

		return data.Value1 + data.Value2 < 4000 
			? new RuleEngineResult { EngineVerdict = Status.Error, Message = "Invalid checksum." } 
			: new RuleEngineResult { EngineVerdict = Status.Ok, Message = "Device data is valid." };
	}

	public RuleEngineResult ProcessDeviceValues(IEnumerable<DeviceProducedValue> values)
	{
		var checkSum = 0;
		foreach (var value in values)
		{
			var data = value.DummyValue;
			checkSum -= data.Value1;
			checkSum += data.Value2;
		}

		return checkSum is <= 0 or > 100_000 
			? new RuleEngineResult { EngineVerdict = Status.Error, Message = "Invalid checksum." } 
			: new RuleEngineResult { EngineVerdict = Status.Ok, Message = "Device data is valid." };
	}
}
