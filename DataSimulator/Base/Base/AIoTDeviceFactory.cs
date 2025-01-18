using Base.Device;

namespace Base.Base;

public abstract class AIoTDeviceFactory
{
	protected DeviceNameGenerator NameGenerator;
	
	public abstract ABaseIoTDevice CreateDevice();
}
