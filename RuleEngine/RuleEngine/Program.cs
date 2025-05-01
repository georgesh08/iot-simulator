using Prometheus;
using Serilog;

namespace RuleEngine;

internal class Program
{
    public static void Main(string[] args)
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
        
        var ruleEngine = new RuleEngine();

        try
        {
	        var metricsServer = new KestrelMetricServer(14624);
	        metricsServer.Start();
	        Log.Information("Started metrics server");
        }
        catch
        {
	        Log.Information("Failed to start metrics server");
        }
        
        ruleEngine.Start();

        Log.Information("Rule engine has started");
        
        Console.WriteLine("Press any key to exit...");

        Console.ReadLine();
    }
}
