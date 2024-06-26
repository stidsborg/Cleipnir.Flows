using Serilog;

namespace Cleipnir.Flows.Sample.Presentation;

internal static class Program
{
    private static async Task Main(string[] args)
    {
        Log.Logger = new LoggerConfiguration()
            .WriteTo.Console()
            .Enrich.FromLogContext()
            .CreateLogger();

        await Task.CompletedTask;
        
        Console.ReadLine();
    }
}