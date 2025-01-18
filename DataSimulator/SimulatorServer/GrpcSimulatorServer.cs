using Base.Base;
using Grpc.Core;
using Serilog;

namespace SimulatorServer;

public class GrpcSimulatorServer
{
	private readonly Server grpcServer;
	
	private readonly IoTDeviceService ioTDeviceService;

	public GrpcSimulatorServer(List<ABaseIoTDevice> devices, int period)
	{
		grpcServer = new Server
		{
			Ports = { new ServerPort("127.0.0.1", 16848, ServerCredentials.Insecure) }
		};
		
		ioTDeviceService = new IoTDeviceService(period)
		{
			DevicesToRegister = devices
		};
	}

	public void Start()
	{
		Log.Information("Starting grpc server...");
		
		grpcServer.Start();
		ioTDeviceService.Start();
		
		Log.Information("Gprc server started.");
	}

	public async Task StopAsync(TimeSpan? timeout = null)
	{
		Log.Information("Stooping grpc server...");
		
		ioTDeviceService.Stop();
		
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
			Log.Information("Gprc server stopped.");
		}
	}
}
