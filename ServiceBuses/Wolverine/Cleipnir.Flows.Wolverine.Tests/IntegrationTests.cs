using Cleipnir.Flows.AspNet;
using Cleipnir.ResilientFunctions.Helpers;
using Cleipnir.ResilientFunctions.Reactive.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Shouldly;
using Wolverine;

namespace Cleipnir.Flows.Wolverine.Tests;

[TestClass]
public class IntegrationTests
{
    public record MyMessage(string Value);

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
            await ServiceProvider!.GetRequiredService<IMessageBus>().SendAsync(new MyMessage("SomeMessage"));
        }
        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
    
    [TestMethod]
    public async Task SunshineScenario()
    { 
        var host = await Host
            .CreateDefaultBuilder([])
            .UseWolverine()
            .ConfigureServices((_, services) =>
            {
                services.AddHostedService<TestHostedService>();
                
                services.AddFlows(c => c
                    .UseInMemoryStore()
                    .RegisterFlow<TestFlow, TestFlows>()
                );
            })
            .StartAsync();

        await BusyWait.Until(() => TestFlow.ReceivedMyMessage is not null, maxWait: TimeSpan.FromSeconds(5));
        TestFlow.ReceivedMyMessage!.Value.ShouldBe("SomeMessage");
        
        host.Dispose();
    }
}