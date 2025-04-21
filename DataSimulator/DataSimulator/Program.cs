using System.Text.Json;
using Base;
using Serilog;
using SimulatorServer;

namespace DataSimulator;

internal class Program
{
	static async Task Main(string[] args)
	{
		var elkServer = Environment.GetEnvironmentVariable("ELK_HOST");
		var elkPort = Environment.GetEnvironmentVariable("ELK_PORT");
		
		ELKLogger.Initialize("elk", $"http://{elkServer}:{elkPort}");
		
		if (args.Length < 2)
		{
			ELKLogger.Error("Invalid number of arguments. Should be at least two: <number of devices> <data send period>");
			return;
		}
		
		ELKLogger.Information("Starting data simulation");
		
		var numberOfDevices = Convert.ToInt32(args[0]);
		var dataSendPeriod = Convert.ToInt32(args[1]); // in seconds

		var message = $"{numberOfDevices} devices will be generated, data sending period is {dataSendPeriod} seconds";
		
		ELKLogger.Information(message);
		
		var dataSimulator = new DataSimulator();
		
		dataSimulator.LaunchSimulator(numberOfDevices, dataSendPeriod);

		Console.WriteLine("Press any key to exit...");

		Console.ReadLine();
		
		await dataSimulator.Stop();
	}
	
}
