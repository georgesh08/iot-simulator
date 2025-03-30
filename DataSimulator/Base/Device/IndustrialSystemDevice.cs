using System.Security.Cryptography;
using Base.Base;
using Google.Protobuf;
using IoTServer;
using Utils;

namespace Base.Device;

public class IndustrialSystemDevice : ABaseIoTDevice
{
	private readonly Random random = new();
	private readonly IndustrialSystemData systemData;
	
	public IndustrialSystemDevice(string name) : base(name)
	{
		systemData = new IndustrialSystemData
		{
			SystemId = "SYS_" + Guid.NewGuid().ToString("N"),
			SystemName = name
		};
	}

	public override IoTDeviceType DeviceType => IoTDeviceType.INDUSTRIAL_SYSTEM;

	public override DeviceProducedValue GetDeviceProducedValue()
	{
		return new DeviceProducedValue
		{
			IndustrialDeviceValue = new IndustrialSystemData
			{
				SystemName = systemData.SystemName,
				Environment = systemData.Environment,
				ErrorCount = (uint)random.Next(0, 5),
				Timestamp = TimestampConverter.ConvertToTimestamp(DateTime.Now),
				SystemId = systemData.SystemId,
				SystemHealth = systemData.SystemHealth,
				Diagnostics = systemData.Diagnostics,
			}
		};
	}

	protected override void ProduceValue()
	{
		systemData.SystemHealth = new SystemHealth
		{
			CpuLoad = 45.0f + (float)random.NextDouble() * 30,
			MemoryUsage = 60.0f + (float)random.NextDouble() * 20,
			StorageUsage = 55.0f + (float)random.NextDouble() * 15,
			BatteryLevel = 100.0f - (float)random.NextDouble() * 50,
			Uptime = (ulong)TimeSpan.FromDays(random.Next(1, 365)).TotalSeconds
		};
		
		systemData.SystemHealth.StatusFlags.Add("powerState", "ONLINE");
		systemData.SystemHealth.StatusFlags.Add("operationalMode", random.NextDouble() < 0.9 ? "PRODUCTION" : "MAINTENANCE");
		systemData.SystemHealth.StatusFlags.Add("controlSystem", "ACTIVE");
		
		systemData.Diagnostics = new DiagnosticData
		{
			RawDiagnosticDump = ByteString.CopyFrom(GenerateRandomByteArray(4096)),
			Health = systemData.SystemHealth
		};
		
		foreach (var param in new Dictionary<string, string>
		         {
			         { "kernelVersion", "4.19.2-industrial" },
			         { "bootloader", "GRUB 2.04" },
			         { "lastRestart", DateTime.Now.AddDays(-random.Next(1, 30)).ToString("yyyy-MM-dd HH:mm:ss") },
			         { "securityPatchLevel", "2024-03-01" },
			         { "configVersion", "2.6.782" }
		         })
		{
			systemData.Diagnostics.SystemParams.Add(param.Key, param.Value);
		}
		
		for (var i = 0; i < 6; i++)
		{
			systemData.Diagnostics.Logs.Add(GetRandomLogMessage());
			systemData.Diagnostics.Warnings.Add(GenerateRandomAlert());
		}
		
		systemData.Environment = new EnvironmentalReadings
		{
			Temperature = 24.5f + (float)random.NextDouble() * 3,
			Humidity= 45.0f + (float)random.NextDouble() * 10,
			Pressure = 1013.0f + (float)random.NextDouble() * 5,
			LightLevel = 450.0f + (float)random.NextDouble() * 100,
			NoiseLevel = 68.0f + (float)random.NextDouble() * 5,
			Quality = GetRandomQuality()
		};
	}
	
	public Alert GenerateRandomAlert()
	{
		var alertId = $"ALT-{DateTime.Now:yyyyMMdd}-{random.Next(1000, 9999)}";
        
		var alert = new Alert
		{
			AlertId = alertId,
			Level = GetRandomAlertLevel(),
			Source = GetRandomAlertSource(),
			Timestamp = (ulong)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
			Acknowledged = random.NextDouble() < 0.3
		};
		
		alert.Metadata.Add("category", GetRandomAlertCategory());
		alert.Metadata.Add("priority", GetRandomPriority());
		alert.Metadata.Add("system", GetRandomSystem());
		
		return alert;
	}
	
	private string GetRandomAlertSource()
	{
		var sources = new[]
		{
			"SensorSubsystem",
			"TemperatureMonitor",
			"PressureController",
			"FlowRegulator",
			"PowerManagement",
			"NetworkSubsystem",
			"SecurityModule",
			"EnvironmentalMonitor",
			"ProcessController",
			"DataAcquisition",
			"StorageManager",
			"RotationSensor",
			"VibrationAnalyzer",
			"MaintenancePredictor",
			"BackupSystem",
			"FirmwareManager"
		};
        
		return sources[random.Next(sources.Length)];
	}
	
	private AlertLevel GetRandomAlertLevel()
	{
		var levels = new[]
		{
			AlertLevel.None,
			AlertLevel.Info,
			AlertLevel.Warning,
			AlertLevel.Critical,
			AlertLevel.Emergency
		};
        
		var weights = new[] { 0.05, 0.45, 0.30, 0.15, 0.05 };
		var value = random.NextDouble();
		double sum = 0;
        
		for (var i = 0; i < weights.Length; i++)
		{
			sum += weights[i];
			if (value <= sum)
			{
				return levels[i];
			}
		}
        
		return AlertLevel.Info;
	}
	
	private string GetRandomAlertCategory()
	{
		var categories = new[]
		{
			"operational",
			"environmental",
			"security",
			"maintenance",
			"performance",
			"connectivity",
			"hardware",
			"software",
			"configuration",
			"power",
			"process",
			"quality",
			"safety",
			"resource",
			"compliance"
		};
        
		return categories[random.Next(categories.Length)];
	}
	
	private string GetRandomPriority()
	{
		var priorities = new[] { "low", "medium", "high", "urgent" };
		return priorities[random.Next(priorities.Length)];
	}
	
	private string GetRandomSystem()
	{
		var systems = new[]
		{
			"production-line-1",
			"assembly-unit-3",
			"warehouse-zone-b",
			"quality-control-2",
			"packaging-system",
			"distribution-center",
			"utilities-management",
			"facility-monitoring",
			"transport-system",
			"storage-area-a4",
			"loading-dock-2",
			"external-monitoring"
		};
        
		return systems[random.Next(systems.Length)];
	}
	
	public static byte[] GenerateRandomByteArray(int n)
	{
		if (n <= 0)
		{
			throw new ArgumentException();
		}

		var randomBytes = new byte[n];
		using var rng = RandomNumberGenerator.Create();
		rng.GetBytes(randomBytes);
		return randomBytes;
	}
	
	private string GetRandomLogMessage()
	{
		var messages = new[]
		{
			"System startup completed",
			"Sensor readings collected successfully",
			"Process stage completed",
			"Configuration loaded",
			"Connection established",
			"Scheduled maintenance check performed",
			"Parameters adjusted to optimal levels",
			"Data backup completed",
			"Alert threshold updated",
			"Failed to connect to remote service",
			"Sensor X not responding",
			"Unexpected value received from sensor",
			"System temperature exceeding normal range",
			"Process synchronization delayed",
			"Memory usage optimization performed",
			"Control parameter outside of bounds",
			"Error code E-423 received, retrying operation",
			"Network packet loss detected",
			"Database cleanup performed"
		};
		return messages[random.Next(messages.Length)];
	}
	
	private DataQuality GetRandomQuality()
	{
		var messages = new[]
		{
			DataQuality.Average,
			DataQuality.Bad,
			DataQuality.Corrupted,
			DataQuality.Excellent,
			DataQuality.Good,
			DataQuality.Poor
		};
		return messages[random.Next(messages.Length)];
	}
}
