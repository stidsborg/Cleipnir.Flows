using Cleipnir.ResilientFunctions.Domain;
using Cleipnir.ResilientFunctions.Reactive.Extensions;

namespace Cleipnir.Flows.MassTransit.Console;

public class SimpleFlow : Flow, ISubscribeTo<MyMessage>
{
    public static RoutingInfo Correlate(MyMessage msg) => Route.To(msg.Value);
    
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