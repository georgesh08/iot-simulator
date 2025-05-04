using Grpc.Core;
using Grpc.Health.V1;
using Grpc.HealthCheck;
using IoTServer;
using Serilog;

namespace ControllerServer;

public class GrpcControllerServer
{
    private int port;
    private readonly Server grpcServer;
    
    private IoTControllerService iotControllerService;
    private HealthServiceImpl healthService;

    public GrpcControllerServer()
    {
	    port = Environment.GetEnvironmentVariable("GRPC_SERVER_PORT") != null
		    ? Convert.ToInt32(Environment.GetEnvironmentVariable("GRPC_SERVER_PORT"))
		    : 18686;
	    
        grpcServer = new Server
        {
            Ports = { new ServerPort("0.0.0.0", port, ServerCredentials.Insecure) }
        };

        iotControllerService = new IoTControllerService();
        
        healthService = new HealthServiceImpl();
        healthService.SetStatus("", HealthCheckResponse.Types.ServingStatus.Serving);
        
        grpcServer.Services.Add(IoTDeviceService.BindService(iotControllerService));
        grpcServer.Services.Add(Health.BindService(healthService));
    }
    
    public void Start()
    {
        Log.Information("Starting grpc server...");
		
        grpcServer.Start();
		
        Log.Information("Gprc server started at port: {0}.", port);
    }
    
    public async Task StopAsync(TimeSpan? timeout = null)
    {
        Log.Information("Stooping grpc server..."); ;
		
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
