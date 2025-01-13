using Grpc.Core;
using Serilog;

namespace SimulatorServer;

public class GrpcSimulatorServer
{
	private readonly Server grpcServer;
	
	private readonly List<IGrpcService> services = new();
	private CancellationTokenSource token;

	public GrpcSimulatorServer()
	{
		grpcServer = new Server
		{
			Ports = { new ServerPort("0.0.0.0", 16848, ServerCredentials.Insecure) }
		};
	}

	public void Start()
	{
		Log.Information("Starting grpc server...");
		token?.Cancel();
		token = new CancellationTokenSource();
		
		foreach (var service in services)
		{
			service.Start(token);
		}

		grpcServer.Start();
		
		Log.Information("Gprc server started.");
	}

	public async Task StopAsync(TimeSpan? timeout = null)
	{
		await token?.CancelAsync();
		foreach (var service in services)
		{
			service.Stop();
		}
		
		var shutdownTask = grpcServer.ShutdownAsync();
		if (timeout.HasValue)
		{
			await Task.WhenAny(shutdownTask, Task.Delay(timeout.Value));
			if (!shutdownTask.IsCompleted)
			{
				await grpcServer.KillAsync();
				throw new TimeoutException("Server shutdown timed out");
			}
		}
		else
		{
			await shutdownTask;
		}
	}
}
