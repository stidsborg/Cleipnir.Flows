using Cleipnir.Flows.AspNet;
using Cleipnir.ResilientFunctions.Helpers;
using Cleipnir.ResilientFunctions.Storage;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Cleipnir.Flows.MassTransit.RabbitMq.Console;

internal static class Program
{
    public static async Task Main(string[] args)
    {
        var host = await CreateHostBuilder([]).StartAsync();
        var bus = host.Services.GetRequiredService<IBus>();
        var store = host.Services.GetRequiredService<IFunctionStore>();

        const int testSize = 10;
        for (var i = 0; i < testSize; i++) 
            await bus.Publish(new MyMessage(i.ToString()));
        
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
                    .IntegrateWithMassTransit()
                );
                
                services.AddMassTransit(x =>
                {
                    x.SetKebabCaseEndpointNameFormatter();
                    x.UsingRabbitMq((context, configure) =>
                    {
                        configure.Host(
                            "localhost",
                            "/",
                            h =>
                            {
                                h.Username("guest");
                                h.Password("guest");
                            }
                        );
                        configure.ConfigureEndpoints(context);
                    });
                });
            });
}
