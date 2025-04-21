using System.Text.Json;
using Base;

namespace SimulatorServer;

public class ELKLogger
{
	private static HttpClientWrapper? wrapper;
	private static string _serverName;
	
	public static void Initialize(string serverName, string baseUrl)
	{
		if (wrapper != null)
		{
			return;
		}
		
		_serverName = serverName;
		wrapper = new HttpClientWrapper();
		wrapper.AddServer(serverName, baseUrl);
	}
	
	public static async Task Information(string message) => await Log(LogLevel.Info, message);
	public static async Task Warning(string message) => await Log(LogLevel.Warning, message);
	public static async Task Error(string message) => await Log(LogLevel.Error, message);
	
	private static async Task Log(LogLevel level, string message)
	{
		if (wrapper == null)
		{
			await Console.Error.WriteLineAsync("ElkLogger is not initialized");
			return;
		}

		var log = new LogMessage(Guid.NewGuid().ToString(), level, message);
		var json = JsonSerializer.Serialize(log);

		try
		{
			var response = await wrapper.SendRequest(_serverName, HttpMethod.Post, "/", json);
			if (!response.IsSuccessStatusCode)
			{
				Console.Error.WriteLine($"Failed to send log: {response.StatusCode}");
			}
		}
		catch (Exception ex)
		{
			Console.Error.WriteLine($"Log exception: {ex.Message}");
		}
	}
}
