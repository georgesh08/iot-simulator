using System.Text.Json;
using Base;
using Serilog;
using SimulatorServer;

namespace DataSimulator;

internal class Program
{
	static async Task Main(string[] args)
	{
		Log.Logger = new LoggerConfiguration().MinimumLevel.Debug()
			.WriteTo.Console()
			.CreateLogger();
		
		HttpClientWrapper httpWrapper = new();
		
		var elkServer = Environment.GetEnvironmentVariable("ELK_HOST");
		var elkPort = Environment.GetEnvironmentVariable("ELK_PORT");
		
		httpWrapper.AddServer("elk", $"http://{elkServer}:{elkPort}");
		
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
		
		var messageToSend = "Data simulator has started" + message;

		var logMessage = new LogMessage(Guid.NewGuid().ToString(), LogLevel.Info, messageToSend);
		
		var logString = JsonSerializer.Serialize(logMessage);
		
		httpWrapper.SendRequest("elk", HttpMethod.Post, content: logString);

		Console.WriteLine("Press any key to exit...");

		Console.ReadLine();
		
		await dataSimulator.Stop();
	}
	
}
