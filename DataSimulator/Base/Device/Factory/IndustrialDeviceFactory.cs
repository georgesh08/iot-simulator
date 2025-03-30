using Base.Base;

namespace Base.Device.Factory;

public class IndustrialDeviceFactory : AIoTDeviceFactory
{
	public IndustrialDeviceFactory()
	{
		NameGenerator = new DeviceNameGenerator("INDUSTRIAL_SYSTEM");
	}
	public override ABaseIoTDevice CreateDevice()
	{
		var name = NameGenerator.GenerateDeviceName();
		
		return new IndustrialSystemDevice(name);
	}
}
