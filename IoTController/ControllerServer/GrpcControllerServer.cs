using Grpc.Core;
using IoTServer;
using Serilog;

namespace ControllerServer;

public class GrpcControllerServer
{
    private const int Port = 18686;
    private readonly Server grpcServer;
    
    private IoTControllerService iotControllerService;

    public GrpcControllerServer()
    {
        grpcServer = new Server
        {
            Ports = { new ServerPort("0.0.0.0", Port, ServerCredentials.Insecure) }
        };

        iotControllerService = new IoTControllerService();
        
        grpcServer.Services.Add(IoTDeviceService.BindService(iotControllerService));
    }
    
    public void Start()
    {
        Log.Information("Starting grpc server...");
		
        grpcServer.Start();
		
        Log.Information("Gprc server started at port: {0}.", Port);
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
