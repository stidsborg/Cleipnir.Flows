using Cleipnir.ResilientFunctions.Domain;
using Cleipnir.ResilientFunctions.Reactive.Extensions;
using MassTransit;

namespace Cleipnir.Flows.MassTransit.Console;

public class SimpleFlow : Flow, ISubscription<ConsumeContext<MyMessage>>
{
    public static RoutingInfo Correlate(ConsumeContext<MyMessage> msg) 
        => Route.To(msg.Message.Value);
    
    public override async Task Run()
    {
        var msg = await Messages.FirstOfType<MyMessage>();
        System.Console.WriteLine($"SimpleFlow({msg}) executed");
    }
}

public class SimpleFlows : Flows<SimpleFlow>
{
    public SimpleFlows(FlowsContainer flowsContainer) 
        : base("SimpleFlow", flowsContainer, options: null) { }
}