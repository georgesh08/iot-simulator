using IoTServer;

namespace RuleEngine.Processor;

public class ProcessorContext : IDeviceValueProcessor
{
	public IDeviceValueProcessor Context {get; set;}
	
	public RuleEngineResult ProcessDeviceValue(DeviceProducedValue value)
	{
		return Context.ProcessDeviceValue(value);
	}

	public RuleEngineResult ProcessDeviceValues(IEnumerable<DeviceProducedValue> values)
	{
		return Context.ProcessDeviceValues(values);
	}
}
