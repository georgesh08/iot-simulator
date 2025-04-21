using System.Text;

namespace RuleEngine;

public class HttpClientWrapper
{
	private readonly Dictionary<string, HttpClient> httpClients;
	private readonly HttpClient defaultClient;
	
	public HttpClientWrapper()
	{
		httpClients = new Dictionary<string, HttpClient>();
		defaultClient = new HttpClient();
	}
	
	public void AddServer(string serverName, string baseAddress)
	{
		if (httpClients.ContainsKey(serverName))
		{
			throw new ArgumentException($"Server '{serverName}' already exists");
		}

		var client = new HttpClient
		{
			BaseAddress = new Uri(baseAddress)
		};
        
		httpClients.Add(serverName, client);
	}
	
	public void RemoveServer(string serverName)
	{
		if (!httpClients.TryGetValue(serverName, out var client))
		{
			return;
		}

		client.Dispose();
		httpClients.Remove(serverName);
	}
	
	public async Task<HttpResponseMessage> SendRequest(
		string serverName,
		HttpMethod method,
		string requestUri = "/",
		string content = null,
		string mediaType = "application/json")
	{
		if (!httpClients.TryGetValue(serverName, out var client))
		{
			throw new KeyNotFoundException($"Server '{serverName}' not found");
		}

		return await SendRequestInternal(client, method, client.BaseAddress + requestUri, content, mediaType);
	}
	
	public async Task<HttpResponseMessage> SendRequest(
		Uri absoluteUri,
		HttpMethod method,
		string requestUri,
		string content = null,
		string mediaType = "application/json")
	{
		return await SendRequestInternal(defaultClient, method, absoluteUri.ToString(), content, mediaType);
	}

	public void Dispose()
	{
		foreach (var client in httpClients.Values)
		{
			client.Dispose();
		}
		defaultClient.Dispose();
		httpClients.Clear();
	}
	
	private async Task<HttpResponseMessage> SendRequestInternal(
		HttpClient client,
		HttpMethod method,
		string requestUri,
		string content,
		string mediaType)
	{
		using var requestMessage = new HttpRequestMessage(method, requestUri);

		if (content != null)
		{
			requestMessage.Content = new StringContent(content, Encoding.UTF8, mediaType);
		}

		return await client.SendAsync(requestMessage);
	}
}
