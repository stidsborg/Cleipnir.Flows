using Cleipnir.ResilientFunctions.Domain;
using Cleipnir.ResilientFunctions.Reactive.Extensions;

namespace Cleipnir.Flows.MassTransit.RabbitMq.Console;

public class SimpleFlow : Flow, ISubscription<MyMessage>
{
    public static RoutingInfo Route(MyMessage msg) => ResilientFunctions.Domain.Route.To(msg.Value);
    
    public override async Task Run()
    {
        var msg = await Messages.FirstOfType<MyMessage>();
        System.Console.WriteLine($"SimpleFlow({msg}) executed");
    }
}