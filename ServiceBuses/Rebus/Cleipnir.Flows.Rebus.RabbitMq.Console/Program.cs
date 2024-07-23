using Cleipnir.Flows.AspNet;
using Cleipnir.ResilientFunctions.Helpers;
using Cleipnir.ResilientFunctions.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Rebus.Bus;
using Rebus.Config;
using Rebus.Routing.TypeBased;

namespace Cleipnir.Flows.Rebus.RabbitMq.Console;

internal static class Program
{
    public static async Task Main(string[] args)
    {
        var host = await CreateHostBuilder([]).StartAsync();

        var bus = host.Services.GetRequiredService<IBus>();
        var store = host.Services.GetRequiredService<IFunctionStore>();

        const int testSize = 100;
        
        for (var i = 0; i < testSize; i++)
        {
            await bus.SendLocal(new MyMessage(i.ToString()));
            await Task.Delay(100);
        }
        
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
        await host.StopAsync();
    }

    private static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureServices((_, services) =>
            {
                services.AddFlows(c => c
                    .UseInMemoryStore()
                    .RegisterFlowsAutomatically()
                    .IntegrateWithRebus()
                );

                services.AddRebus(configure =>
                    configure
                        .Routing(r => r
                            .TypeBased()
                            .MapAssemblyOf<SimpleFlow>("simple-flows-queue")
                        )
                        .Transport(
                            t => t
                                .UseRabbitMq(
                                    "amqp://guest:guest@localhost",
                                    "simple-flows-demo"
                                )
                        )
                );
            });
}
