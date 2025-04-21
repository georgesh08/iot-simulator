using Base.Base;
using Grpc.Core;
using Serilog;

namespace SimulatorServer;

public class GrpcSimulatorServer
{
	private int port;
	private readonly Server grpcServer;
	
	private readonly IoTDeviceService ioTDeviceService;

	public GrpcSimulatorServer(List<ABaseIoTDevice> devices, int period)
	{
		port = Environment.GetEnvironmentVariable("GRPC_SERVER_PORT") != null
			? Convert.ToInt32(Environment.GetEnvironmentVariable("GRPC_SERVER_PORT"))
			: 16848;
		
		grpcServer = new Server
		{
			Ports = { new ServerPort("0.0.0.0", port, ServerCredentials.Insecure) }
		};
		
		ioTDeviceService = new IoTDeviceService(period)
		{
			DevicesToRegister = devices
		};
	}

	public void Start()
	{
		ELKLogger.Information("Starting grpc server...");
		
		grpcServer.Start();
		ioTDeviceService.Start();
		
		ELKLogger.Information($"Gprc server started at port: {port}");
	}

	public async Task StopAsync(TimeSpan? timeout = null)
	{
		await ELKLogger.Information("Stooping grpc server...");
		
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
			await ELKLogger.Information("Gprc server stopped.");
		}
	}
}
