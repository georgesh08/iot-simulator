using System.Collections.Concurrent;
using ControllerServer.Caching;
using ControllerServer.Caching.Redis;
using DataAccessLayer;
using DataAccessLayer.Models;
using DataAccessLayer.MongoDb;
using Grpc.Core;
using IoTServer;
using Serilog;
using StackExchange.Redis;
using Status = IoTServer.Status;

namespace ControllerServer;

public class IoTControllerService : IoTDeviceService.IoTDeviceServiceBase
{
	private readonly RedisCache cache;
	private readonly DeviceDataController dataController;
	private readonly IDatabaseService dbService;
	private readonly ConcurrentBag<Guid> deviceIds;

	public IoTControllerService()
	{
		var redisConnection = Environment.GetEnvironmentVariable("REDIS_CONNECTION") ?? "localhost:6379";
		var connectionMultiplexer = ConnectionMultiplexer.Connect(redisConnection);

		cache = new RedisCache(connectionMultiplexer);
		dbService = new MongoDbService();
		dataController = new DeviceDataController(dbService);
		deviceIds = [];

		LoadDeviceIdsFromCacheAsync().ConfigureAwait(false);
	}
	
    public override async Task<DeviceRegisterResponse> RegisterNewDevice(DeviceRegisterRequest request, ServerCallContext context)
    {
	    var deviceName = request.Device.Name;

	    Log.Information("Received register request for device {0} ({1}) from host: {2}",
		    deviceName, request.Device.Type.ToString(),
		    context.Host);
	    
	    var cacheKey = $"device:name:{deviceName}";
	    var cachedDevice = await cache.GetAsync<CachedDevice>(cacheKey);
	    
	    if (cachedDevice != null)
	    {
		    cachedDevice.LastSeen = DateTime.UtcNow;
		    await cache.SetAsync(cacheKey, cachedDevice, TimeSpan.FromHours(24));
            
		    if (!deviceIds.Contains(cachedDevice.Id))
		    {
			    deviceIds.Add(cachedDevice.Id);
			    await CacheDeviceIdsAsync();
		    }
            
		    Log.Information("Device '{DeviceName}' found in cache", deviceName);
		    return new DeviceRegisterResponse 
		    { 
			    DeviceId = cachedDevice.Id.ToString(), 
			    Status = Status.Ok
		    };
	    }

	    var device = await dbService.DeviceExistsAsync(deviceName);

	    if (device != null)
	    {
		    await cache.SetAsync(cacheKey, device, TimeSpan.FromHours(24));
		    
		    if (!deviceIds.Contains(device.Id))
		    {
			    deviceIds.Add(device.Id);
			    await CacheDeviceIdsAsync();
		    }
		    Log.Information("Device {0} already exists ", device.Name);
		    return new DeviceRegisterResponse
		    {
			    DeviceId = device.Id.ToString(), 
			    Status = Status.Ok
		    };
	    }
	    
	    var newDeviceId = Guid.NewGuid();
	    
	    var newDevice = new CachedDevice
	    {
		    Id = newDeviceId,
		    Name = deviceName,
		    Type = request.Device.Type,
		    CreatedAt = DateTime.UtcNow,
		    LastSeen = DateTime.UtcNow
	    };

	    await dbService.CreateDeviceAsync(MongoEntityMapper.CreateDevice(request, newDeviceId));
	    
	    deviceIds.Add(newDeviceId);
	    await cache.SetAsync(cacheKey, newDevice, TimeSpan.FromHours(24));
	    await cache.SetAsync($"device:id:{newDeviceId}", newDevice, TimeSpan.FromHours(24));
	    
	    await CacheDeviceIdsAsync();
	    
	    Log.Information("Registered new device. Device with name {0} now has server id {1}", 
		    deviceName, newDeviceId);
	    
        return new DeviceRegisterResponse { DeviceId = newDeviceId.ToString(), Status = Status.Ok};
    }

    public override async Task<DeviceDataResponse> SendDeviceData(DeviceData request, ServerCallContext context)
    {
	    Log.Information("Received device data for {0}", request.DeviceId);

	    var deviceValue = request.DeviceValue;

	    var deviceId = Guid.Parse(request.DeviceId);
	    
	    var recentDataKey = $"data:{deviceId}:latest";
	    var deviceDataInfo = new
	    {
		    DeviceId = deviceId,
		    Timestamp = DateTime.UtcNow,
		    DataType = GetDataType(deviceValue),
		    HasEnvironmentalData = HasEnvironmentalReadings(deviceValue),
		    HasSystemHealth = HasSystemHealth(deviceValue)
	    };
	    
	    await cache.SetAsync(recentDataKey, deviceDataInfo, TimeSpan.FromMinutes(10));
	    var contentHash = GetDeviceValueHash(deviceValue);
	    var duplicateKey = $"processed:{deviceId}:{contentHash}";
	    
	    if (await cache.ExistsAsync(duplicateKey))
	    {
		    Log.Debug("Duplicate data detected for device {DeviceId}, skipping processing", deviceId);
		    return new DeviceDataResponse
		    {
			    Status = Status.Ok
		    };
	    }
	    
	    await ProcessDeviceDataAsync(deviceId, deviceValue);
	    
	    await cache.SetAsync(duplicateKey, true, TimeSpan.FromMinutes(5));
	    
	    await UpdateDeviceLastSeenAsync(deviceId);
	    
	    return new DeviceDataResponse { Status = Status.Ok };
    }
    
    private async Task ProcessDeviceDataAsync(Guid deviceId, DeviceProducedValue deviceValue)
    {
	    // Cache processing results based on device type and value
	    var cacheKey = $"processing:{deviceId}:{GetDeviceValueHash(deviceValue)}";
	    var cachedResult = await cache.GetAsync<DeviceDataResult>(cacheKey);
        
	    if (cachedResult != null)
	    {
		    Log.Debug("Using cached processing result for device {DeviceId}", deviceId);
		    cachedResult.RuleType = RuleType.Instant;
		    await dbService.SaveDeviceDataRecordAsync(cachedResult);
		    return;
	    }
	    
	    await dataController.ProcessMessage(deviceId, deviceValue);
    }
    
    private async Task LoadDeviceIdsFromCacheAsync()
    {
	    var cachedIds = await cache.GetAsync<List<Guid>>("active_device_ids");
	    if (cachedIds != null)
	    {
		    foreach (var id in cachedIds)
		    {
			    deviceIds.Add(id);
		    }
		    Log.Information("Loaded {Count} device IDs from cache", cachedIds.Count);
	    }
    }

    private async Task CacheDeviceIdsAsync()
    {
	    await cache.SetAsync("active_device_ids", deviceIds.ToList(), TimeSpan.FromDays(1));
    }
    
    private bool HasEnvironmentalReadings(DeviceProducedValue value)
    {
	    return value.ValueCase == DeviceProducedValue.ValueOneofCase.IndustrialDeviceValue &&
	           value.IndustrialDeviceValue.Environment != null;
    }

    private bool HasSystemHealth(DeviceProducedValue value)
    {
	    return value.ValueCase == DeviceProducedValue.ValueOneofCase.IndustrialDeviceValue &&
	           value.IndustrialDeviceValue.SystemHealth != null;
    }
    
    private string GetDataType(DeviceProducedValue value)
    {
	    return value.ValueCase switch
	    {
		    DeviceProducedValue.ValueOneofCase.DummyValue => "Dummy",
		    DeviceProducedValue.ValueOneofCase.SensorValue => "Sensor",
		    DeviceProducedValue.ValueOneofCase.IndustrialDeviceValue => "Industrial",
		    _ => "Unknown"
	    };
    }
    
    private int GetDeviceValueHash(DeviceProducedValue value)
    {
	    return value.ToString().GetHashCode();
    }
    
    private async Task UpdateDeviceLastSeenAsync(Guid deviceId)
    {
	    var deviceKey = $"device:id:{deviceId}";
	    var device = await cache.GetAsync<CachedDevice>(deviceKey);
        
	    if (device != null)
	    {
		    device.LastSeen = DateTime.UtcNow;
		    await cache.SetAsync(deviceKey, device, TimeSpan.FromHours(24));
	    }
    }
}
