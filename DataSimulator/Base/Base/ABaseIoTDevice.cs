using System;
using Base.Device;
using IoTServer;
using Utils;

namespace Base.Base;

public abstract class ABaseIoTDevice
{
	private bool isActive;
	private readonly string name;
	private readonly Guid id;

	protected readonly PeriodicalScheduler valueProducerScheduler;
	
	public ABaseIoTDevice(string name)
	{
		this.name = name;
		id = Guid.NewGuid();
		valueProducerScheduler = new PeriodicalScheduler(ProduceValue, TimeSpan.FromSeconds(1));
	}
	
	public Guid Id => id;
	public string Name => name;
	
	public virtual IoTDeviceType DeviceType { get; }
	
	public bool IsActive => isActive;

	public virtual void Start()
	{
		isActive = true;
		valueProducerScheduler.Start();
	}

	public virtual void Stop()
	{
		isActive = false;
		if (valueProducerScheduler.IsRunning)
		{
			valueProducerScheduler.Stop();
		}
	}

	public abstract DeviceProducedValue GetDeviceProducedValue();
	
	protected abstract void ProduceValue();
}
