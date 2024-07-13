using Cleipnir.Flows.AspNet;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Rebus.Bus;
using Rebus.Config;
using Rebus.Transport.InMem;

namespace Cleipnir.Flows.Rebus.Console;

internal static class Program
{
    private class Service(IBus bus) : IHostedService
    {
        
        public Task StartAsync(CancellationToken cancellationToken)
        {
            _ = Task.Delay(1_000).ContinueWith(_ =>
                bus.SendLocal(new MyMessage("Hallo from HostedService!!!"))
            );
            
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
    
    public static async Task Main(string[] args)
    {
        await CreateHostBuilder([]).RunConsoleAsync();
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
                    configure.Transport(
                        t => t.UseInMemoryTransport(new InMemNetwork(), "who cares")
                    )
                );
                services.AddHostedService<Service>();
            });
}
