using Base.Base;

namespace Base.Device.Factory;

public class DummyDeviceFactory : AIoTDeviceFactory
{
	public DummyDeviceFactory()
	{
		NameGenerator = new DeviceNameGenerator("DUMMY");
	}
	
	public override DummyDevice CreateDevice()
	{
		var name = NameGenerator.GenerateDeviceName();
		
		return new DummyDevice(name);
	}
}
