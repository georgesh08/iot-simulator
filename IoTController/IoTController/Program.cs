using Prometheus;
using Serilog;

namespace IoTController;

internal class Program
{
    public static async Task Main(string[] args)
    {
	    var elkServer = Environment.GetEnvironmentVariable("ELK_HOST");
	    var elkPort = Environment.GetEnvironmentVariable("ELK_PORT");
	    
	    Log.Logger = new LoggerConfiguration()
		    .MinimumLevel.Debug()
		    .WriteTo.Console()
		    .WriteTo.DurableHttpUsingFileSizeRolledBuffers(
			    requestUri: $"http://{elkServer}:{elkPort}",
			    textFormatter: new Serilog.Formatting.Json.JsonFormatter())
		    .CreateLogger();
	    
	    try
	    {
		    var metricsServer = new KestrelMetricServer(14620);
		    metricsServer.Start();
		    Log.Information("Started metrics server");
	    }
	    catch
	    {
		    Log.Information("Failed to start metrics server");
	    }
        
        var iotController = new IoTController();

        iotController.LaunchController();

        Log.Information("IoT controller has started");
        
        Console.WriteLine("Press any key to exit...");
        
        Console.ReadLine();

        await iotController.Stop();
    }
}

