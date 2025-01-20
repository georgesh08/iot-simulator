using IoTServer;

namespace RuleEngine;

public class DeviceDataProcessor
{
	public static RuleEngineResult ProcessDeviceData(List<DeviceMessage> data)
	{
		var converted = data.Select(x => ConvertFromByteString(x.Value)).ToList();
		
		var dataType = converted.First().ValueCase;

		return dataType switch
		{
			DeviceProducedValue.ValueOneofCase.DummyValue => ProcessDummyValues(converted),
			DeviceProducedValue.ValueOneofCase.SensorValue => ProcessSensorValues(converted),
			_ => new RuleEngineResult { EngineVerdict = Status.Error, Message = "Unknown device type." }
		};
	}

	public static RuleEngineResult ProcessDeviceData(DeviceMessage data)
	{
		var value = ConvertFromByteString(data.Value);
		
		var dataType = value.ValueCase;
		
		return dataType switch
		{
			DeviceProducedValue.ValueOneofCase.DummyValue => ProcessDummyValue(value),
			DeviceProducedValue.ValueOneofCase.SensorValue => ProcessSensorValue(value),
			_ => new RuleEngineResult { EngineVerdict = Status.Error, Message = "Unknown device type." }
		};
	}

	private static RuleEngineResult ProcessDummyValue(DeviceProducedValue value)
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

		return data.Value1 + data.Value2 < 20533 
			? new RuleEngineResult { EngineVerdict = Status.Error, Message = "Invalid checksum" } 
			: new RuleEngineResult { EngineVerdict = Status.Ok, Message = "Device data is valid." };
	}
	
	private static RuleEngineResult ProcessSensorValue(DeviceProducedValue value)
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

	private static RuleEngineResult ProcessDummyValues(IEnumerable<DeviceProducedValue> values)
	{
		var checkSum = 0;
		foreach (var value in values)
		{
			var data = value.DummyValue;
			checkSum += data.Value1;
			checkSum -= data.Value2;
		}

		return checkSum is <= 0 or > 100_000 
			? new RuleEngineResult { EngineVerdict = Status.Error, Message = "Invalid checksum." } 
			: new RuleEngineResult { EngineVerdict = Status.Ok, Message = "Device data is valid." };
	}

	private static RuleEngineResult ProcessSensorValues(IEnumerable<DeviceProducedValue> values)
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

	private static DeviceProducedValue ConvertFromByteString(string data)
	{
		var byteValue = Convert.FromBase64String(data);
		
		using var memoryStream = new MemoryStream(byteValue);
		
		return DeviceProducedValue.Parser.ParseFrom(memoryStream);
	}
}
