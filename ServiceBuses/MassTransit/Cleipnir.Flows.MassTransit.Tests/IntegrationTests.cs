using Cleipnir.Flows.AspNet;
using Cleipnir.ResilientFunctions.Helpers;
using MassTransit;
using Microsoft.Extensions.Hosting;
using Shouldly;
using Microsoft.Extensions.DependencyInjection;

namespace Cleipnir.Flows.MassTransit.Tests;

[TestClass]
public class IntegrationTests
{
    private class TestHostedService(IBus bus) : IHostedService
    {
        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await bus.Publish(new MyMessage("test"), cancellationToken);
        }
        
        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
    
    [TestMethod]
    public async Task SunshineScenario()
    { 
        var hostBuilder = Host.CreateDefaultBuilder(null)
            .ConfigureServices((_, services) =>
            {
                services.AddFlows(c => c
                    .UseInMemoryStore()
                    .RegisterFlow<MassTransitTestFlow, MassTransitTestFlows>()
                );
                
                services.AddMassTransit(x =>
                {
                    x.AddConsumers(GetType().Assembly);
                    x.UsingInMemory((context,cfg) =>
                    {
                        cfg.ConfigureEndpoints(context);
                    });
                });
                
                services.AddHostedService<TestHostedService>();
            });

        var host = await hostBuilder.StartAsync();
        
        await BusyWait.Until(() => MassTransitTestFlow.ReceivedMyMessage is not null);

        MassTransitTestFlow.ReceivedMyMessage.ShouldNotBeNull();
        MassTransitTestFlow.ReceivedMyMessage.Value.ShouldBe("test");

        await host.StopAsync();
    }
}