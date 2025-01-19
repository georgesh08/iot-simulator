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
        
        Console.WriteLine("Press any key to exit...");
        
        Console.ReadLine();

        await iotController.Stop();
    }
}

