using System.Text.Json;
using Serilog;

namespace RuleEngine;

internal class Program
{
    public static void Main(string[] args)
    {
        Log.Logger = new LoggerConfiguration().MinimumLevel.Debug()
            .WriteTo.Console()
            .CreateLogger();
        
        var ruleEngine = new RuleEngine();
        
        HttpClientWrapper httpWrapper = new();
		
		var elkServer = Environment.GetEnvironmentVariable("ELK_HOST");
	    var elkPort = Environment.GetEnvironmentVariable("ELK_PORT");
        		
	    httpWrapper.AddServer("elk", $"http://{elkServer}:{elkPort}");
        
        ruleEngine.Start();

        var messageToSend = "Rule engine has started";
        
        var logMessage = new LogMessage(Guid.NewGuid().ToString(), LogLevel.Info, messageToSend);
		
        var logString = JsonSerializer.Serialize(logMessage);
		
        httpWrapper.SendRequest("elk", HttpMethod.Post, content: logString);
        
        Console.WriteLine("Press any key to exit...");

        Console.ReadLine();
    }
}
