using Cleipnir.Flows.AspNet;
using Cleipnir.ResilientFunctions.Domain;
using Cleipnir.ResilientFunctions.Helpers;
using Cleipnir.ResilientFunctions.Reactive.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Rebus.Bus;
using Rebus.Config;
using Rebus.Transport.InMem;
using Shouldly;

namespace Cleipnir.Flows.Rebus.Tests;

[TestClass]
public class IntegrationTests
{
    public record MyMessage(string Value);

    private class RebusTestFlow : Flow, ISubscribeTo<MyMessage>
    {
        public static RoutingInfo Correlate(MyMessage msg) => Route.To(msg.Value);
        
        public static volatile MyMessage? ReceivedMyMessage; 
        
        public override async Task Run()
        {
            ReceivedMyMessage = await Messages.FirstOfType<MyMessage>();
        }
    }

    private class RebusTestFlows : Flows<RebusTestFlow>
    {
        public RebusTestFlows(FlowsContainer flowsContainer) : base(flowName: "RebusTestFlow", flowsContainer) { }
    }

    private class TestHostedService : IHostedService
    {
        public static IServiceProvider? ServiceProvider { get; set; }

        public TestHostedService(IServiceProvider serviceProvider) => ServiceProvider = serviceProvider;

        public Task StartAsync(CancellationToken cancellationToken)
        {
            return ServiceProvider!.GetRequiredService<IBus>().SendLocal(new MyMessage("SomeMessage"));
        }
        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
    
    [TestMethod]
    public async Task SunshineScenario()
    { 
        var host = Host.CreateDefaultBuilder(null)
            .ConfigureServices((_, services) =>
            {
                services.AddHostedService<TestHostedService>();
                services.UseFlows(c => c
                    .UseInMemoryStore()
                    .RegisterFlow<RebusTestFlow, RebusTestFlows>());
                services.AddRebus(
                    configure => configure
                        .Transport(t => t.UseInMemoryTransport(new InMemNetwork(), "who cares"))
                );

                services.IntegrateRebusWithFlows(
                    c => c.AddFlowsAutomatically(typeof(RebusTestFlow).Assembly)
                );
            });
        var cancellationTokenSource = new CancellationTokenSource();
        _ = Task.Run(() =>
                host.RunConsoleAsync(cancellationTokenSource.Token),
            cancellationTokenSource.Token
        );

        await BusyWait.UntilAsync(() => RebusTestFlow.ReceivedMyMessage is not null);
        RebusTestFlow.ReceivedMyMessage!.Value.ShouldBe("SomeMessage");
        await cancellationTokenSource.CancelAsync();
    }
}