using Cleipnir.ResilientFunctions.Domain;
using Cleipnir.ResilientFunctions.Helpers;
using Cleipnir.ResilientFunctions.Reactive.Extensions;
using Cleipnir.ResilientFunctions.Storage;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace Cleipnir.Flows.Tests.Flows;

[TestClass]
public class CorrelationIdFlowTests
{
    [TestMethod]
    public async Task MessageIsRoutedToFlowUsingCorrelationId()
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddTransient<RouteUsingCorrelationParamlessFlow>();

        var flowStore = new InMemoryFunctionStore();
        var flowsContainer = new FlowsContainer(
            flowStore,
            serviceCollection.BuildServiceProvider(),
            Options.Default
        );

        var flows = new RouteUsingCorrelationParamlessFlows(flowsContainer);
        await flows.Schedule("SomeInstanceId");

        await BusyWait.Until(() => RouteUsingCorrelationParamlessFlow.CorrelationRegistered);
        
        var msg = new Message("SomeCorrelationId", "SomeValue");
        await flows.RouteMessage(msg, msg.Route);

        await BusyWait.Until(() => RouteUsingCorrelationParamlessFlow.ReceivedMessage != null);
        RouteUsingCorrelationParamlessFlow.ReceivedMessage.ShouldBe(msg);
    }

    public class RouteUsingCorrelationParamlessFlow : Flow
    {
        public static volatile bool CorrelationRegistered;
        public static Message? ReceivedMessage { get; set; }

        public override async Task Run()
        {
            await Workflow.RegisterCorrelation("SomeCorrelationId");
            CorrelationRegistered = true;
            
            ReceivedMessage = await Messages.FirstOfType<Message>();
        }
    }
    
    public record Message(string Route, string Value);
}