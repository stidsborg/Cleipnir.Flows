using Cleipnir.Flows.AspNet;
using Cleipnir.ResilientFunctions.Helpers;
using Cleipnir.ResilientFunctions.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Wolverine;

namespace Cleipnir.Flows.Wolverine.Console;

internal static class Program
{
    public static async Task Main(string[] args)
    {
        var host = await CreateHostBuilder([]).StartAsync();
        var bus = host.Services.GetRequiredService<IMessageBus>();
        var store = host.Services.GetRequiredService<IFunctionStore>();
        
        var testSize = 1_000;
        for (var i = 0; i < testSize; i++)
            await bus.PublishAsync(new MyMessage(i.ToString()));

        while (true)
        {
            var succeeded = await store.GetSucceededFunctions(
                nameof(SimpleFlow),
                DateTime.UtcNow.Ticks + 1_000_000
            ).SelectAsync(f => f.Count);
            if (succeeded == testSize)
                break;

            await Task.Delay(250);
        }

        System.Console.WriteLine("All completed");
    }
    
    private static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureServices((_, services) =>
            {
                services.AddFlows(c => c
                    .UseInMemoryStore()
                    .RegisterFlowsAutomatically()
                );
            })
            .UseWolverine();
}
