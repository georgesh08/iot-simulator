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
		
        httpWrapper.AddServer("elk", "http://localhost:5044");
        
        var messageToSend = "Rule engine has started";
        
        var logMessage = new LogMessage(Guid.NewGuid().ToString(), LogLevel.Info, messageToSend);
		
        var logString = JsonSerializer.Serialize(logMessage);
		
        httpWrapper.SendRequest("http://localhost:5044", HttpMethod.Post, logString);
        
        Console.WriteLine("Press any key to exit...");
        
        Console.ReadLine();

        await iotController.Stop();
    }
}

