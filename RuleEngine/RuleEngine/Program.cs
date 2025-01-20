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
        
        ruleEngine.Start();
        
        Console.WriteLine("Press any key to exit...");

        Console.ReadLine();
    }
}
