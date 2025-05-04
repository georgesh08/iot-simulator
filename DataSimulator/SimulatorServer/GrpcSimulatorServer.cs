using Base.Base;
using Grpc.Core;
using Grpc.Health.V1;
using Grpc.HealthCheck;
using Serilog;

namespace SimulatorServer;

public class GrpcSimulatorServer
{
	private int port;
	private readonly Server grpcServer;
	
	private readonly IoTDeviceService ioTDeviceService;
	private HealthServiceImpl healthService;

	public GrpcSimulatorServer(List<ABaseIoTDevice> devices, int period)
	{
		port = Environment.GetEnvironmentVariable("GRPC_SERVER_PORT") != null
			? Convert.ToInt32(Environment.GetEnvironmentVariable("GRPC_SERVER_PORT"))
			: 16868;
		
		grpcServer = new Server
		{
			Ports = { new ServerPort("0.0.0.0", port, ServerCredentials.Insecure) }
		};
		
		ioTDeviceService = new IoTDeviceService(period)
		{
			DevicesToRegister = devices
		};
		
		healthService = new HealthServiceImpl();
		healthService.SetStatus("", HealthCheckResponse.Types.ServingStatus.Serving);
    
		grpcServer.Services.Add(Health.BindService(healthService));
	}

	public void Start()
	{
		Log.Information("Starting grpc server...");
		
		grpcServer.Start();
		ioTDeviceService.Start();
		
		Log.Information("Gprc server started at port: {0}", port);
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
