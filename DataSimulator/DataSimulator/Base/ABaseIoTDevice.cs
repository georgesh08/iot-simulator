using DataSimulator.Device;
using Utils;

namespace DataSimulator.Base;

public abstract class ABaseIoTDevice<T> where T : struct
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

	public T ProducedValue { get; }

	public Guid Id => id;
	public string Name => name;
	
	public T LastProducedValue { get; protected set; }
	
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
	protected abstract void ProduceValue();
}
