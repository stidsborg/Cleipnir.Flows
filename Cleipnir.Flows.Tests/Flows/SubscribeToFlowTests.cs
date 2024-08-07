using Cleipnir.ResilientFunctions.Domain;
using Cleipnir.ResilientFunctions.Helpers;
using Cleipnir.ResilientFunctions.Reactive.Extensions;
using Cleipnir.ResilientFunctions.Storage;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace Cleipnir.Flows.Tests.Flows;

[TestClass]
public class SubscribeToFlowTests
{
    [TestMethod]
    public async Task MessageIsRoutedToFlowUsingInstanceId()
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddTransient<RouteToInstanceParamlessFlow>();

        var flowStore = new InMemoryFunctionStore();
        var flowsContainer = new FlowsContainer(
            flowStore,
            serviceCollection.BuildServiceProvider(),
            Options.Default
        );

        var flows = new RouteToInstanceParamlessFlows(flowsContainer);
        await flows.Schedule("SomeInstanceId");
        
        var msg = new Message("SomeInstanceId", "SomeValue");
        await flows.Postman.RouteMessage(msg);

        await BusyWait.Until(() => RouteToInstanceParamlessFlow.ReceivedMessage != null);
        RouteToInstanceParamlessFlow.ReceivedMessage.ShouldBe(msg);
    }

    public class RouteToInstanceParamlessFlow : Flow, ISubscription<Message>
    {
        public static RoutingInfo Correlate(Message msg) => ResilientFunctions.Domain.Route.To(msg.Route);
        
        public static Message? ReceivedMessage { get; set; }

        public override async Task Run()
        {
            ReceivedMessage = await Messages.FirstOfType<Message>();
        }
    }

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
        await ((IBaseFlows) flows).Postman.RouteMessage(msg);

        await BusyWait.Until(() => RouteUsingCorrelationParamlessFlow.ReceivedMessage != null);
        RouteUsingCorrelationParamlessFlow.ReceivedMessage.ShouldBe(msg);
    }

    public class RouteUsingCorrelationParamlessFlow : Flow, ISubscription<Message>
    {
        public static RoutingInfo Correlate(Message msg) => ResilientFunctions.Domain.Route.Using(msg.Route);

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