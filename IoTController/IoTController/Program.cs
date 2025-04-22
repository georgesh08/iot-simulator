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
        
        var iotController = new IoTController();

        iotController.LaunchController();

        Log.Information("IoT controller has started");
        
        Console.WriteLine("Press any key to exit...");
        
        Console.ReadLine();

        await iotController.Stop();
    }
}

