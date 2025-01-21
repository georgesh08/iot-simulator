using IoTServer;

namespace RuleEngine.Processor;

public class DeviceDataProcessor 
{
	public static RuleEngineResult ProcessDeviceData(List<DeviceMessage> data)
	{
		var converted = data.Select(x => ConvertFromByteString(x.Value)).ToList();
		
		var dataType = converted.First().ValueCase;

		var processorContext = GetProcessorContext(dataType);

		return processorContext.ProcessDeviceValues(converted);
	}

	public static RuleEngineResult ProcessDeviceData(DeviceMessage data)
	{
		var value = ConvertFromByteString(data.Value);
		
		var dataType = value.ValueCase;
		
		var processorContext = GetProcessorContext(dataType);

		return processorContext.ProcessDeviceValue(value);
	}

	private static ProcessorContext GetProcessorContext(DeviceProducedValue.ValueOneofCase type)
	{
		var processorContext = new ProcessorContext();

		processorContext.Context = type switch
		{
			DeviceProducedValue.ValueOneofCase.DummyValue => new DummyValueProcessor(),
			DeviceProducedValue.ValueOneofCase.SensorValue => new SensorValueProcessor(),
			_ => processorContext.Context
		};

		return processorContext;
	}

	private static DeviceProducedValue ConvertFromByteString(string data)
	{
		var byteValue = Convert.FromBase64String(data);
		
		using var memoryStream = new MemoryStream(byteValue);
		
		return DeviceProducedValue.Parser.ParseFrom(memoryStream);
	}
}
