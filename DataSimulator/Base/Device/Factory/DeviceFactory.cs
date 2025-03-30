using Base.Base;

namespace Base.Device.Factory;

public class DeviceFactory
{
	private readonly Dictionary<IoTDeviceType, AIoTDeviceFactory> strategies = new()
	{
		{ IoTDeviceType.SENSOR, new SensorDeviceFactory() },
		{ IoTDeviceType.OTHER, new DummyDeviceFactory() },
		{ IoTDeviceType.INDUSTRIAL_SYSTEM, new IndustrialDeviceFactory() }
	};

	public ABaseIoTDevice? CreateDevice(IoTDeviceType deviceType)
	{
		return strategies.TryGetValue(deviceType, out var creationStrategy) 
			? creationStrategy.CreateDevice() 
			: null;
	} 
}
