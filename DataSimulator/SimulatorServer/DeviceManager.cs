using System.Collections.Concurrent;
using Base.Base;
using Base.Device;
using Utils;

namespace SimulatorServer;

public class DeviceManager
{
	private readonly PeriodicalScheduler dataSenderScheduler;
	
	private readonly ConcurrentDictionary<Guid, ABaseIoTDevice> devices = new();
}
