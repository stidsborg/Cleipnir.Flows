using Cleipnir.Flows.AspNet;
using Cleipnir.ResilientFunctions.Helpers;
using Cleipnir.ResilientFunctions.Reactive.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Shouldly;

namespace Cleipnir.Flows.NServiceBus.Tests;

public record MyMessage(string Value) : IEvent;

[TestClass]
public class IntegrationTests
{
    public class TestFlow : Flow
    {
        public static volatile MyMessage? ReceivedMyMessage; 
        
        public override async Task Run()
        {
            ReceivedMyMessage = await Messages.FirstOfType<MyMessage>();
        }
    }

    public class TestFlows : Flows<TestFlow>
    {
        public TestFlows(FlowsContainer flowsContainer) : base(flowName: "RebusTestFlow", flowsContainer) { }
    }

    private class TestHostedService : IHostedService
    {
        public static IServiceProvider? ServiceProvider { get; set; }

        public TestHostedService(IServiceProvider serviceProvider) => ServiceProvider = serviceProvider;

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await ServiceProvider!.GetRequiredService<IMessageSession>().Publish(new MyMessage("SomeMessage"), cancellationToken);
        }
        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
    
    [TestMethod]
    public async Task SunshineScenario()
    { 
        var host = await Host
            .CreateDefaultBuilder([])
            .ConfigureServices((_, services) =>
            {
                services.AddHostedService<TestHostedService>();
                
                services.AddFlows(c => c
                    .UseInMemoryStore()
                    .RegisterFlow<TestFlow, TestFlows>()
                );
                
            })
            .UseNServiceBus(_ =>
            {
                var endpointConfiguration = new EndpointConfiguration("Training");
                endpointConfiguration.UseTransport<LearningTransport>();
                endpointConfiguration.EnableInstallers();
                endpointConfiguration.UseSerialization(new SystemJsonSerializer());

                return endpointConfiguration;
            })
            .StartAsync();

        await BusyWait.Until(() => TestFlow.ReceivedMyMessage is not null, maxWait: TimeSpan.FromSeconds(5));
        TestFlow.ReceivedMyMessage!.Value.ShouldBe("SomeMessage");
        
        host.Dispose();
    }
}