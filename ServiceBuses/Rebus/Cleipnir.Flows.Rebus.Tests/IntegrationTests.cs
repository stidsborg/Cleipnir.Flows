using Cleipnir.Flows.AspNet;
using Cleipnir.ResilientFunctions.Helpers;
using Cleipnir.ResilientFunctions.Reactive.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Rebus.Bus;
using Rebus.Config;
using Rebus.Handlers;
using Rebus.Transport.InMem;
using Shouldly;

namespace Cleipnir.Flows.Rebus.Tests;

[TestClass]
public class IntegrationTests
{
    public record MyMessage(string Value);

    public class RebusTestFlow : Flow
    {
        public static volatile MyMessage? ReceivedMyMessage; 
        
        public override async Task Run()
        {
            ReceivedMyMessage = await Messages.FirstOfType<MyMessage>();
        }
    }

    public class RebusTestFlows : Flows<RebusTestFlow>
    {
        public RebusTestFlows(FlowsContainer flowsContainer) : base(flowName: "RebusTestFlow", flowsContainer) { }
    }

    public class RebusTestFlowHandler(RebusTestFlows flows) : IHandleMessages<MyMessage>
    {
        public Task Handle(MyMessage message) => flows.SendMessage(message.Value, message);
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
                
                services.AddFlows(c => c
                    .UseInMemoryStore()
                    .RegisterFlow<RebusTestFlow, RebusTestFlows>()
                );
                
                services.AddRebus(configure =>
                    configure.Transport(t => 
                        t.UseInMemoryTransport(new InMemNetwork(), "who cares")
                    )
                );
                services.AutoRegisterHandlersFromAssemblyOf<IntegrationTests>();
            });
        var cancellationTokenSource = new CancellationTokenSource();
        _ = Task.Run(() =>
                host.RunConsoleAsync(cancellationTokenSource.Token),
            cancellationTokenSource.Token
        );

        await BusyWait.Until(() => RebusTestFlow.ReceivedMyMessage is not null, maxWait: TimeSpan.FromMinutes(5));
        RebusTestFlow.ReceivedMyMessage!.Value.ShouldBe("SomeMessage");
        await cancellationTokenSource.CancelAsync();
    }
}