using Cleipnir.ResilientFunctions.Domain;
using Cleipnir.ResilientFunctions.Reactive.Extensions;

namespace Cleipnir.Flows.MassTransit.Tests;

public class MassTransitTestFlows : Flows<MassTransitTestFlow>
{
    public MassTransitTestFlows(FlowsContainer flowsContainer) : base(flowName: "MassTransitTestFlow", flowsContainer) { }
}

public class MassTransitTestFlow : Flow, ISubscription<MyMessage>
{
    public static RoutingInfo Route(MyMessage msg) => ResilientFunctions.Domain.Route.To(msg.Value);
        
    public static volatile MyMessage? ReceivedMyMessage; 
        
    public override async Task Run()
    {
        ReceivedMyMessage = await Messages.FirstOfType<MyMessage>();
    }
}