namespace ControllerServer.Caching;

public interface ICacheService
{
	Task<T?> GetAsync<T>(string key);
	Task SetAsync<T>(string key, T value, TimeSpan? expiration = null);
	Task<bool> ExistsAsync(string key);
}
