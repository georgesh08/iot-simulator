using System.Text.Json;
using Serilog;
using StackExchange.Redis;

namespace ControllerServer.Caching.Redis;

public class RedisCache(IConnectionMultiplexer redis) : ICacheService
{
	private readonly IDatabase db = redis.GetDatabase();

	public async Task<T?> GetAsync<T>(string key)
	{
		try
		{
			var value = await db.StringGetAsync(key);
			return value.HasValue ? JsonSerializer.Deserialize<T>(value) : default;
		}
		catch (Exception ex)
		{
			Log.Warning(ex, "Cache GET failed for key: {Key}", key);
			return default;
		}
	}

	public async Task SetAsync<T>(string key, T value, TimeSpan? expiry = null)
	{
		try
		{
			var json = System.Text.Json.JsonSerializer.Serialize(value);
			await db.StringSetAsync(key, json, expiry ?? TimeSpan.FromMinutes(30));
		}
		catch (Exception ex)
		{
			Log.Warning(ex, "Cache SET failed for key: {Key}", key);
		}
	}

	public async Task<bool> ExistsAsync(string key)
	{
		try
		{
			return await db.KeyExistsAsync(key);
		}
		catch
		{
			return false;
		}
	}
}
