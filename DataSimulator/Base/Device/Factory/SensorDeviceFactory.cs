using Base.Base;

namespace Base.Device.Factory;

public class SensorDeviceFactory : AIoTDeviceFactory
{
	public SensorDeviceFactory()
	{
		NameGenerator = new DeviceNameGenerator("SENSOR");
	}
	
	public override SensorDevice CreateDevice()
	{
		var name = NameGenerator.GenerateDeviceName();
		
		return new SensorDevice(name);
	}
}
