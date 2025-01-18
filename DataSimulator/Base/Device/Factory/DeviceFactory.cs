using Base.Base;

namespace Base.Device.Factory;

public class DeviceFactory
{
	private readonly Dictionary<IoTDeviceType, AIoTDeviceFactory> strategies;

	public DeviceFactory()
	{
		strategies = new Dictionary<IoTDeviceType, AIoTDeviceFactory>
		{
			{ IoTDeviceType.SENSOR, new SensorDeviceFactory() },
			{ IoTDeviceType.OTHER, new DummyDeviceFactory() }
		};
	}

	public ABaseIoTDevice? CreateDevice(IoTDeviceType deviceType)
	{
		return strategies.TryGetValue(deviceType, out var creationStrategy) 
			? creationStrategy.CreateDevice() 
			: null;
	} 
}
