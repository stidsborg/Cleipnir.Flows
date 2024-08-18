using Cleipnir.Flows.AspNet;
using Cleipnir.ResilientFunctions.Helpers;
using Cleipnir.ResilientFunctions.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Rebus.Bus;
using Rebus.Config;
using Rebus.Transport.InMem;

namespace Cleipnir.Flows.Rebus.Console;

internal static class Program
{
    public static async Task Main(string[] args)
    {
        var host = await CreateHostBuilder([]).StartAsync();
        var bus = host.Services.GetRequiredService<IBus>();
        var store = host.Services.GetRequiredService<IFunctionStore>();
        
        for (var i = 0; i < 1_000; i++)
            await bus.SendLocal(new MyMessage(i.ToString()));
        
        while (true)
        {
            var succeeded = await store.GetSucceededFunctions(
                nameof(SimpleFlow),
                DateTime.UtcNow.Ticks + 1_000_000
            ).SelectAsync(f => f.Count);
            if (succeeded == 1_000)
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

                services.AutoRegisterHandlersFromAssembly(typeof(Program).Assembly);
                services.AddRebus(configure => 
                    configure.Transport(
                        t => t.UseInMemoryTransport(new InMemNetwork(), "who cares")
                    )
                );
            });
}
