using Serilog;

namespace DataSimulator;

internal class Program
{
	static async Task Main(string[] args)
	{
		Log.Logger = new LoggerConfiguration().MinimumLevel.Debug()
			.WriteTo.Console()
			.CreateLogger();
		
		if (args.Length < 2)
		{
			Log.Error("Invalid number of arguments. Should be at least two: <number of devices> <data send period>");
			return;
		}
		
		string controllerHost;
		if (args.Length == 3)
		{
			controllerHost = args[2];
			Log.Information("Use provided controller host: {0}", controllerHost);
		}
		else
		{
			controllerHost = "localhost";
			Log.Information("Use localhost as controller host");
		}
		
		Log.Information("Starting data simulation");
		
		var numberOfDevices = Convert.ToInt32(args[0]);
		var dataSendPeriod = Convert.ToInt32(args[1]); // in seconds
		
		Log.Information("{0} devices will be generated, data sending period is {1} seconds", 
			numberOfDevices, dataSendPeriod);
		
		var dataSimulator = new DataSimulator();
		
		dataSimulator.LaunchSimulator(numberOfDevices, dataSendPeriod, controllerHost);

		Console.WriteLine("Press any key to exit...");

		Console.ReadLine();
		
		await dataSimulator.Stop();
	}
}
