using System.Text.Json;
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
        
        ruleEngine.Start();

        Log.Information("Rule engine has started");
        
        Console.WriteLine("Press any key to exit...");

        Console.ReadLine();
    }
}
