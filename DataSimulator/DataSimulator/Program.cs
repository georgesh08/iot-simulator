using Serilog;
using Prometheus;

namespace DataSimulator;

internal class Program
{
	static void Main(string[] args)
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
			var metricsServer = new KestrelMetricServer(14622);
			metricsServer.Start();
			Log.Information("Started metrics server");
		}
		catch
		{
			Log.Information("Failed to start metrics server");
		}
		
		if (args.Length < 2)
		{
			Log.Error("Invalid number of arguments. Should be at least two: <number of devices> <data send period>");
			return;
		}
		
		Log.Information("Starting data simulation");
		
		var numberOfDevices = Convert.ToInt32(args[0]);
		var dataSendPeriod = Convert.ToInt32(args[1]); // in seconds

		var message = $"{numberOfDevices} devices will be generated, data sending period is {dataSendPeriod} seconds";
		
		Log.Information(message);
		
		var dataSimulator = new DataSimulator();
		
		dataSimulator.LaunchSimulator(numberOfDevices, dataSendPeriod);

		Console.WriteLine("Press any key to exit...");

		Console.ReadLine();
	}
	
}
