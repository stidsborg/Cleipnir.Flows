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

        //await C_NewsletterSender.Distributed.Example.Perform();
        await H_BankTransfer.Example.Perform();
        
        Console.ReadLine();
    }
}