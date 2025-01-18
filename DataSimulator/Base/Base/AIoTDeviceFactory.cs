using Base.Device;

namespace Base.Base;

public abstract class AIoTDeviceFactory<TDevice, TValue> 
	where TDevice : ABaseIoTDevice<TValue>
	where TValue : IIoTDeviceValue
{
	protected DeviceNameGenerator NameGenerator;
	
	public abstract TDevice CreateDevice();
}
