using Serilog;

namespace IoTController;

internal class Program
{
    public static void Main(string[] args)
    {
        Log.Logger = new LoggerConfiguration().MinimumLevel.Debug()
            .WriteTo.Console()
            .CreateLogger();
        
        Console.WriteLine("Hello World!");
    }
}

