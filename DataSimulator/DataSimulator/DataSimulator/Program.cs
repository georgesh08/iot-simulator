using Serilog;

namespace DataSimulator;

internal class Program
{
	static void Main(string[] args)
	{
		Log.Logger = new LoggerConfiguration().MinimumLevel.Debug()
			.WriteTo.Console()
			.CreateLogger();
		
		if (args.Length < 2)
		{
			Log.Error("Invalid number of arguments. Should be two: <number of devices> <data send period>");
			return;
		}
		
		var numberOfDevices = Convert.ToInt32(args[0]);
		var dataSendPeriod = Convert.ToInt32(args[1]); // in seconds
		
		DataSimulator.LaunchSimulator(numberOfDevices, dataSendPeriod);
	}
}
