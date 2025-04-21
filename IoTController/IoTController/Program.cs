using System.Text.Json;
using ControllerServer;
using Serilog;

namespace IoTController;

internal class Program
{
    public static async Task Main(string[] args)
    {
        Log.Logger = new LoggerConfiguration().MinimumLevel.Debug()
            .WriteTo.Console()
            .CreateLogger();
        
        var iotController = new IoTController();

        iotController.LaunchController();
        
        HttpClientWrapper httpWrapper = new();
		
        var elkServer = Environment.GetEnvironmentVariable("ELK_HOST");
        var elkPort = Environment.GetEnvironmentVariable("ELK_PORT");
		
        httpWrapper.AddServer("elk", $"http://{elkServer}:{elkPort}");
        
        var messageToSend = "IoT controller has started";
        
        var logMessage = new LogMessage(Guid.NewGuid().ToString(), LogLevel.Info, messageToSend);
		
        var logString = JsonSerializer.Serialize(logMessage);
		
        httpWrapper.SendRequest("elk", HttpMethod.Post, content: logString);
        
        Console.WriteLine("Press any key to exit...");
        
        Console.ReadLine();

        await iotController.Stop();
    }
}

