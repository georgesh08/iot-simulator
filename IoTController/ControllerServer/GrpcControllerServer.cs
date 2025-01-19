using Grpc.Core;
using Serilog;

namespace ControllerServer;

public class GrpcControllerServer
{
    private const int Port = 18686;
    private readonly Server grpcServer;

    public GrpcControllerServer()
    {
        grpcServer = new Server
        {
            Ports = { new ServerPort("127.0.0.1", Port, ServerCredentials.Insecure) }
        };
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