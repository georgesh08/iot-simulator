using IoTServer;

namespace RuleEngine.Processor;

public interface IDeviceValueProcessor
{
	RuleEngineResult ProcessDeviceValue(DeviceProducedValue value);
	RuleEngineResult ProcessDeviceValues(IEnumerable<DeviceProducedValue> values);
}
